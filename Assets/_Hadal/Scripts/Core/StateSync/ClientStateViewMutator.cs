using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using Hadal.Core.Network;
using Hadal.Data.Models;

namespace Hadal.Core.StateSync
{
    /// <summary>
    /// Applies server snapshots/deltas into <see cref="ClientStateView"/> (RAM visual cache).
    /// </summary>
    public static class ClientStateViewMutator
    {
        public static void ApplySnapshot(ClientStateView view, StateSnapshot snapshot, NetworkSerializationLayer serialization)
        {
            if (view == null || snapshot == null)
                return;

            var data = view.Data;
            data.version = SaveGameData.CurrentVersion;

            // A snapshot is the full authoritative truth, including any offline accrual the
            // server already folded in — it wins over whatever tick the wallet was last at.
            if (snapshot.Resources != null)
            {
                var snapshotTick = snapshot.Resources.ServerTick != 0UL
                    ? snapshot.Resources.ServerTick
                    : snapshot.ServerTick;

                view.ForceResourceTick(snapshotTick);
                ApplyResources(data, snapshot.Resources);
            }

            if (snapshot.Buildings != null)
                ApplyBuildingList(data, snapshot.Buildings);

            // 0 = a server without ring unlocks; keep the catalog default then.
            if (snapshot.UnlockedRings > 0)
                data.unlockedGridRings = snapshot.UnlockedRings;

            view.ApplyProduction(snapshot.Production);

            view.MarkCaptured();
        }

        public static void ApplyDelta(
            ClientStateView view,
            StateDelta delta,
            NetworkSerializationLayer serialization,
            ICollection<CommandAckDto> commandAcks)
        {
            if (view == null || delta?.Changes == null)
                return;

            var data = view.Data;

            foreach (var change in delta.Changes)
            {
                if (change == null || string.IsNullOrEmpty(change.EntityId))
                    continue;

                if (change.EntityId.StartsWith(EntityIdPrefixes.Command, StringComparison.Ordinal))
                {
                    if (TryDeserializePayload(change.Payload, serialization, out CommandAckDto ack))
                        commandAcks.Add(ack);
                    continue;
                }

                if (change.EntityId.StartsWith(EntityIdPrefixes.Building, StringComparison.Ordinal))
                {
                    ApplyBuildingChange(data, change, serialization);
                    continue;
                }

                if (change.EntityId.StartsWith(EntityIdPrefixes.Resource, StringComparison.Ordinal))
                {
                    ApplyResourceChange(view, change, serialization);
                    continue;
                }

                if (change.EntityId.StartsWith(EntityIdPrefixes.Production, StringComparison.Ordinal))
                {
                    ApplyProductionChange(view, change, serialization);
                    continue;
                }

                if (change.EntityId.StartsWith(EntityIdPrefixes.Hero, StringComparison.Ordinal))
                {
                    ApplyHeroChange(data, change, serialization);
                    continue;
                }

                if (change.EntityId == EntityIds.BaseLayout && change.Kind != EntityChangeKind.Delete
                    && TryDeserializePayload(change.Payload, serialization, out BaseLayoutDto layout) && layout.UnlockedRings > 0)
                {
                    data.unlockedGridRings = layout.UnlockedRings;
                }
            }

            view.MarkCaptured();
        }

        private static void ApplyBuildingList(SaveGameData data, IEnumerable<BuildingStateDto> buildings)
        {
            data.buildings = buildings.Select(MapBuilding).ToArray();
        }

        private static void ApplyBuildingChange(SaveGameData data, EntityChangeDto change, NetworkSerializationLayer serialization)
        {
            var entityId = change.EntityId.Substring(EntityIdPrefixes.Building.Length);
            var list = data.buildings?.ToList() ?? new List<BuildingSaveEntry>();

            if (change.Kind == EntityChangeKind.Delete)
            {
                list.RemoveAll(b => b.instanceKey == entityId);
                data.buildings = list.ToArray();
                return;
            }

            if (!TryDeserializePayload(change.Payload, serialization, out BuildingStateDto dto))
                return;

            var entry = MapBuilding(dto);
            entry.instanceKey = string.IsNullOrEmpty(entry.instanceKey) ? entityId : entry.instanceKey;

            var index = list.FindIndex(b => b.instanceKey == entry.instanceKey);
            if (index >= 0)
                list[index] = entry;
            else
                list.Add(entry);

            data.buildings = list.ToArray();
        }

        private static void ApplyResourceChange(ClientStateView view, EntityChangeDto change, NetworkSerializationLayer serialization)
        {
            if (change.Kind == EntityChangeKind.Delete)
            {
                view.Data.resources = Array.Empty<ResourceSaveEntry>();
                return;
            }

            if (!TryDeserializePayload(change.Payload, serialization, out ResourceStateDto dto))
                return;

            // Balances replicate as absolute totals, so an out-of-order frame would move the
            // wallet backwards. Drop anything older than what we already hold.
            if (!view.TryAcceptResourceTick(dto.ServerTick))
                return;

            ApplyResources(view.Data, dto);
        }

        private static void ApplyProductionChange(ClientStateView view, EntityChangeDto change, NetworkSerializationLayer serialization)
        {
            if (change.Kind == EntityChangeKind.Delete)
            {
                view.ApplyProduction(new ProductionStateDto());
                return;
            }

            if (TryDeserializePayload(change.Payload, serialization, out ProductionStateDto dto))
                view.ApplyProduction(dto);
        }

        private static void ApplyResources(SaveGameData data, ResourceStateDto dto)
        {
            if (dto.Amounts == null)
                return;

            // resourceType stores the SHARED ordinal (see ResourceTypeMap). Unknown values are
            // dropped rather than written, so a schema skew cannot alias one resource onto another.
            data.resources = dto.Amounts
                .Where(pair => pair.Key != ResourceType.Unknown)
                .Select(pair => new ResourceSaveEntry
                {
                    resourceType = ResourceTypeMap.ToStoredValue(pair.Key),
                    amount = pair.Value
                })
                .ToArray();
        }

        private static void ApplyHeroChange(SaveGameData data, EntityChangeDto change, NetworkSerializationLayer serialization)
        {
            var entityId = change.EntityId.Substring(EntityIdPrefixes.Hero.Length);
            var list = data.heroes?.ToList() ?? new List<HeroSaveEntry>();

            if (change.Kind == EntityChangeKind.Delete)
            {
                list.RemoveAll(h => h.instanceId == entityId);
                data.heroes = list.ToArray();
                return;
            }

            if (!TryDeserializePayload(change.Payload, serialization, out HeroStateDto dto))
                return;

            var entry = new HeroSaveEntry
            {
                instanceId = string.IsNullOrEmpty(dto.EntityId) ? entityId : dto.EntityId,
                definitionId = dto.DefinitionId,
                level = dto.Level,
                experience = dto.Experience,
                faction = dto.Faction
            };

            var index = list.FindIndex(h => h.instanceId == entry.instanceId);
            if (index >= 0)
                list[index] = entry;
            else
                list.Add(entry);

            data.heroes = list.ToArray();
        }

        private static BuildingSaveEntry MapBuilding(BuildingStateDto dto)
        {
            return new BuildingSaveEntry
            {
                instanceKey = dto.EntityId,
                definitionId = dto.DefinitionId,
                level = dto.Level,
                cellRing = dto.RingIndex,
                cellSector = dto.SectorIndex,
                rotation = dto.RotationSteps * 90f,
                cellX = dto.RingIndex,
                cellY = dto.SectorIndex,
                // Remaining time is relative to when the frame arrived, so client clock skew is harmless.
                constructionEndsAt = dto.ConstructionRemainingMs > 0
                    ? UnityEngine.Time.realtimeSinceStartup + dto.ConstructionRemainingMs / 1000f
                    : 0f,
                constructionSeconds = dto.ConstructionTotalMs / 1000f
            };
        }

        private static bool TryDeserializePayload<T>(byte[] payload, NetworkSerializationLayer serialization, out T value)
        {
            value = default!;
            if (payload == null || payload.Length == 0)
                return false;

            return serialization.TryDeserialize(payload, out value);
        }
    }
}
