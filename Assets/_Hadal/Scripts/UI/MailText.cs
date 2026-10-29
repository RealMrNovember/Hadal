using System;
using System.Linq;

namespace Hadal.UI
{
    /// <summary>
    /// The game's own mails (event boss prizes and the like) carry their text as a localisation key with arguments,
    /// "loc:key|arg|…", so each player reads them in their language. An argument that is itself a key ("monster.chasm_maw")
    /// is translated too. A text may hold several such lines (battle reports, docs/30 §5.5). Player mails are always shown
    /// as written.
    /// </summary>
    public static class MailText
    {
        public const string Prefix = "loc:";

        public static string Show(string text, string kind)
        {
            if (string.IsNullOrEmpty(text) || kind == "player" || !text.StartsWith(Prefix, StringComparison.Ordinal))
                return text ?? string.Empty;
            if (text.Contains("\n" + Prefix))
                return string.Join("\n", text.Split('\n').Select(line => Show(line, kind)));
            var parts = text.Substring(Prefix.Length).Split('|');
            var args = parts.Skip(1).Select(a => (object)(a.Contains('.') && Loc.Has(a) ? Loc.T(a)
                : long.TryParse(a, out var n) && n >= 1000 ? n.ToString("N0") : a)).ToArray();
            return args.Length == 0 ? Loc.T(parts[0]) : Loc.F(parts[0], args);
        }

        /// <summary>The battle a battle-report mail is about (its "mail.pvp.id" or "mail.pvp.ref" line): id and report kind.</summary>
        public static bool BattleRef(string body, out string battleId, out string reportKind)
        {
            battleId = reportKind = string.Empty;
            foreach (var line in (body ?? string.Empty).Split('\n'))
            {
                if (!line.StartsWith(Prefix + "mail.pvp.id|", StringComparison.Ordinal) && !line.StartsWith(Prefix + "mail.pvp.ref|", StringComparison.Ordinal))
                    continue;
                var parts = line.Split('|');
                if (parts.Length < 3)
                    continue;
                (battleId, reportKind) = (parts[1], parts[2]);
                return true;
            }

            return false;
        }
    }
}
