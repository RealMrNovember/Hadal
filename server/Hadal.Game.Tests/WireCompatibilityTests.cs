using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using ProtoBuf;
using Xunit;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Resource maps travel as repeated key/value entries (IL2CPP cannot run protobuf-net's map
    /// serializer). The bytes must stay identical to the protobuf map encoding older builds use.
    /// </summary>
    public sealed class WireCompatibilityTests
    {
        [ProtoContract]
        public sealed class LegacyResourceState
        {
            [ProtoMember(1)] public Dictionary<ResourceType, long> Amounts { get; set; } = new();
            [ProtoMember(2)] public ulong ServerTick { get; set; }
        }

        [ProtoContract]
        public sealed class LegacySnapshot
        {
            [ProtoMember(1)] public ulong ServerTick { get; set; }
            [ProtoMember(2)] public string PlayerId { get; set; } = string.Empty;
            [ProtoMember(4)] public List<BuildingStateDto> Buildings { get; set; } = new();
        }

        private static TOut Convert<TIn, TOut>(TIn value)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, value);
            stream.Position = 0;
            return Serializer.Deserialize<TOut>(stream);
        }

        private static byte[] Bytes<T>(T value)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, value);
            return stream.ToArray();
        }

        [Fact]
        public void Entry_list_reads_legacy_map_bytes()
        {
            var legacy = new LegacyResourceState
            {
                Amounts = { [ResourceType.Oxygen] = 4200, [ResourceType.Titanium] = 17 },
                ServerTick = 99
            };

            var current = Convert<LegacyResourceState, ResourceStateDto>(legacy);

            Assert.Equal(4200, current.Amounts[ResourceType.Oxygen]);
            Assert.Equal(17, current.Amounts[ResourceType.Titanium]);
            Assert.Equal(99UL, current.ServerTick);
        }

        [Fact]
        public void Entry_list_writes_map_bytes()
        {
            var current = new ResourceStateDto
            {
                Amounts = { [ResourceType.Oxygen] = 4200, [ResourceType.Food] = 3 },
                ServerTick = 7
            };
            var legacy = new LegacyResourceState
            {
                Amounts = { [ResourceType.Oxygen] = 4200, [ResourceType.Food] = 3 },
                ServerTick = 7
            };

            Assert.Equal(Bytes(legacy), Bytes(current));
            Assert.Equal(3, Convert<ResourceStateDto, LegacyResourceState>(current).Amounts[ResourceType.Food]);
        }

        [Fact]
        public void Production_rates_round_trip_inside_a_snapshot()
        {
            var snapshot = new StateSnapshot
            {
                ServerTick = 5,
                Production = new ProductionStateDto { NetPerTick = { [ResourceType.Energy] = -2 }, TickIntervalSeconds = 1 }
            };

            var copy = Convert<StateSnapshot, StateSnapshot>(snapshot);

            Assert.Equal(-2, copy.Production.NetPerTick[ResourceType.Energy]);
        }
        [Fact]
        public void Snapshot_buildings_keep_repeated_field_bytes()
        {
            var building = new BuildingStateDto { EntityId = "b:1", DefinitionId = "oxygen_plant", RingIndex = 1, Level = 2 };
            var current = new StateSnapshot { ServerTick = 3, Buildings = { building }, Resources = null!, Production = null! };
            var legacy = new LegacySnapshot { ServerTick = 3, Buildings = { building } };

            Assert.Equal(Bytes(legacy), Bytes(current));
            Assert.Equal("oxygen_plant", Assert.Single(Convert<LegacySnapshot, StateSnapshot>(legacy).Buildings).DefinitionId);
        }

        [Fact]
        public void Wire_contracts_use_arrays_only()
        {
            // IL2CPP cannot run protobuf-net's probe of List<T> / Dictionary<K,V> members.
            var offenders =
                from type in typeof(StateSnapshot).Assembly.GetTypes()
                where type.GetCustomAttribute<ProtoContractAttribute>() != null
                from property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                where property.GetCustomAttribute<ProtoMemberAttribute>() != null
                let t = property.PropertyType
                where t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>) || t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                select $"{type.Name}.{property.Name}";

            Assert.Empty(offenders);
        }
    }
}
