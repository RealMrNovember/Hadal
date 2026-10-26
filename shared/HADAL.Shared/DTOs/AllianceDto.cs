using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>The player's alliance (entity <c>EntityIds.MyAlliance</c>).</summary>
    [ProtoContract]
    public sealed class AllianceDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public string Tag { get; set; } = string.Empty;
        [ProtoMember(4)] public string Description { get; set; } = string.Empty;
        [ProtoMember(5)] public string LeaderId { get; set; } = string.Empty;
        [ProtoMember(6)] public AllianceMemberDto[] Members { get; set; } = System.Array.Empty<AllianceMemberDto>();
        [ProtoMember(7)] public int MaxMembers { get; set; }
    }

    [ProtoContract]
    public sealed class AllianceMemberDto
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public bool IsLeader { get; set; }
        [ProtoMember(4)] public long Power { get; set; }
        [ProtoMember(5)] public bool Online { get; set; }
    }

    /// <summary>Alliances to join (entity <c>EntityIds.AllianceList</c>).</summary>
    [ProtoContract]
    public sealed class AllianceListDto
    {
        [ProtoMember(1)] public AllianceSummaryDto[] Alliances { get; set; } = System.Array.Empty<AllianceSummaryDto>();
    }

    [ProtoContract]
    public sealed class AllianceSummaryDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public string Tag { get; set; } = string.Empty;
        [ProtoMember(4)] public int Members { get; set; }
        [ProtoMember(5)] public int MaxMembers { get; set; }
        [ProtoMember(6)] public string LeaderName { get; set; } = string.Empty;
        [ProtoMember(7)] public long Power { get; set; }
        [ProtoMember(8)] public string Description { get; set; } = string.Empty;
    }

    /// <summary>Alliance mates' constructions waiting for help (entity <c>EntityIds.AllianceHelp</c>).</summary>
    [ProtoContract]
    public sealed class HelpListDto
    {
        [ProtoMember(1)] public HelpRequestDto[] Requests { get; set; } = System.Array.Empty<HelpRequestDto>();
    }

    [ProtoContract]
    public sealed class HelpRequestDto
    {
        [ProtoMember(1)] public string RequestId { get; set; } = string.Empty;
        [ProtoMember(2)] public string OwnerId { get; set; } = string.Empty;
        [ProtoMember(3)] public string OwnerName { get; set; } = string.Empty;
        [ProtoMember(4)] public string BuildingDefinitionId { get; set; } = string.Empty;
        [ProtoMember(5)] public int TargetLevel { get; set; }
        [ProtoMember(6)] public int Helps { get; set; }
        [ProtoMember(7)] public int MaxHelps { get; set; }

        /// <summary>The viewing player already helped this one.</summary>
        [ProtoMember(8)] public bool HelpedByMe { get; set; }
    }
}
