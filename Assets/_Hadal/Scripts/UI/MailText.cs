using System;
using System.Linq;

namespace Hadal.UI
{
    /// <summary>
    /// The game's own mails (event boss prizes and the like) carry their text as a localisation key with arguments,
    /// "loc:key|arg|…", so each player reads them in their language. An argument that is itself a key ("monster.chasm_maw")
    /// is translated too. Player mails are always shown as written.
    /// </summary>
    public static class MailText
    {
        public const string Prefix = "loc:";

        public static string Show(string text, string kind)
        {
            if (string.IsNullOrEmpty(text) || kind == "player" || !text.StartsWith(Prefix, StringComparison.Ordinal))
                return text ?? string.Empty;
            var parts = text.Substring(Prefix.Length).Split('|');
            var args = parts.Skip(1).Select(a => (object)(a.Contains('.') && Loc.Has(a) ? Loc.T(a)
                : long.TryParse(a, out var n) && n >= 1000 ? n.ToString("N0") : a)).ToArray();
            return args.Length == 0 ? Loc.T(parts[0]) : Loc.F(parts[0], args);
        }
    }
}
