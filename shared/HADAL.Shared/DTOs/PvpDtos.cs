using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>The player's PvP status (entity <c>EntityIds.Pvp</c>, docs/30 §3, §5.1).</summary>
    [ProtoContract]
    public sealed class PvpStatusDto
    {
        /// <summary>Unix ms the shield ends; 0 = none.</summary>
        [ProtoMember(1)] public long ShieldUntilMs { get; set; }

        /// <summary>Under the beginner reactor level: cannot attack or be attacked.</summary>
        [ProtoMember(2)] public bool Beginner { get; set; }

        /// <summary>Hostile marches on the way to the base or a team of this player.</summary>
        [ProtoMember(3)] public IncomingDto[] Incoming { get; set; } = System.Array.Empty<IncomingDto>();

        [ProtoMember(4)] public long ServerNowMs { get; set; }
    }

    [ProtoContract]
    public sealed class IncomingDto
    {
        [ProtoMember(1)] public string MarchId { get; set; } = string.Empty;

        /// <summary>"attack", "scout" or "rally".</summary>
        [ProtoMember(2)] public string Kind { get; set; } = string.Empty;
        [ProtoMember(3)] public string AttackerId { get; set; } = string.Empty;
        [ProtoMember(4)] public string AttackerName { get; set; } = string.Empty;
        [ProtoMember(5)] public string AttackerTag { get; set; } = string.Empty;
        [ProtoMember(6)] public int FromX { get; set; }
        [ProtoMember(7)] public int FromY { get; set; }
        [ProtoMember(8)] public int ToX { get; set; }
        [ProtoMember(9)] public int ToY { get; set; }

        /// <summary>When the battle happens (server time). For a rally still mustering: when it is expected.</summary>
        [ProtoMember(10)] public long ArrivesAtMs { get; set; }

        /// <summary>"base" or "team".</summary>
        [ProtoMember(11)] public string TargetKind { get; set; } = string.Empty;

        /// <summary>The team under attack (TargetKind "team").</summary>
        [ProtoMember(12)] public string TargetMarchId { get; set; } = string.Empty;

        /// <summary>A rally still mustering: it sets out then.</summary>
        [ProtoMember(13)] public long DepartsAtMs { get; set; }
    }

    /// <summary>The last PvP battles and scouts (entity <c>EntityIds.WarLog</c>), newest first.</summary>
    [ProtoContract]
    public sealed class WarLogDto
    {
        [ProtoMember(1)] public PvpReportDto[] Reports { get; set; } = System.Array.Empty<PvpReportDto>();
    }

    /// <summary>One PvP battle or scout, from this player's side.</summary>
    [ProtoContract]
    public sealed class PvpReportDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public long AtMs { get; set; }

        /// <summary>"attack", "defence", "scout", "scouted", "rally", "reinforce" (fought while reinforcing), "team" (my gathering team was hit).</summary>
        [ProtoMember(3)] public string Kind { get; set; } = string.Empty;

        /// <summary>"won", "lost", "shielded", "moved", "gone", "scouted".</summary>
        [ProtoMember(4)] public string Outcome { get; set; } = string.Empty;
        [ProtoMember(5)] public string OpponentId { get; set; } = string.Empty;
        [ProtoMember(6)] public string OpponentName { get; set; } = string.Empty;
        [ProtoMember(7)] public string OpponentTag { get; set; } = string.Empty;
        [ProtoMember(8)] public int X { get; set; }
        [ProtoMember(9)] public int Y { get; set; }

        /// <summary>Resources taken (positive, attacker) or lost (defender).</summary>
        [ProtoMember(10)] public ResourceAmountEntry[] Plunder { get; set; } = System.Array.Empty<ResourceAmountEntry>();

        /// <summary>The attacking and defending armies (all teams together).</summary>
        [ProtoMember(11)] public BattleStackDto[] Attacker { get; set; } = System.Array.Empty<BattleStackDto>();
        [ProtoMember(12)] public BattleStackDto[] Defender { get; set; } = System.Array.Empty<BattleStackDto>();
        [ProtoMember(13)] public long AttackerPower { get; set; }
        [ProtoMember(14)] public long DefenderPower { get; set; }
        [ProtoMember(15)] public int Rounds { get; set; }

        /// <summary>This player's own troops: dead and wounded.</summary>
        [ProtoMember(16)] public long Dead { get; set; }
        [ProtoMember(17)] public long Wounded { get; set; }

        /// <summary>Scouts: reinforcing teams at the base.</summary>
        [ProtoMember(18)] public int Reinforcements { get; set; }

        /// <summary>A stronghold battle: its name/level; a captured region flips with it.</summary>
        [ProtoMember(19)] public string StrongholdId { get; set; } = string.Empty;
        [ProtoMember(20)] public int StrongholdLevel { get; set; }
        [ProtoMember(21)] public bool Captured { get; set; }
    }

    /// <summary>The alliance's war list (entity <c>EntityIds.AllianceWar</c>, docs/30 §5.4).</summary>
    [ProtoContract]
    public sealed class WarListDto
    {
        [ProtoMember(1)] public WarRowDto[] Rows { get; set; } = System.Array.Empty<WarRowDto>();
        [ProtoMember(2)] public long ServerNowMs { get; set; }
    }

    [ProtoContract]
    public sealed class WarRowDto
    {
        /// <summary>The leading march (attack, scout) or the rally id.</summary>
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;

        /// <summary>"attack", "scout" or "rally".</summary>
        [ProtoMember(2)] public string Kind { get; set; } = string.Empty;

        /// <summary>true = an enemy coming at a member; false = members going out.</summary>
        [ProtoMember(3)] public bool Incoming { get; set; }
        [ProtoMember(4)] public string AttackerId { get; set; } = string.Empty;
        [ProtoMember(5)] public string AttackerName { get; set; } = string.Empty;
        [ProtoMember(6)] public string AttackerTag { get; set; } = string.Empty;
        [ProtoMember(7)] public string TargetKind { get; set; } = string.Empty;
        [ProtoMember(8)] public string TargetId { get; set; } = string.Empty;
        [ProtoMember(9)] public string TargetName { get; set; } = string.Empty;
        [ProtoMember(10)] public string TargetTag { get; set; } = string.Empty;
        [ProtoMember(11)] public int ToX { get; set; }
        [ProtoMember(12)] public int ToY { get; set; }
        [ProtoMember(13)] public long ArrivesAtMs { get; set; }

        /// <summary>A rally: when it sets out (0 once it has), teams in it and the cap, and whether the viewer is in it.</summary>
        [ProtoMember(14)] public long DepartsAtMs { get; set; }
        [ProtoMember(15)] public int Teams { get; set; }
        [ProtoMember(16)] public int MaxTeams { get; set; }
        [ProtoMember(17)] public bool Joined { get; set; }
        [ProtoMember(18)] public long Power { get; set; }

        /// <summary>The base of the rally leader (joiners march there).</summary>
        [ProtoMember(19)] public int FromX { get; set; }
        [ProtoMember(20)] public int FromY { get; set; }
    }

    /// <summary>The strategic map (entity <c>EntityIds.Territory</c>, docs/30 §6.4).</summary>
    [ProtoContract]
    public sealed class TerritoryDto
    {
        [ProtoMember(1)] public StrongholdDto[] Strongholds { get; set; } = System.Array.Empty<StrongholdDto>();

        /// <summary>Every base: x, y and relation (0 mine, 1 alliance, 2 other) packed as x*1000000 + y*1000 + relation.</summary>
        [ProtoMember(2)] public int[] Bases { get; set; } = System.Array.Empty<int>();
        [ProtoMember(3)] public string MyAllianceId { get; set; } = string.Empty;
        [ProtoMember(4)] public int MapSize { get; set; }
        [ProtoMember(5)] public int MyX { get; set; }
        [ProtoMember(6)] public int MyY { get; set; }

        /// <summary>The viewer's alliance: regions held and allowed (0 without one).</summary>
        [ProtoMember(7)] public int Held { get; set; }
        [ProtoMember(8)] public int Allowed { get; set; }

        /// <summary>
        /// The alliance's rally point (docs/30 §6.3.1): its hub, or the leader's base while there is none; -1 without an
        /// alliance. The alliance teleport lands next to it.
        /// </summary>
        [ProtoMember(9)] public int RallyX { get; set; } = -1;
        [ProtoMember(10)] public int RallyY { get; set; } = -1;
        [ProtoMember(11)] public bool RallyIsHub { get; set; }
    }

    [ProtoContract]
    public sealed class StrongholdDto
    {
        /// <summary>"k" + region index (TerritoryRules).</summary>
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public int Region { get; set; }
        [ProtoMember(3)] public int X { get; set; }
        [ProtoMember(4)] public int Y { get; set; }
        [ProtoMember(5)] public int Level { get; set; }
        [ProtoMember(6)] public string AllianceId { get; set; } = string.Empty;
        [ProtoMember(7)] public string AllianceTag { get; set; } = string.Empty;

        /// <summary>Index into the alliance colour table (TerritoryRules.ColorOf); -1 unowned.</summary>
        [ProtoMember(8)] public int Color { get; set; } = -1;

        /// <summary>The owner's Alliance Hub stands here.</summary>
        [ProtoMember(9)] public bool Hub { get; set; }

        /// <summary>Garrison power now (it refills after a battle).</summary>
        [ProtoMember(10)] public long Power { get; set; }

        /// <summary>A team is on its way to it.</summary>
        [ProtoMember(11)] public bool UnderAttack { get; set; }
    }
}
