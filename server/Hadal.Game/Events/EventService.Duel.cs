using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Events
{
    /// <summary>An alliance that could enter the duel: its kingdom, power and size at pairing time.</summary>
    public sealed record DuelEntrant(string Id, string Kingdom, long Power, int Members, string Tag, string Name);

    /// <summary>
    /// Alliance Duel (docs/29 §4.3, AllianceDuelRules): pairing when the week goes live, day points per alliance, the
    /// members' daily chests and the week result. Only alliances paired this week score; the day themes are the event's
    /// six phases, so the points table is the Overseer's like any other event.
    /// </summary>
    public sealed partial class EventService
    {
        /// <summary>Every alliance with its kingdom, power and members (set by the world to SocialHub.DuelEntrants).</summary>
        public Func<IReadOnlyList<DuelEntrant>>? DuelEntrants { get; set; }

        /// <summary>Pairs every duel that is live and not paired yet; returns how many were paired. Caller holds the lock.</summary>
        private int PairDuels(DateTimeOffset now)
        {
            var paired = 0;
            foreach (var live in _events.Values)
            {
                var def = live.Def;
                if (def.Template != EventTemplates.AllianceDuel || def.Status != EventStatus.Published || def.DuelState.PairedAt != null
                    || PhaseOf(def, now) != EventPhase.Live)
                    continue;
                PairDuel(live, now);
                paired++;
            }

            return paired;
        }

        /// <summary>Pairs one duel by power inside each kingdom it covers (AllianceDuelRules.Pair). Caller holds the lock.</summary>
        private void PairDuel(Live live, DateTimeOffset now)
        {
            var def = live.Def;
            var entrants = (DuelEntrants?.Invoke() ?? Array.Empty<DuelEntrant>())
                .Where(e => e.Members >= def.Duel.MinMembers && (def.Kingdoms.Count == 0 || def.Kingdoms.Contains(e.Kingdom, StringComparer.Ordinal)))
                .ToList();
            var byId = entrants.ToDictionary(e => e.Id, StringComparer.Ordinal);
            var pairings = new List<DuelPairing>();
            foreach (var kingdom in entrants.GroupBy(e => e.Kingdom, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                foreach (var pair in AllianceDuelRules.Pair(kingdom.Select(e => new KeyValuePair<string, long>(e.Id, e.Power))))
                {
                    var a = byId[pair.Key];
                    byId.TryGetValue(pair.Value, out var b);
                    pairings.Add(new DuelPairing
                    {
                        Kingdom = kingdom.Key, A = a.Id, APower = a.Power, ATag = a.Tag, AName = a.Name,
                        B = pair.Value, BPower = b?.Power ?? 0, BTag = b?.Tag ?? string.Empty, BName = b?.Name ?? string.Empty
                    });
                }
            }

            var copy = def.Clone();
            copy.DuelState = new DuelState { PairedAt = now, Pairings = pairings };
            Replace(live, copy, now);
            _logger.LogInformation("Duel {Id}: {Count} pairings over {Alliances} alliances.", def.Id, pairings.Count, entrants.Count);
        }

        private static DuelPairing? PairingOf(EventDefinition def, string allianceId) =>
            allianceId.Length == 0 ? null : def.DuelState.Pairings.FirstOrDefault(p => p.A == allianceId || p.B == allianceId);

        /// <summary>
        /// Scores the running day's actions for a player whose alliance is paired: personal week and day points, and the
        /// alliance's total and day points. Caller holds the lock.
        /// </summary>
        private bool RecordDuel(Live live, string playerId, string allianceId, List<(string Action, long Amount)> actions, DateTimeOffset now)
        {
            if (PairingOf(live.Def, allianceId) == null)
                return false;
            var nowMs = now.ToUnixTimeMilliseconds();
            var day = AllianceDuelRules.DayAt(live.Shape, nowMs);
            if (day < 0)
                return false;
            var active = live.Shape.Phases[day].Actions;
            var score = ScoreOf(live, playerId, create: false);
            long gained = 0;
            foreach (var (action, amount) in actions)
            {
                if (Array.Find(active, a => a.Action == action) == null)
                    continue;
                score ??= ScoreOf(live, playerId, create: true)!;
                var carry = score.Carry.TryGetValue(action, out var c) ? c : 0;
                gained += EventRules.Score(active, action, amount, ref carry);
                score.Carry[action] = carry;
                _dirtyScores.Add(score);
            }

            if (score == null || gained <= 0)
                return false;
            score.DayPoints[day] += gained;
            AddPoints(live, score, allianceId, gained, now);
            var state = StateOf(live, allianceId);
            state.DayPoints[day] += gained;
            StateChanged(live, allianceId);
            return true;
        }

        /// <summary>
        /// One side's points per day; for the Abyss Echo the average of every real alliance of the pairing's kingdom
        /// (days not started stay 0). Caller holds the lock.
        /// </summary>
        private long[] DayPointsOf(Live live, DuelPairing pairing, string side, int daysStarted)
        {
            if (side != AllianceDuelRules.Echo)
                return live.States.TryGetValue(side, out var state) ? state.DayPoints.ToArray() : new long[AllianceDuelRules.Days];
            var real = live.Def.DuelState.Pairings.Where(p => p.Kingdom == pairing.Kingdom)
                .SelectMany(p => new[] { p.A, p.B }).Where(id => id != AllianceDuelRules.Echo).Distinct(StringComparer.Ordinal).ToList();
            var echo = new long[AllianceDuelRules.Days];
            for (var d = 0; d < Math.Min(daysStarted, AllianceDuelRules.Days); d++)
                echo[d] = AllianceDuelRules.EchoScore(real.Select(id => live.States.TryGetValue(id, out var s) ? s.DayPoints[d] : 0));
            return echo;
        }

        private int DaysStarted(Live live, long nowMs)
        {
            var done = AllianceDuelRules.DaysDone(live.Shape, nowMs);
            var today = AllianceDuelRules.DayAt(live.Shape, nowMs);
            return Math.Max(done, today + 1);
        }

        private DuelSideDto Side(Live live, DuelPairing pairing, bool first, int daysStarted)
        {
            var id = first ? pairing.A : pairing.B;
            var info = id == AllianceDuelRules.Echo ? null : AllianceInfo?.Invoke(id);
            return new DuelSideDto
            {
                AllianceId = id,
                Tag = info?.Tag ?? (first ? pairing.ATag : pairing.BTag),
                Name = info?.Name ?? (first ? pairing.AName : pairing.BName),
                Emblem = info?.Emblem ?? string.Empty,
                Power = first ? pairing.APower : pairing.BPower,
                DayPoints = DayPointsOf(live, pairing, id, daysStarted)
            };
        }

        /// <summary>The player's view of the duel: both banners, the days, their own day points and chests. Caller holds the lock.</summary>
        private DuelDto DuelView(Live live, string playerId, EventScore? score, DateTimeOffset now)
        {
            var nowMs = now.ToUnixTimeMilliseconds();
            var allianceId = score != null && score.AllianceId.Length > 0 && PairingOf(live.Def, score.AllianceId) != null
                ? score.AllianceId : AllianceIdOf?.Invoke(playerId) ?? string.Empty;
            var dto = new DuelDto
            {
                Pairings = live.Def.DuelState.Pairings.Count,
                MyDayPoints = score?.DayPoints.ToArray() ?? new long[AllianceDuelRules.Days],
                ClaimedChests = score?.Claimed.OrderBy(i => i).ToArray() ?? Array.Empty<int>(),
                History = History(live.Def, allianceId)
            };
            var pairing = PairingOf(live.Def, allianceId);
            if (pairing == null)
                return dto;

            var started = DaysStarted(live, nowMs);
            var first = pairing.A == allianceId;
            var a = Side(live, pairing, true, started);
            var b = Side(live, pairing, false, started);
            var done = AllianceDuelRules.DaysDone(live.Shape, nowMs);
            AllianceDuelRules.WeekPoints(a.DayPoints, b.DayPoints, done, out var aWins, out var bWins);
            a.Wins = aWins;
            b.Wins = bWins;
            dto.InDuel = true;
            dto.Mine = first ? a : b;
            dto.Rival = first ? b : a;
            dto.DayWinners = Enumerable.Range(0, AllianceDuelRules.Days).Select(d =>
            {
                if (d >= done)
                    return AllianceDuelRules.Pending;
                var winner = AllianceDuelRules.DayWinner(dto.Mine.DayPoints[d], dto.Rival.DayPoints[d]);
                return winner;
            }).ToArray();
            if (nowMs >= live.Shape.EndsAtMs)
                dto.Result = AllianceDuelRules.WeekResult(dto.Mine.DayPoints, dto.Rival.DayPoints);
            return dto;
        }

        /// <summary>The alliance's earlier duels in this kingdom, newest first (finished ones only). Caller holds the lock.</summary>
        private DuelHistoryDto[] History(EventDefinition current, string allianceId)
        {
            if (allianceId.Length == 0)
                return Array.Empty<DuelHistoryDto>();
            return _events.Values.Select(e => e.Def)
                .Where(d => d.Template == EventTemplates.AllianceDuel && d.Id != current.Id && d.PaidOutAt != null && d.StartsAt < current.StartsAt)
                .Select(d => (Def: d, Pairing: PairingOf(d, allianceId)))
                .Where(x => x.Pairing != null)
                .OrderByDescending(x => x.Def.StartsAt)
                .Take(5)
                .Select(x =>
                {
                    var p = x.Pairing!;
                    var first = p.A == allianceId;
                    var result = first ? p.AResult : p.AResult == AllianceDuelRules.Won ? AllianceDuelRules.Lost : p.AResult == AllianceDuelRules.Lost ? AllianceDuelRules.Won : p.AResult;
                    return new DuelHistoryDto
                    {
                        StartsAtMs = x.Def.StartsAt.ToUnixTimeMilliseconds(),
                        RivalTag = first ? p.BTag : p.ATag,
                        RivalName = first ? p.BName : p.AName,
                        MyWins = first ? p.AWins : p.BWins,
                        RivalWins = first ? p.BWins : p.AWins,
                        Result = result
                    };
                }).ToArray();
        }

        /// <summary>
        /// Fills each pairing's result once the week is over (Payout calls it before the mails are planned). Caller holds
        /// the lock; the definition is written with the payout mark.
        /// </summary>
        private void SettleDuel(Live live)
        {
            foreach (var p in live.Def.DuelState.Pairings)
            {
                var a = DayPointsOf(live, p, p.A, AllianceDuelRules.Days);
                var b = DayPointsOf(live, p, p.B, AllianceDuelRules.Days);
                AllianceDuelRules.WeekPoints(a, b, AllianceDuelRules.Days, out var aWins, out var bWins);
                p.AWins = aWins;
                p.BWins = bWins;
                p.AResult = AllianceDuelRules.WeekResult(a, b);
            }
        }

        /// <summary>The Overseer's live view of a duel: every pairing with both sides' day points and week points.</summary>
        public sealed record DuelPairingView(string Kingdom, DuelSideDto A, DuelSideDto B, int[] DayWinners);

        public List<DuelPairingView> DuelPairings(string eventId, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!_events.TryGetValue(eventId ?? string.Empty, out var live) || live.Def.Template != EventTemplates.AllianceDuel)
                    return new List<DuelPairingView>();
                var nowMs = now.ToUnixTimeMilliseconds();
                var started = DaysStarted(live, nowMs);
                var done = AllianceDuelRules.DaysDone(live.Shape, nowMs);
                return live.Def.DuelState.Pairings.Select(p =>
                {
                    var a = Side(live, p, true, started);
                    var b = Side(live, p, false, started);
                    AllianceDuelRules.WeekPoints(a.DayPoints, b.DayPoints, done, out var aWins, out var bWins);
                    a.Wins = aWins;
                    b.Wins = bWins;
                    var winners = Enumerable.Range(0, AllianceDuelRules.Days)
                        .Select(d => d >= done ? AllianceDuelRules.Pending : AllianceDuelRules.DayWinner(a.DayPoints[d], b.DayPoints[d])).ToArray();
                    return new DuelPairingView(p.Kingdom, a, b, winners);
                }).ToList();
            }
        }

        /// <summary>Pairs a live duel now (the Overseer, e.g. after alliances were missing at the start). Only before anyone scored.</summary>
        public string RepairDuel(string eventId, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!_events.TryGetValue(eventId ?? string.Empty, out var live) || live.Def.Template != EventTemplates.AllianceDuel)
                    return "UnknownEvent";
                if (PhaseOf(live.Def, now) != EventPhase.Live)
                    return "NotLive";
                if (live.States.Values.Any(s => s.DayPoints.Any(p => p > 0)))
                    return "AlreadyScoring";
                PairDuel(live, now);
                return string.Empty;
            }
        }
    }
}
