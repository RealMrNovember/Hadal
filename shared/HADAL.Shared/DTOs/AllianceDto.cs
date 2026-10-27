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

        /// <summary><c>AllianceJoinMode</c> value.</summary>
        [ProtoMember(8)] public int JoinMode { get; set; }

        /// <summary>Waiting applications; filled only for R3 and above.</summary>
        [ProtoMember(9)] public AllianceApplicantDto[] Applicants { get; set; } = System.Array.Empty<AllianceApplicantDto>();
    }

    [ProtoContract]
    public sealed class AllianceApplicantDto
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public long Power { get; set; }
    }

    [ProtoContract]
    public sealed class AllianceMemberDto
    {
        [ProtoMember(1)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public bool IsLeader { get; set; }
        [ProtoMember(4)] public long Power { get; set; }
        [ProtoMember(5)] public bool Online { get; set; }

        /// <summary>1 (Recruit) to 5 (Leader), see <c>AlliancePermissions</c>.</summary>
        [ProtoMember(6)] public int Rank { get; set; }

        /// <summary>Officer duty tag ("war", "logistics", "diplomacy", "scout") or empty.</summary>
        [ProtoMember(7)] public string Role { get; set; } = string.Empty;
    }

    /// <summary>Alliance invitations for a player without an alliance, and where they applied (entity <c>EntityIds.AllianceInvites</c>).</summary>
    [ProtoContract]
    public sealed class AllianceInviteListDto
    {
        [ProtoMember(1)] public AllianceInviteDto[] Invites { get; set; } = System.Array.Empty<AllianceInviteDto>();
        [ProtoMember(2)] public string[] Applied { get; set; } = System.Array.Empty<string>();
    }

    [ProtoContract]
    public sealed class AllianceInviteDto
    {
        [ProtoMember(1)] public string AllianceId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Name { get; set; } = string.Empty;
        [ProtoMember(3)] public string Tag { get; set; } = string.Empty;
        [ProtoMember(4)] public string InviterName { get; set; } = string.Empty;
        [ProtoMember(5)] public int Members { get; set; }
        [ProtoMember(6)] public int MaxMembers { get; set; }
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

        /// <summary><c>AllianceJoinMode</c> value.</summary>
        [ProtoMember(9)] public int JoinMode { get; set; }
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
