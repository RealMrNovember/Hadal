using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Sets the alliance announcement shown on the hub (officers, R4 and up); the leader may also set the alliance
    /// language. An empty <see cref="Language"/> leaves it as it is.
    /// </summary>
    [ProtoContract]
    public sealed class SetAllianceInfoCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string Announcement { get; set; } = string.Empty;

        /// <summary>A UI language code ("tr", "en", "zh-Hans", ...).</summary>
        [ProtoMember(4)] public string Language { get; set; } = string.Empty;
    }
}
