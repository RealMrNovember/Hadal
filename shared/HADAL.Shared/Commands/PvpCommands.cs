using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// A team sent against or for another player (docs/30): attack, scout, reinforce, start or join a rally.
    /// </summary>
    [ProtoContract]
    public sealed class MarchPvpCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>"attack", "scout", "reinforce", "rally" (start) or "join".</summary>
        [ProtoMember(3)] public string Mode { get; set; } = string.Empty;

        /// <summary>"base", "team" or "stronghold".</summary>
        [ProtoMember(4)] public string TargetKind { get; set; } = string.Empty;

        /// <summary>A player id (base), a march id (team), a stronghold id, or the rally id when joining.</summary>
        [ProtoMember(5)] public string TargetId { get; set; } = string.Empty;

        /// <summary>The team; empty = the strongest free heroes.</summary>
        [ProtoMember(6)] public string[] HeroIds { get; set; } = System.Array.Empty<string>();

        /// <summary>Rally start: muster time (60, 180, 300 or 600).</summary>
        [ProtoMember(7)] public int MusterSeconds { get; set; }
    }

    /// <summary>Asks for the alliance war list (<c>EntityIds.AllianceWar</c>).</summary>
    [ProtoContract]
    public sealed class ViewWarCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }

    /// <summary>Asks for the strategic map (<c>EntityIds.Territory</c>).</summary>
    [ProtoContract]
    public sealed class ViewTerritoryCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }

    /// <summary>Alliance land actions (docs/32): "hub", "beacon", "move" the hub, "remove" a beacon, "donate" to the hub, "abandon" a city.</summary>
    [ProtoContract]
    public sealed class TerritoryCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string Action { get; set; } = string.Empty;

        /// <summary>The structure or city acted on (remove, donate, abandon).</summary>
        [ProtoMember(4)] public string StrongholdId { get; set; } = string.Empty;

        /// <summary>Where a hub or beacon goes up, or where the hub moves (docs/32).</summary>
        [ProtoMember(5)] public int X { get; set; }
        [ProtoMember(6)] public int Y { get; set; }

        /// <summary>A donation: the resource ("Titanium", "Biomass", "Food") and how much.</summary>
        [ProtoMember(7)] public string Resource { get; set; } = string.Empty;
        [ProtoMember(8)] public long Amount { get; set; }
    }

    /// <summary>Raises a shield (docs/30 §3): the bag item, or its Hadalite price when there is none.</summary>
    [ProtoContract]
    public sealed class UseShieldCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string ItemId { get; set; } = string.Empty;
    }
}
