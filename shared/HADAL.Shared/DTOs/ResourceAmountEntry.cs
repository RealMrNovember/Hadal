using System.Collections.Generic;
using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// One entry of a resource map on the wire. A protobuf <c>map&lt;K,V&gt;</c> is encoded exactly as a
    /// repeated message with key = field 1 and value = field 2, so the bytes match the old map field.
    /// <para>
    /// IL2CPP rule for every wire DTO: no <see cref="Dictionary{TKey,TValue}"/> or <see cref="List{T}"/>
    /// members, only arrays. protobuf-net probes non-contract member types for "tuple" constructors and
    /// checks each public setter for init-only modifiers (List&lt;T&gt;.Capacity, KeyValuePair) through
    /// ParameterInfo.GetRequiredCustomModifiers, which IL2CPP does not implement. Code-facing
    /// collections stay as properties without [ProtoMember] and are copied in the
    /// [ProtoBeforeSerialization] / [ProtoAfterDeserialization] callbacks.
    /// </para>
    /// </summary>
    [ProtoContract]
    public sealed class ResourceAmountEntry
    {
        [ProtoMember(1)] public ResourceType Key { get; set; }
        [ProtoMember(2)] public long Value { get; set; }

        public static ResourceAmountEntry[] FromMap(Dictionary<ResourceType, long>? map)
        {
            if (map == null || map.Count == 0)
                return System.Array.Empty<ResourceAmountEntry>();

            var entries = new ResourceAmountEntry[map.Count];
            var i = 0;
            foreach (var pair in map)
                entries[i++] = new ResourceAmountEntry { Key = pair.Key, Value = pair.Value };
            return entries;
        }

        public static Dictionary<ResourceType, long> ToMap(ResourceAmountEntry[]? entries)
        {
            var map = new Dictionary<ResourceType, long>(entries?.Length ?? 0);
            if (entries == null)
                return map;

            // Last write wins, matching protobuf map semantics for duplicate keys.
            foreach (var entry in entries)
                map[entry.Key] = entry.Value;
            return map;
        }
    }
}
