using System.Globalization;
using System.Text.Json;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Social
{
    /// <summary>
    /// Alliance polls and event time-slot votes (docs/29 §5, AlliancePollRules). Diplomacy officers and the leader open
    /// polls, war officers and the leader open time-slot votes; every member has one ballot and may change it while the
    /// vote runs. A vote closes when its time is up (<see cref="Tick"/>) or early by hand; the summary goes to alliance
    /// chat and the log, and a time-slot vote's winning time is kept for the event that asked for it
    /// (<see cref="DecidedSlot"/>). Votes live in memory and are written through to <c>hadal.alliance_polls</c>; the open
    /// ones and the last <see cref="AlliancePollRules.History"/> closed ones of each alliance are kept.
    /// </summary>
    public sealed partial class SocialHub
    {
        /// <summary>One vote as stored (JSON in hadal.alliance_polls.data).</summary>
        internal sealed class PollRecord
        {
            public string Id { get; set; } = string.Empty;
            public string AllianceId { get; set; } = string.Empty;
            public string Kind { get; set; } = AlliancePollRules.KindPoll;
            public string Question { get; set; } = string.Empty;
            public List<string> Options { get; set; } = new();
            public List<long> SlotsMs { get; set; } = new();
            public bool Multiple { get; set; }
            public bool Anonymous { get; set; }
            public string Purpose { get; set; } = string.Empty;
            public string CreatorId { get; set; } = string.Empty;
            public string CreatorName { get; set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset ClosesAt { get; set; }
            public DateTimeOffset? ClosedAt { get; set; }
            public string ClosedBy { get; set; } = string.Empty;

            /// <summary>Player id → chosen option indexes (the latest ballot).</summary>
            public Dictionary<string, int[]> Ballots { get; set; } = new(StringComparer.Ordinal);

            /// <summary>1-based winning option once closed; 0 when nobody voted.</summary>
            public int Winner { get; set; }

            /// <summary>A closed time-slot vote's fixed time (Unix ms, server time).</summary>
            public long WinnerAtMs { get; set; }

            public int OptionCount => Kind == AlliancePollRules.KindSlots ? SlotsMs.Count : Options.Count;

            public bool IsOpen => ClosedAt == null;
        }

        /// <summary>A decided time slot an event step can read (docs/29 §5).</summary>
        public sealed record DecidedTimeSlot(string PollId, string Purpose, DateTimeOffset At, DateTimeOffset DecidedAt);

        private readonly Dictionary<string, List<PollRecord>> _polls = new(StringComparer.Ordinal);
        private DateTimeOffset _nextPollSweep = DateTimeOffset.MinValue;
        private DateTimeOffset _nextLeaderSweep = DateTimeOffset.MinValue;

        /// <summary>How often expired votes are closed and leaders checked for inactivity.</summary>
        private static readonly TimeSpan PollSweep = TimeSpan.FromSeconds(15);

        private static readonly TimeSpan LeaderSweep = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Alliance mail (leader handover notice, docs/29 §5): recipients, title and body as "loc:key|arg|…". Called under
        /// the hub lock; the receiver must not call back into the hub synchronously.
        /// </summary>
        public Action<IReadOnlyList<string>, string, string>? MailTo { get; set; }

        private static readonly JsonSerializerOptions PollJson = new(JsonSerializerDefaults.Web);

        private async Task LoadPollsAsync(CancellationToken cancellationToken)
        {
            var stored = await _store.LoadPollsAsync(cancellationToken);
            var orphans = new List<string>();
            lock (_gate)
            {
                foreach (var json in stored)
                {
                    PollRecord? poll;
                    try
                    {
                        poll = JsonSerializer.Deserialize<PollRecord>(json, PollJson);
                    }
                    catch (JsonException)
                    {
                        continue; // a damaged vote is skipped rather than stopping the server
                    }

                    if (poll == null || poll.Id.Length == 0)
                        continue;
                    if (!_alliances.ContainsKey(poll.AllianceId))
                    {
                        orphans.Add(poll.AllianceId);
                        continue;
                    }

                    poll.Ballots = new Dictionary<string, int[]>(poll.Ballots ?? new Dictionary<string, int[]>(), StringComparer.Ordinal);
                    PollsOf(poll.AllianceId).Add(poll);
                }

                foreach (var list in _polls.Values)
                    list.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
            }

            foreach (var allianceId in orphans.Distinct(StringComparer.Ordinal))
                await _store.DeletePollsOfAllianceAsync(allianceId, cancellationToken);
            _logger.LogInformation("Social hub: {Polls} alliance votes.", stored.Count - orphans.Count);
        }

        private List<PollRecord> PollsOf(string allianceId)
        {
            if (!_polls.TryGetValue(allianceId, out var list))
                _polls[allianceId] = list = new List<PollRecord>();
            return list;
        }

        private void SavePoll(PollRecord poll)
        {
            var json = JsonSerializer.Serialize(poll, PollJson);
            var (id, allianceId, open, created) = (poll.Id, poll.AllianceId, poll.IsOpen, poll.CreatedAt);
            Write(ct => _store.SavePollAsync(id, allianceId, open, created, json, ct));
        }

        private string RoleOf(Alliance alliance, string playerId) =>
            alliance.Members.FirstOrDefault(m => m.PlayerId == playerId)?.Role ?? string.Empty;

        /// <summary>Whether a member may open (and close) votes of <paramref name="kind"/>.</summary>
        private bool MayRun(Alliance alliance, string playerId, string kind)
        {
            var rank = RankOf(alliance, playerId);
            var role = RoleOf(alliance, playerId);
            return kind == AlliancePollRules.KindSlots ? AlliancePermissions.CanRunEvents(rank, role) : AlliancePermissions.CanRunPolls(rank, role);
        }

        // --- Commands -----------------------------------------------------------------------------

        private SocialRejectReason CreatePoll(PlayerSession session, CreateAlliancePollCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var kind = command.Kind ?? string.Empty;
            if (!AlliancePollRules.IsKind(kind))
                return SocialRejectReason.InvalidPoll;
            if (!MayRun(alliance, session.PublicId, kind))
                return SocialRejectReason.NotAllowed;

            var slots = kind == AlliancePollRules.KindSlots;
            var question = AlliancePollRules.Clean(command.Question, AlliancePollRules.MaxQuestion);
            var options = slots ? new List<string>() : (command.Options ?? Array.Empty<string>()).Select(o => AlliancePollRules.Clean(o, AlliancePollRules.MaxOption)).ToList();
            var times = slots ? (command.SlotsMs ?? Array.Empty<long>()).OrderBy(t => t).ToList() : new List<long>();
            var hours = AlliancePollRules.HoursOrDefault(kind, command.Hours);
            var purpose = slots ? command.Purpose ?? string.Empty : string.Empty;
            var verdict = AlliancePollRules.Check(kind, question, options, times, hours, purpose, now.ToUnixTimeMilliseconds());
            if (verdict != SocialRejectReason.None)
                return verdict;
            var list = PollsOf(alliance.Record.Id);
            if (list.Count(p => p.IsOpen) >= AlliancePollRules.MaxOpen)
                return SocialRejectReason.TooManyPolls;

            var poll = new PollRecord
            {
                Id = "v" + Guid.NewGuid().ToString("N"),
                AllianceId = alliance.Record.Id,
                Kind = kind,
                Question = question,
                Options = options,
                SlotsMs = times,
                Multiple = command.Multiple,
                Anonymous = !slots && command.Anonymous,
                Purpose = purpose,
                CreatorId = session.PublicId,
                CreatorName = session.DisplayName,
                CreatedAt = now,
                ClosesAt = now.AddHours(hours)
            };
            list.Add(poll);
            SavePoll(poll);
            SystemMessage(alliance, slots ? "sys.slots" : "sys.poll", Arg(session.DisplayName) + "|" + (slots ? PurposeKey(purpose) : question), now);
            PushPolls(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason Vote(PlayerSession session, VoteAlliancePollCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var poll = PollsOf(alliance.Record.Id).FirstOrDefault(p => p.Id == command.PollId);
            if (poll == null)
                return SocialRejectReason.UnknownPoll;
            if (!poll.IsOpen || poll.ClosesAt <= now)
                return SocialRejectReason.PollClosed;
            var choices = command.Choices ?? Array.Empty<int>();
            if (!AlliancePollRules.ValidBallot(choices, poll.OptionCount, poll.Multiple))
                return SocialRejectReason.InvalidVote;

            poll.Ballots[session.PublicId] = choices.OrderBy(c => c).ToArray();
            SavePoll(poll);
            PushPolls(alliance, now);
            return SocialRejectReason.None;
        }

        private SocialRejectReason ClosePoll(PlayerSession session, CloseAlliancePollCommand command, DateTimeOffset now)
        {
            var alliance = AllianceOf(session.PublicId);
            if (alliance == null)
                return SocialRejectReason.NotInAlliance;
            var poll = PollsOf(alliance.Record.Id).FirstOrDefault(p => p.Id == command.PollId);
            if (poll == null)
                return SocialRejectReason.UnknownPoll;
            if (!poll.IsOpen)
                return SocialRejectReason.PollClosed;
            if (poll.CreatorId != session.PublicId && !MayRun(alliance, session.PublicId, poll.Kind))
                return SocialRejectReason.NotAllowed;
            Finish(alliance, poll, AlliancePollRules.ClosedByOfficer, now);
            return SocialRejectReason.None;
        }

        /// <summary>Counts the ballots, fixes the winner, posts the summary and trims the history. Caller holds the lock.</summary>
        private void Finish(Alliance alliance, PollRecord poll, string by, DateTimeOffset now)
        {
            var counts = AlliancePollRules.Tally(poll.OptionCount, poll.Ballots.Values);
            poll.ClosedAt = now;
            poll.ClosedBy = by;
            poll.Winner = AlliancePollRules.Winner(counts);
            poll.WinnerAtMs = poll.Kind == AlliancePollRules.KindSlots && poll.Winner > 0 ? poll.SlotsMs[poll.Winner - 1] : 0;
            SavePoll(poll);

            var voters = poll.Ballots.Count;
            if (poll.Kind == AlliancePollRules.KindSlots)
            {
                var purpose = PurposeKey(poll.Purpose);
                if (poll.Winner > 0)
                    SystemMessage(alliance, "sys.slotsend", $"{purpose}|{ServerTime(poll.WinnerAtMs)}|{counts[poll.Winner - 1]}|{voters}", now);
                else
                    SystemMessage(alliance, "sys.slotsnone", purpose, now);
            }
            else if (poll.Winner > 0)
                SystemMessage(alliance, "sys.pollend", $"{poll.Question}|{poll.Options[poll.Winner - 1]}|{counts[poll.Winner - 1]}|{voters}", now);
            else
                SystemMessage(alliance, "sys.pollnone", poll.Question, now);

            TrimPolls(alliance.Record.Id);
            PushPolls(alliance, now);
        }

        /// <summary>Keeps the open votes and the newest closed ones. Caller holds the lock.</summary>
        private void TrimPolls(string allianceId)
        {
            var list = PollsOf(allianceId);
            foreach (var old in list.Where(p => !p.IsOpen).OrderByDescending(p => p.ClosedAt).Skip(AlliancePollRules.History).ToList())
            {
                list.Remove(old);
                var id = old.Id;
                Write(ct => _store.DeletePollAsync(id, ct));
            }
        }

        private static string PurposeKey(string purpose) => "alliance.poll.purpose." + (string.IsNullOrEmpty(purpose) ? AlliancePollRules.Purposes[0] : purpose);

        /// <summary>A time in server time (UTC) as chat shows it: "2026-11-02 20:00 UTC".</summary>
        internal static string ServerTime(long ms) =>
            DateTimeOffset.FromUnixTimeMilliseconds(ms).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";

        /// <summary>A name inside a "key|arg|…" notice: the separator cannot appear in it.</summary>
        private static string Arg(string text) => (text ?? string.Empty).Replace('|', '/');

        // --- Housekeeping ------------------------------------------------------------------------

        /// <summary>
        /// Closes votes whose time is up (every 15 s) and hands an inactive leader's alliance over (every 10 min).
        /// Called from the game tick.
        /// </summary>
        public void Tick(DateTimeOffset now)
        {
            lock (_gate)
            {
                if (now >= _nextPollSweep)
                {
                    _nextPollSweep = now + PollSweep;
                    foreach (var (allianceId, list) in _polls.ToList())
                    {
                        if (!_alliances.TryGetValue(allianceId, out var alliance))
                            continue;
                        foreach (var due in list.Where(p => p.IsOpen && p.ClosesAt <= now).ToList())
                            Finish(alliance, due, AlliancePollRules.ClosedByTime, now);
                    }
                }

                if (now >= _nextLeaderSweep)
                {
                    _nextLeaderSweep = now + LeaderSweep;
                    foreach (var alliance in _alliances.Values.ToList())
                        CheckLeader(alliance, now);
                }
            }
        }

        /// <summary>
        /// A leader offline <see cref="AlliancePermissions.InactiveLeaderDays"/> days hands over to the most powerful
        /// active officer (docs/29 §5); the alliance hears it in chat and by mail. A leader whose last visit is unknown
        /// (stored before it was recorded) starts the clock now. Caller holds the lock.
        /// </summary>
        private void CheckLeader(Alliance alliance, DateTimeOffset now)
        {
            var leaderId = alliance.Record.LeaderId;
            var leader = alliance.Members.FirstOrDefault(m => m.PlayerId == leaderId);
            if (leader == null || alliance.Members.Count < 2)
                return;
            if (_online.ContainsKey(leaderId))
            {
                // An online leader's visit is kept fresh, so a restart (no disconnect) cannot make them look long gone.
                if (leader.LastSeen is not { } last || now - last > TimeSpan.FromHours(1))
                    SetMember(alliance, leader with { LastSeen = now });
                return;
            }
            if (leader.LastSeen is not { } seen)
            {
                SetMember(alliance, leader with { LastSeen = now });
                return;
            }

            var days = AlliancePermissions.InactiveLeaderDays;
            if (now - seen < TimeSpan.FromDays(days))
                return;

            var active = TimeSpan.FromDays(AlliancePermissions.ActiveHeirDays);
            var heirId = AlliancePermissions.InactiveLeaderHeir(alliance.Members.Select(m =>
            {
                var online = _online.TryGetValue(m.PlayerId, out var s);
                return (m.PlayerId, m.Rank, online ? s!.Power : m.Power, online || (m.LastSeen is { } last && now - last <= active));
            }).ToList(), leaderId);
            if (heirId == null)
                return;

            var heir = alliance.Members.First(m => m.PlayerId == heirId);
            var officers = alliance.Members.Count(m => m.Rank == AlliancePermissions.Officer && m.PlayerId != heirId);
            SetMember(alliance, heir with { Rank = AlliancePermissions.Leader, Role = string.Empty });
            SetMember(alliance, leader with
            {
                Rank = officers < AlliancePermissions.MaxOfficers ? AlliancePermissions.Officer : AlliancePermissions.Veteran, Role = string.Empty
            });
            alliance.Record = alliance.Record with { LeaderId = heirId };
            var record = alliance.Record;
            Write(ct => _store.SaveAllianceAsync(record, ct));

            var (oldName, newName) = (Arg(NameOf(leaderId) is { Length: > 0 } o ? o : leader.Name), Arg(NameOf(heirId) is { Length: > 0 } n ? n : heir.Name));
            SystemMessage(alliance, "sys.inactive", $"{oldName}|{newName}|{days}", now);
            MailTo?.Invoke(alliance.Members.Select(m => m.PlayerId).ToList(), "loc:mail.alliance.leader.title",
                $"loc:mail.alliance.leader.body|{oldName}|{newName}|{days}");
            _logger.LogInformation("Alliance {AllianceId}: leader {Old} inactive {Days} days, {New} leads now.", alliance.Record.Id, leaderId, days, heirId);
            PushAlliance(alliance, now);
        }

        // --- Views -------------------------------------------------------------------------------

        private EntityChangeDto PollsChange(string playerId, DateTimeOffset now)
        {
            var alliance = AllianceOf(playerId);
            var polls = alliance == null || !_polls.TryGetValue(alliance.Record.Id, out var list)
                ? Array.Empty<AlliancePollDto>()
                : list.Where(p => p.IsOpen).OrderBy(p => p.ClosesAt)
                    .Concat(list.Where(p => !p.IsOpen).OrderByDescending(p => p.ClosedAt))
                    .Select(p => PollDto(p, playerId)).ToArray();
            return new EntityChangeDto
            {
                EntityId = EntityIds.AlliancePolls,
                Kind = EntityChangeKind.Update,
                Payload = _serializer.Serialize(new AlliancePollsDto { Polls = polls })
            };
        }

        private AlliancePollDto PollDto(PollRecord p, string viewerId)
        {
            var counts = AlliancePollRules.Tally(p.OptionCount, p.Ballots.Values);
            var options = new AlliancePollOptionDto[p.OptionCount];
            for (var i = 0; i < options.Length; i++)
            {
                var index = i;
                options[i] = new AlliancePollOptionDto
                {
                    Text = p.Kind == AlliancePollRules.KindSlots ? string.Empty : p.Options[i],
                    AtMs = p.Kind == AlliancePollRules.KindSlots ? p.SlotsMs[i] : 0,
                    Votes = counts[i],
                    VoterNames = p.Anonymous
                        ? Array.Empty<string>()
                        : p.Ballots.Where(b => b.Value.Contains(index)).Select(b => NameOf(b.Key)).Where(n => n.Length > 0).OrderBy(n => n, StringComparer.Ordinal).ToArray()
                };
            }

            return new AlliancePollDto
            {
                Id = p.Id,
                Kind = p.Kind,
                Question = p.Question,
                Options = options,
                Multiple = p.Multiple,
                Anonymous = p.Anonymous,
                CreatorId = p.CreatorId,
                CreatorName = NameOf(p.CreatorId) is { Length: > 0 } n ? n : p.CreatorName,
                CreatedAtMs = p.CreatedAt.ToUnixTimeMilliseconds(),
                ClosesAtMs = p.ClosesAt.ToUnixTimeMilliseconds(),
                ClosedAtMs = p.ClosedAt?.ToUnixTimeMilliseconds() ?? 0,
                Voters = p.Ballots.Count,
                MyVotes = p.Ballots.TryGetValue(viewerId, out var mine) ? mine : Array.Empty<int>(),
                Purpose = p.Purpose,
                Winner = p.Winner,
                WinnerAtMs = p.WinnerAtMs,
                ClosedBy = p.ClosedBy
            };
        }

        private void PushPolls(Alliance alliance, DateTimeOffset now)
        {
            foreach (var member in alliance.Members)
            {
                if (_online.TryGetValue(member.PlayerId, out var session))
                    session.Push(new List<EntityChangeDto> { PollsChange(member.PlayerId, now) }, now);
            }
        }

        /// <summary>A disbanded alliance's votes go, in memory and in the store. Caller holds the lock.</summary>
        private void ForgetPolls(string allianceId)
        {
            if (_polls.Remove(allianceId))
                Write(ct => _store.DeletePollsOfAllianceAsync(allianceId, ct));
        }

        // --- Event steps and the Overseer --------------------------------------------------------

        /// <summary>
        /// The newest decided time of an alliance's time-slot votes for <paramref name="purpose"/> (docs/29 §5), or null.
        /// Later event steps (Lure Trap, Trench Conquest) read their start from here.
        /// </summary>
        public DecidedTimeSlot? DecidedSlot(string allianceId, string purpose)
        {
            lock (_gate)
            {
                return _polls.TryGetValue(allianceId ?? string.Empty, out var list)
                    ? list.Where(p => p.Kind == AlliancePollRules.KindSlots && p.Purpose == purpose && !p.IsOpen && p.Winner > 0)
                        .OrderByDescending(p => p.ClosedAt)
                        .Select(p => new DecidedTimeSlot(p.Id, p.Purpose, DateTimeOffset.FromUnixTimeMilliseconds(p.WinnerAtMs), p.ClosedAt!.Value))
                        .FirstOrDefault()
                    : null;
            }
        }

        /// <summary>One vote as the Overseer sees it.</summary>
        public sealed record PollSummary(string Id, string AllianceId, string AllianceTag, string AllianceName, string Kind, string Question,
            string Purpose, string CreatorName, DateTimeOffset CreatedAt, DateTimeOffset ClosesAt, DateTimeOffset? ClosedAt, string ClosedBy,
            bool Multiple, bool Anonymous, int Voters, IReadOnlyList<(string Text, long AtMs, int Votes)> Options, int Winner);

        /// <summary>Every alliance's votes (read-only), newest first; <paramref name="query"/> filters by tag or name.</summary>
        public List<PollSummary> PollsForOverseer(string? query)
        {
            var q = (query ?? string.Empty).Trim();
            lock (_gate)
            {
                return _polls.Where(p => _alliances.ContainsKey(p.Key))
                    .Select(p => (Alliance: _alliances[p.Key], Polls: p.Value))
                    .Where(x => q.Length == 0 || x.Alliance.Record.Tag.Contains(q, StringComparison.OrdinalIgnoreCase)
                                || x.Alliance.Record.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(x => x.Polls.Select(p =>
                    {
                        var counts = AlliancePollRules.Tally(p.OptionCount, p.Ballots.Values);
                        var options = Enumerable.Range(0, p.OptionCount).Select(i =>
                            (p.Kind == AlliancePollRules.KindSlots ? string.Empty : p.Options[i], p.Kind == AlliancePollRules.KindSlots ? p.SlotsMs[i] : 0L, counts[i])).ToList();
                        return new PollSummary(p.Id, x.Alliance.Record.Id, x.Alliance.Record.Tag, x.Alliance.Record.Name, p.Kind, p.Question, p.Purpose,
                            p.CreatorName, p.CreatedAt, p.ClosesAt, p.ClosedAt, p.ClosedBy, p.Multiple, p.Anonymous, p.Ballots.Count, options, p.Winner);
                    }))
                    .OrderByDescending(p => p.ClosedAt == null).ThenByDescending(p => p.CreatedAt)
                    .ToList();
            }
        }

        /// <summary>The Overseer closes an open vote early (the summary still goes to chat). False when not found or closed.</summary>
        public bool CloseByOverseer(string pollId, DateTimeOffset now)
        {
            lock (_gate)
            {
                var (alliance, poll) = FindPoll(pollId);
                if (alliance == null || poll == null || !poll.IsOpen)
                    return false;
                Finish(alliance, poll, AlliancePollRules.ClosedByOverseer, now);
                return true;
            }
        }

        /// <summary>The Overseer removes an abusive vote without a trace in chat. False when not found.</summary>
        public bool DeletePoll(string pollId, DateTimeOffset now)
        {
            lock (_gate)
            {
                var (alliance, poll) = FindPoll(pollId);
                if (alliance == null || poll == null)
                    return false;
                PollsOf(alliance.Record.Id).Remove(poll);
                var id = poll.Id;
                Write(ct => _store.DeletePollAsync(id, ct));
                PushPolls(alliance, now);
                return true;
            }
        }

        private (Alliance? Alliance, PollRecord? Poll) FindPoll(string pollId)
        {
            foreach (var (allianceId, list) in _polls)
            {
                var poll = list.FirstOrDefault(p => p.Id == pollId);
                if (poll != null && _alliances.TryGetValue(allianceId, out var alliance))
                    return (alliance, poll);
            }

            return (null, null);
        }
    }
}
