using HADAL.Shared.Commands;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Social
{
    /// <summary>The alliance hub's header (docs/31 §1): the announcement officers write and the alliance language.</summary>
    public sealed partial class SocialHub
    {
        /// <summary>Officers set the announcement; the leader may change the language too. Caller holds the lock.</summary>
        private SocialRejectReason SetInfo(string playerId, SetAllianceInfoCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(playerId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var rank = RankOf(alliance, playerId);
            if (!AlliancePermissions.CanEditAnnouncement(rank))
                return SocialRejectReason.NotAllowed;

            var record = alliance.Record with
            {
                Announcement = SocialRules.CleanAnnouncement(command.Announcement),
                Language = rank == AlliancePermissions.Leader && SocialRules.IsLanguageCode(command.Language) ? command.Language : alliance.Record.Language
            };
            alliance.Record = record;
            Write(ct => _store.SaveAllianceAsync(record, ct));
            PushAlliance(alliance, now);
            return SocialRejectReason.None;
        }

        /// <summary>Waits until every queued store write is done (tests).</summary>
        internal async Task DrainAsyncForTests()
        {
            var done = new TaskCompletionSource();
            Write(_ =>
            {
                done.SetResult();
                return Task.CompletedTask;
            });
            await done.Task;
        }
    }
}
