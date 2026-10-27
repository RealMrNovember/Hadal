using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>The player's VIP standing (VipRules): points, level, sign-in streak and what can be claimed today.</summary>
    [ProtoContract]
    public sealed class VipStateDto
    {
        [ProtoMember(1)] public long Points { get; set; }
        [ProtoMember(2)] public int Level { get; set; }

        /// <summary>Days in a row the daily VIP points were claimed.</summary>
        [ProtoMember(3)] public int Streak { get; set; }

        /// <summary>Today's sign-in VIP points can be claimed (and how many).</summary>
        [ProtoMember(4)] public bool LoginReady { get; set; }
        [ProtoMember(5)] public long LoginPoints { get; set; }

        /// <summary>Today's VIP chest can be claimed.</summary>
        [ProtoMember(6)] public bool ChestReady { get; set; }

        /// <summary>Milliseconds until the next UTC day (both daily claims reset).</summary>
        [ProtoMember(7)] public long ResetInMs { get; set; }
    }
}
