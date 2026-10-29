using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;

namespace Hadal.Game
{
    /// <summary>A command this player sent recently: its id and, once answered, the answer (null while it runs).</summary>
    public sealed class CommandRecord
    {
        public string Id { get; init; } = string.Empty;
        public CommandResultCode? Result { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// The command ledger (roadmap §1, research §14 "Ağ"): a command that arrives again with the same CommandId
    /// (a re-send after a dropped connection, a duplicate envelope after a timeout, the same frame on an old and a new
    /// socket) never runs twice. The first copy runs; later copies get the first one's answer again and change nothing.
    /// The ledger lives in the player's state and is saved with it, so it holds across reconnects, unloads and restarts.
    /// </summary>
    public sealed partial class PlayerSession
    {
        /// <summary>How many recent command ids are remembered. A client re-sends only its last few unanswered commands.</summary>
        public const int CommandLedgerSize = 64;

        /// <summary>Read-only requests answer every time: a repeated one changes nothing and its answer is fresh data.</summary>
        private static bool IsReadOnly(CommandType type) => type is CommandType.ViewWorld or CommandType.ViewProfile
            or CommandType.ViewEventRanking or CommandType.ViewWar or CommandType.ViewTerritory or CommandType.ViewAllianceLog
            or CommandType.ViewAllianceRanking or CommandType.BrowseAlliances;

        /// <summary>
        /// Claims a command id before the command runs. False when this id was seen already: the command must not run
        /// again; if its answer is known it is sent again to the current connection. Commands without an id run as before.
        /// </summary>
        internal bool BeginCommand(CommandEnvelope envelope, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(envelope.CommandId) || IsReadOnly(envelope.Type))
                return true;

            lock (_gate)
            {
                var seen = _state.RecentCommands.Find(r => r.Id == envelope.CommandId);
                if (seen != null)
                {
                    if (seen.Result is { } result)
                        SendDelta(now, new List<EntityChangeDto> { AckChange(envelope.CommandId, envelope.ClientSequence, result, seen.Reason) });
                    return false;
                }

                Remember(new CommandRecord { Id = envelope.CommandId });
                return true;
            }
        }

        /// <summary>Notes a command's answer in the ledger. Caller holds the lock (every ack is built in <see cref="AckChange"/>).</summary>
        private void RecordAnswer(string commandId, CommandResultCode result, string reason)
        {
            if (string.IsNullOrEmpty(commandId))
                return;
            var record = _state.RecentCommands.Find(r => r.Id == commandId);
            if (record == null)
                Remember(record = new CommandRecord { Id = commandId });
            record.Result = result;
            record.Reason = reason ?? string.Empty;
        }

        private void Remember(CommandRecord record)
        {
            _state.RecentCommands.Add(record);
            if (_state.RecentCommands.Count > CommandLedgerSize)
                _state.RecentCommands.RemoveRange(0, _state.RecentCommands.Count - CommandLedgerSize);
            _state.IsDirty = true;
        }
    }
}
