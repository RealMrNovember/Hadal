using Hadal.Game.Mailbox;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Events
{
    /// <summary>One event's end-of-event mails, keyed so each goes out once (MailService.SendOnceAsync).</summary>
    public sealed record EventPayout(string EventId, List<KeyedMail> Mails);

    /// <summary>
    /// The end-of-event payout (docs/29 §2.2 "Payout"): when an event ends, every reward still owed goes out by mail in the
    /// Events tab — rank rewards (personal, or the alliance's band for each member), stage chests and sign-in days reached
    /// but not claimed, duel daily chests and the duel's week result. Nothing is claimed on the page after the end.
    /// <para>
    /// Exactly once: every mail has a fixed key ("ev:{event}:{player}:rank" …) that the mail store keeps in the same
    /// transaction as the mail, so a retry, a restart between sending and marking, or a second server sends nothing twice.
    /// Only after the mails are stored is the event marked paid (PaidOutAt, PaidOutMails: the audit trail with the keys)
    /// and the scores marked claimed, so a failure retries instead of losing rewards.
    /// </para>
    /// </summary>
    public sealed partial class EventService
    {
        public const string PayoutSender = "HADAL";

        private static readonly TimeSpan PayoutRetry = TimeSpan.FromMinutes(1);
        private readonly Dictionary<string, DateTimeOffset> _payoutRetryAt = new(StringComparer.Ordinal);

        /// <summary>
        /// The payouts that are due: published events that ended and are not paid yet (and whose scores are in memory).
        /// Each is handed out once until <see cref="ConfirmPayout"/> or <see cref="PayoutFailed"/>.
        /// </summary>
        public List<EventPayout> TakePayouts(DateTimeOffset now)
        {
            var due = new List<EventPayout>();
            lock (_gate)
            {
                foreach (var live in _events.Values.OrderBy(e => e.Def.EndsAt))
                {
                    var def = live.Def;
                    if (def.Status != EventStatus.Published || now < def.EndsAt || def.PaidOutAt != null || live.Paying
                        || def.ClaimUntil <= now.AddDays(-1) || (_payoutRetryAt.TryGetValue(def.Id, out var retry) && now < retry))
                        continue;
                    live.Paying = true;
                    if (def.Template == EventTemplates.AllianceDuel)
                        SettleDuel(live);
                    Sort(live, now, force: true);
                    due.Add(new EventPayout(def.Id, PlanPayout(live)));
                }
            }

            return due;
        }

        /// <summary>
        /// The mails of <paramref name="payout"/> were stored (<paramref name="sent"/> of them now, the rest by an earlier
        /// try): marks the event paid and every reward it held as given. Audited in the log.
        /// </summary>
        public void ConfirmPayout(EventPayout payout, int sent, DateTimeOffset now)
        {
            var eventId = payout.EventId;
            lock (_gate)
            {
                if (!_events.TryGetValue(eventId, out var live))
                    return;
                live.Paying = false;
                _payoutRetryAt.Remove(eventId);
                var ranked = payout.Mails.Where(m => m.Key.EndsWith(":rank", StringComparison.Ordinal)).Select(m => m.PlayerId).ToHashSet(StringComparer.Ordinal);
                foreach (var score in live.Scores.Values)
                {
                    MarkPaid(live.Def, score, ranked.Contains(score.PlayerId));
                    score.UpdatedAt = now;
                    _dirtyScores.Add(score);
                }

                var def = live.Def.Clone();
                def.PaidOutAt = now;
                def.PaidOutMails = sent;
                def.UpdatedAt = now;
                Replace(live, def, now);
                Flush();
                _logger.LogInformation("Event {Id} paid out: {Sent} mails.", eventId, sent);
            }
        }

        /// <summary>Sending failed: the payout is tried again a minute later (the keys keep it from doubling).</summary>
        public void PayoutFailed(string eventId, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (_events.TryGetValue(eventId, out var live))
                    live.Paying = false;
                _payoutRetryAt[eventId] = now + PayoutRetry;
            }
        }

        /// <summary>Every reward the payout gives counts as claimed from now on (the page shows it so). Caller holds the lock.</summary>
        private static void MarkPaid(EventDefinition def, EventScore score, bool ranked)
        {
            score.RankClaimed |= ranked;
            if (def.Template == EventTemplates.Login)
            {
                for (var i = 0; i < Math.Min(score.LoginDays, def.Stages.Count); i++)
                    score.Claimed.Add(i);
            }
            else if (def.Template == EventTemplates.AllianceDuel)
            {
                for (var d = 0; d < AllianceDuelRules.Days; d++)
                for (var s = 0; s < def.Stages.Count; s++)
                {
                    if (score.DayPoints[d] >= def.Stages[s].Points)
                        score.Claimed.Add(AllianceDuelRules.ChestKey(d, s));
                }
            }
            else
            {
                for (var i = 0; i < def.Stages.Count; i++)
                {
                    if (score.Points >= def.Stages[i].Points)
                        score.Claimed.Add(i);
                }
            }
        }

        /// <summary>Every mail one ended event owes. Caller holds the lock and has sorted the event.</summary>
        private List<KeyedMail> PlanPayout(Live live)
        {
            var def = live.Def;
            var name = NameArg(def);
            var mails = new List<KeyedMail>();
            foreach (var score in live.Scores.Values.OrderBy(s => s.PlayerId, StringComparer.Ordinal))
            {
                var key = $"ev:{def.Id}:{score.PlayerId}:";

                // Stages (or sign-in days, or duel daily chests) reached and never claimed.
                var waiting = new List<EventRewardConfig>();
                var count = 0;
                if (def.Template == EventTemplates.Login)
                {
                    for (var i = 0; i < Math.Min(score.LoginDays, def.Stages.Count); i++)
                    {
                        if (score.Claimed.Contains(i))
                            continue;
                        waiting.AddRange(def.Stages[i].Rewards);
                        count++;
                    }
                }
                else if (def.Template == EventTemplates.AllianceDuel)
                {
                    for (var d = 0; d < AllianceDuelRules.Days; d++)
                    for (var s = 0; s < def.Stages.Count; s++)
                    {
                        if (score.DayPoints[d] < def.Stages[s].Points || score.Claimed.Contains(AllianceDuelRules.ChestKey(d, s)))
                            continue;
                        waiting.AddRange(def.Stages[s].Rewards);
                        count++;
                    }
                }
                else
                {
                    for (var i = 0; i < def.Stages.Count; i++)
                    {
                        if (score.Points < def.Stages[i].Points || score.Claimed.Contains(i))
                            continue;
                        waiting.AddRange(def.Stages[i].Rewards);
                        count++;
                    }
                }

                if (count > 0)
                {
                    var body = def.Template == EventTemplates.AllianceDuel ? $"loc:mail.duel.chests.body|{count}" : $"loc:mail.event.stages.body|{name}|{count}";
                    Add(mails, key + "stages", score.PlayerId, $"loc:mail.event.stages.title|{name}", body, waiting);
                }

                // The rank reward: the player's place, or their alliance's in an alliance ranking.
                if (def.Template is not (EventTemplates.Login or EventTemplates.AllianceDuel) && !score.RankClaimed && score.Points > 0
                    && score.Points >= def.MinRankPoints)
                {
                    var rank = RankOf(live, score);
                    var band = BandFor(def, rank);
                    if (band != null)
                    {
                        var body = EventTemplates.RanksAlliances(def.Template)
                            ? $"loc:mail.event.allianceRank.body|{name}|{rank}|{score.Points}"
                            : $"loc:mail.event.rank.body|{name}|{rank}|{score.Points}";
                        Add(mails, key + "rank", score.PlayerId, $"loc:mail.event.rank.title|{name}", body, band.Rewards);
                    }
                }

                // The duel's week result for every member who scored for a paired alliance.
                if (def.Template == EventTemplates.AllianceDuel && score.Points > 0 && PairingOf(def, score.AllianceId) is { } pairing)
                {
                    var first = pairing.A == score.AllianceId;
                    var result = first ? pairing.AResult
                        : pairing.AResult == AllianceDuelRules.Won ? AllianceDuelRules.Lost
                        : pairing.AResult == AllianceDuelRules.Lost ? AllianceDuelRules.Won : AllianceDuelRules.Draw;
                    var rival = first ? pairing.B : pairing.A;
                    var rivalName = rival == AllianceDuelRules.Echo ? "duel.echo" : Clean($"[{(first ? pairing.BTag : pairing.ATag)}] {(first ? pairing.BName : pairing.AName)}");
                    var mine = first ? pairing.AWins : pairing.BWins;
                    var theirs = first ? pairing.BWins : pairing.AWins;
                    var rewards = score.Points < def.MinRankPoints ? new List<EventRewardConfig>()
                        : result == AllianceDuelRules.Won ? def.Duel.Win : result == AllianceDuelRules.Lost ? def.Duel.Loss : def.Duel.Draw;
                    var title = result == AllianceDuelRules.Won ? "loc:mail.duel.won.title" : result == AllianceDuelRules.Lost ? "loc:mail.duel.lost.title" : "loc:mail.duel.draw.title";
                    Add(mails, key + "duel", score.PlayerId, title, $"loc:mail.duel.result.body|{rivalName}|{mine}|{theirs}|{score.Points}", rewards, always: true);
                }
            }

            return mails;
        }

        private void Add(List<KeyedMail> mails, string key, string playerId, string title, string body, IEnumerable<EventRewardConfig> rewards, bool always = false)
        {
            var attachments = Attach(rewards, key);
            if (attachments.IsEmpty && !always)
                return;
            mails.Add(new KeyedMail(key, playerId, new MailDraft(MailKinds.Event, PayoutSender, title, body, attachments)));
        }

        /// <summary>Rewards as mail attachments: resources, bag items and hero cards (a rarity card is drawn here, by the key).</summary>
        internal MailAttachments Attach(IEnumerable<EventRewardConfig> rewards, string key)
        {
            var attachments = new MailAttachments();
            var n = 0;
            foreach (var r in rewards)
            {
                if (r == null || r.Amount <= 0)
                    continue;
                switch (r.Type)
                {
                    case "resource" when Enum.TryParse<ResourceType>(r.Id, out var resource) && resource != ResourceType.Unknown:
                        attachments.Amounts[resource.ToString()] = attachments.Amounts.GetValueOrDefault(resource.ToString()) + r.Amount;
                        break;
                    case "item" when r.Id.Length > 0:
                        attachments.Items[r.Id] = (int)Math.Min(int.MaxValue, attachments.Items.GetValueOrDefault(r.Id) + r.Amount);
                        break;
                    case "shards":
                        var hero = r.Id;
                        if (string.IsNullOrEmpty(hero))
                        {
                            var pool = Catalog.Heroes.Where(h => h.Rarity.id == r.Rarity).OrderBy(h => h.Id, StringComparer.Ordinal).ToList();
                            hero = pool.Count == 0 ? string.Empty : pool[(int)(MobilizationRules.Hash(key + "|" + n++) % (uint)pool.Count)].Id;
                        }

                        if (hero.Length > 0)
                            attachments.Shards[hero] = attachments.Shards.GetValueOrDefault(hero) + r.Amount;
                        break;
                }
            }

            return attachments;
        }

        /// <summary>
        /// The event's name as a mail argument: a localisation key for the default schedules ("event.name.depth-race"), the
        /// English (else Turkish, else any) title for the Overseer's own events.
        /// </summary>
        private static string NameArg(EventDefinition def)
        {
            if (!string.IsNullOrEmpty(def.ScheduleId) && DefaultSchedules.Ids.Contains(def.ScheduleId))
                return "event.name." + def.ScheduleId;
            var title = def.Title.TryGetValue("en", out var en) && !string.IsNullOrWhiteSpace(en) ? en
                : def.Title.TryGetValue("tr", out var tr) && !string.IsNullOrWhiteSpace(tr) ? tr
                : def.Title.Values.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? def.Id;
            return Clean(title);
        }

        /// <summary>Mail arguments are split on '|' and lines: neither may appear inside one.</summary>
        private static string Clean(string text) => text.Replace('|', '/').Replace('\n', ' ').Replace('\r', ' ').Trim();
    }
}
