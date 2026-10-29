using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Sends the repair crew to the running emergency (docs/35 §4): pays the repair cost and starts the repair timer,
    /// which alliance mates can help (HelpKinds.Repair).
    /// </summary>
    [ProtoContract]
    public sealed class RepairEmergencyCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The emergency being repaired (SurvivalStateDto.EmergencyId); a stale id is refused.</summary>
        [ProtoMember(3)] public string EmergencyId { get; set; } = string.Empty;
    }
}
