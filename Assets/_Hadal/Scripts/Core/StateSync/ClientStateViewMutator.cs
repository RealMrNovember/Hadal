using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
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

            if (snapshot.Resources != null)
                ApplyResources(data, snapshot.Resources);

            if (snapshot.Buildings != null)
                ApplyBuildingList(data, snapshot.Buildings);

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
                    ApplyResourceChange(data, change, serialization);
                    continue;
                }

                if (change.EntityId.StartsWith(EntityIdPrefixes.Hero, StringComparison.Ordinal))
                {
                    ApplyHeroChange(data, change, serialization);
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

        private static void ApplyResourceChange(SaveGameData data, EntityChangeDto change, NetworkSerializationLayer serialization)
        {
            if (change.Kind == EntityChangeKind.Delete)
            {
                data.resources = Array.Empty<ResourceSaveEntry>();
                return;
            }

            if (!TryDeserializePayload(change.Payload, serialization, out ResourceStateDto dto))
                return;

            ApplyResources(data, dto);
        }

        private static void ApplyResources(SaveGameData data, ResourceStateDto dto)
        {
            if (dto.Amounts == null)
                return;

            data.resources = dto.Amounts
                .Select(pair => new ResourceSaveEntry
                {
                    resourceType = (int)pair.Key,
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
                cellY = dto.SectorIndex
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
