using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HADAL.Shared.Auth
{
    /// <summary>
    /// Keeps profanity out of player names. Names are folded before matching (case, Turkish letters, look-alike
    /// digits and symbols, separators between letters), so "S.i_k" and "$1K" are caught as well. Long stems match
    /// anywhere in the folded name; short ones only as a whole word, so ordinary names that merely contain the
    /// letters ("Ambiyans", "Sikke") stay allowed.
    /// </summary>
    public static class NameFilter
    {
        // Matched anywhere in the folded name.
        private static readonly string[] Stems =
        {
            // Turkish
            "orospu", "orosbu", "siktir", "sikerim", "sikeyim", "sikik", "amcik", "aminakoy", "aminakoyim", "yarrak",
            "pezevenk", "kahpe", "gavat", "ibne", "puşt", "götveren", "ananısikim", "sürtük", "kaltak", "yavşak",
            "dalyarak", "taşak",
            // English
            "fuck", "shit", "bitch", "cunt", "whore", "slut", "nigger", "nigga", "faggot", "pussy", "asshole",
            "motherfucker", "hitler",
        };

        // Matched only as a whole word of the name.
        private static readonly string[] Words =
            { "sik", "am", "amk", "aq", "got", "pic", "oc", "mk", "yarak", "pust", "fag", "ass", "cock", "dick", "rape", "nazi" };

        public static bool IsProfane(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            // Words are split on spaces only; dots, dashes and underscores inside a word are folded away ("S.i_k").
            var words = name!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(Fold).ToArray();
            if (words.Any(w => Words.Contains(w)))
                return true;
            var joined = Fold(name);
            return Stems.Select(Fold).Any(s => s.Length > 0 && joined.Contains(s));
        }

        /// <summary>Lower case, Turkish and accented letters to plain ASCII, look-alikes to letters, everything else dropped.</summary>
        internal static string Fold(string text)
        {
            var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;
                var mapped = c switch
                {
                    'ı' => 'i',
                    '0' => 'o',
                    '1' or '!' or '|' => 'i',
                    '3' => 'e',
                    '4' or '@' => 'a',
                    '5' or '$' => 's',
                    '7' => 't',
                    '8' => 'b',
                    _ => c
                };
                if (mapped >= 'a' && mapped <= 'z')
                    sb.Append(mapped);
            }

            return sb.ToString();
        }
    }
}
