using System.Collections.Concurrent;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Game.Mailbox;

namespace Hadal.Game.World
{
    /// <summary>
    /// Every PvP report also goes out as a mail (docs/30 §5.5): result, both armies, losses, loot with the protected share
    /// and fatigue, contribution, coordinates and the battle id the client opens the result screen with. The text is
    /// "loc:key|args" lines so each player reads it in their language, and the report itself rides along as data (docs/37).
    /// One mail per player, battle and report kind.
    /// </summary>
    public sealed partial class WorldService
    {
        private const int MailedMemory = 5000;
        private const int MaxStacksInMail = 6;

        private readonly ConcurrentQueue<(string PlayerId, MailDraft Draft)> _battleMails = new();
        private readonly HashSet<string> _mailed = new(StringComparer.Ordinal);
        private readonly Queue<string> _mailedOrder = new();

        /// <summary>The battle being resolved, for where its attack came from. Caller holds the world lock.</summary>
        private PvpRecord? _resolving;

        /// <summary>The next battle mail to send; the game world sends them outside the world lock.</summary>
        public bool TryTakeBattleMail(out string playerId, out MailDraft draft)
        {
            if (_battleMails.TryDequeue(out var next))
            {
                (playerId, draft) = next;
                return true;
            }

            (playerId, draft) = (string.Empty, null!);
            return false;
        }

        /// <summary>Queues the report's mail once. Caller holds the world lock.</summary>
        private void MailReport(string playerId, PvpReportDto report)
        {
            var key = $"{report.Id}|{playerId}|{report.Kind}";
            if (playerId.Length == 0 || !_mailed.Add(key))
                return;
            _mailedOrder.Enqueue(key);
            if (_mailedOrder.Count > MailedMemory)
                _mailed.Remove(_mailedOrder.Dequeue());
            _battleMails.Enqueue((playerId, BattleMail(report)));
        }

        internal static MailDraft BattleMail(PvpReportDto r)
        {
            var who = Who(r);
            var title = $"loc:mail.pvp.{r.Outcome}|pvp.kind.{r.Kind}|{Clip(who, 36)}";
            var lines = new List<string>();
            var hit = r.Kind is "defence" or "team" or "reinforce";
            if (r.Kind == "scouted")
                lines.Add(Line("mail.pvp.scoutedBy", who, r.FromX, r.FromY));
            else if (hit && r.OpponentName.Length > 0)
                lines.Add(Line("mail.pvp.from", who, r.FromX, r.FromY));
            else
                lines.Add(Line("mail.pvp.target", who));
            lines.Add(Line("mail.pvp.at", r.X, r.Y));
            // A shield up at the arrival instant: no battle, no losses, the army sails home.
            if (r.Outcome == "shielded")
                lines.Add(hit ? Line("mail.pvp.blocked", who) : Line("mail.pvp.bounced"));

            if (r.Rounds > 0)
            {
                lines.Add(Line("mail.pvp.powers", r.AttackerPower, r.DefenderPower, r.Rounds));
                Army("report.attacker", r.Attacker);
                Army("report.defender", r.Defender);
                lines.Add(Line("mail.pvp.losses", r.Dead, r.Wounded));
            }

            if (r.Kind == "scout")
            {
                lines.Add(Line("mail.pvp.scout", r.DefenderPower, r.Reinforcements));
                Army("report.defender", r.Defender);
            }

            var loot = (r.Plunder ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0).ToList();
            if (loot.Count > 0)
            {
                lines.Add(Line(r.Kind == "scout" ? "mail.pvp.loot.scout" : hit ? "mail.pvp.loot.lost" : "mail.pvp.loot.won"));
                lines.AddRange(loot.Select(p => Line(hit ? "mail.pvp.minus" : "mail.pvp.plus", p.Value, "resource." + p.Key)));
            }
            else if (r.Rounds > 0 && r.Outcome == "won" && r.Kind is "attack" or "rally")
                lines.Add(Line("mail.pvp.loot.none"));

            var kept = (r.Protected ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0).ToList();
            if (kept.Count > 0)
            {
                lines.Add(Line("mail.pvp.protected"));
                lines.AddRange(kept.Select(p => Line("mail.pvp.amount", p.Value, "resource." + p.Key)));
            }

            if (r.PlunderShare < 100 && r.Kind is "attack" or "rally")
                lines.Add(Line("mail.pvp.share", r.PlunderShare));
            if (r.Contribution > 0 && r.Contribution < 100)
                lines.Add(Line("mail.pvp.contribution", r.Contribution));
            if (r.Captured)
                lines.Add(Line("mail.pvp.captured"));
            // The battle id opens the result screen; the seed and rule version make it reproducible (docs/33 §4).
            lines.Add(r.Rounds > 0 ? Line("mail.pvp.id", r.Id, r.Kind, "#" + r.Seed, r.RuleVersion) : Line("mail.pvp.ref", r.Id, r.Kind));

            var body = string.Join("\n", lines);
            if (body.Length > MailRules.MaxBody)
                body = string.Join("\n", lines.Where(l => !l.StartsWith("loc:mail.pvp.stack", StringComparison.Ordinal)));
            // The report travels with the mail so the mailbox draws a report card (docs/37); the text stays for older clients.
            return new MailDraft(MailKinds.Battle, string.Empty, title, body, new MailAttachments(), Report: r);

            void Army(string side, BattleStackDto[]? stacks)
            {
                var all = (stacks ?? Array.Empty<BattleStackDto>()).OrderByDescending(s => s.Start).ToList();
                if (all.Count == 0)
                    return;
                lines.Add(Line("mail.pvp.side", side));
                lines.AddRange(all.Take(MaxStacksInMail).Select(s => Line("mail.pvp.stack", "troop." + s.Type, s.Tier, s.Start, s.Lost)));
                if (all.Count > MaxStacksInMail)
                    lines.Add(Line("mail.pvp.more", all.Count - MaxStacksInMail));
            }
        }

        /// <summary>The other side as a name ("[TAG] Name") or a place key (a city, a building, a beacon).</summary>
        private static string Who(PvpReportDto r)
        {
            if (r.StrongholdId.Length > 0)
                return AllianceLandRules.IsBuilding(r.StrongholdId) ? "bld." + r.StrongholdId
                    : r.StrongholdId == AllianceLandRules.Beacon ? "land.beacon" : "city.name";
            return (r.OpponentTag.Length > 0 ? $"[{r.OpponentTag}] " : string.Empty) + r.OpponentName;
        }

        private static string Line(string key, params object[] args) =>
            "loc:" + key + string.Concat(args.Select(a => "|" + (a?.ToString() ?? string.Empty).Replace('|', '/').Replace('\n', ' ')));

        private static string Clip(string text, int max) => text.Length <= max ? text : text[..max];
    }
}
