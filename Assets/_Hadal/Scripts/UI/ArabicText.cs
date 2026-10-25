using System.Collections.Generic;
using System.Text;

namespace Hadal.UI
{
    /// <summary>
    /// Arabic for UGUI's legacy Text, which neither joins letters nor lays out right to left:
    /// picks each letter's contextual presentation form (isolated / final / initial / medial,
    /// lam-alef ligatures) and reverses the line for display while keeping runs of Latin letters,
    /// digits and {0} placeholders in reading order. One line at a time; long text that UGUI wraps
    /// itself may show its lines in the wrong order, so Arabic strings are kept short.
    /// </summary>
    public static class ArabicText
    {
        // letter → isolated, final, initial, medial (0 where the form does not exist)
        private static readonly Dictionary<char, char[]> Forms = new()
        {
            ['ء'] = new[] { 'ﺀ', 'ﺀ', '\0', '\0' },
            ['آ'] = new[] { 'ﺁ', 'ﺂ', '\0', '\0' },
            ['أ'] = new[] { 'ﺃ', 'ﺄ', '\0', '\0' },
            ['ؤ'] = new[] { 'ﺅ', 'ﺆ', '\0', '\0' },
            ['إ'] = new[] { 'ﺇ', 'ﺈ', '\0', '\0' },
            ['ئ'] = new[] { 'ﺉ', 'ﺊ', 'ﺋ', 'ﺌ' },
            ['ا'] = new[] { 'ﺍ', 'ﺎ', '\0', '\0' },
            ['ب'] = new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' },
            ['ة'] = new[] { 'ﺓ', 'ﺔ', '\0', '\0' },
            ['ت'] = new[] { 'ﺕ', 'ﺖ', 'ﺗ', 'ﺘ' },
            ['ث'] = new[] { 'ﺙ', 'ﺚ', 'ﺛ', 'ﺜ' },
            ['ج'] = new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' },
            ['ح'] = new[] { 'ﺡ', 'ﺢ', 'ﺣ', 'ﺤ' },
            ['خ'] = new[] { 'ﺥ', 'ﺦ', 'ﺧ', 'ﺨ' },
            ['د'] = new[] { 'ﺩ', 'ﺪ', '\0', '\0' },
            ['ذ'] = new[] { 'ﺫ', 'ﺬ', '\0', '\0' },
            ['ر'] = new[] { 'ﺭ', 'ﺮ', '\0', '\0' },
            ['ز'] = new[] { 'ﺯ', 'ﺰ', '\0', '\0' },
            ['س'] = new[] { 'ﺱ', 'ﺲ', 'ﺳ', 'ﺴ' },
            ['ش'] = new[] { 'ﺵ', 'ﺶ', 'ﺷ', 'ﺸ' },
            ['ص'] = new[] { 'ﺹ', 'ﺺ', 'ﺻ', 'ﺼ' },
            ['ض'] = new[] { 'ﺽ', 'ﺾ', 'ﺿ', 'ﻀ' },
            ['ط'] = new[] { 'ﻁ', 'ﻂ', 'ﻃ', 'ﻄ' },
            ['ظ'] = new[] { 'ﻅ', 'ﻆ', 'ﻇ', 'ﻈ' },
            ['ع'] = new[] { 'ﻉ', 'ﻊ', 'ﻋ', 'ﻌ' },
            ['غ'] = new[] { 'ﻍ', 'ﻎ', 'ﻏ', 'ﻐ' },
            ['ف'] = new[] { 'ﻑ', 'ﻒ', 'ﻓ', 'ﻔ' },
            ['ق'] = new[] { 'ﻕ', 'ﻖ', 'ﻗ', 'ﻘ' },
            ['ك'] = new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' },
            ['ل'] = new[] { 'ﻝ', 'ﻞ', 'ﻟ', 'ﻠ' },
            ['م'] = new[] { 'ﻡ', 'ﻢ', 'ﻣ', 'ﻤ' },
            ['ن'] = new[] { 'ﻥ', 'ﻦ', 'ﻧ', 'ﻨ' },
            ['ه'] = new[] { 'ﻩ', 'ﻪ', 'ﻫ', 'ﻬ' },
            ['و'] = new[] { 'ﻭ', 'ﻮ', '\0', '\0' },
            ['ى'] = new[] { 'ﻯ', 'ﻰ', '\0', '\0' },
            ['ي'] = new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' },
        };

        // lam + (alef madda, hamza above, hamza below, alef) → isolated, final ligature
        private static readonly Dictionary<char, char[]> LamAlef = new()
        {
            ['آ'] = new[] { 'ﻵ', 'ﻶ' },
            ['أ'] = new[] { 'ﻷ', 'ﻸ' },
            ['إ'] = new[] { 'ﻹ', 'ﻺ' },
            ['ا'] = new[] { 'ﻻ', 'ﻼ' },
        };

        public static bool IsArabic(char c) => (c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= '﻿');

        public static string Shape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
                lines[i] = Visual(Join(lines[i]));
            return string.Join("\n", lines);
        }

        private static bool JoinsForward(char c) => Forms.TryGetValue(c, out var f) && f[2] != '\0';

        private static bool IsHaraka(char c) => c >= 'ً' && c <= 'ْ';

        /// <summary>Logical order, contextual forms.</summary>
        private static string Join(string line)
        {
            var output = new StringBuilder(line.Length);
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (!Forms.TryGetValue(c, out var forms))
                {
                    output.Append(c);
                    continue;
                }

                var previous = Neighbour(line, i, -1);
                var joinsBefore = previous != '\0' && JoinsForward(previous);

                if (c == 'ل')
                {
                    var next = Neighbour(line, i, +1);
                    if (LamAlef.TryGetValue(next, out var ligature))
                    {
                        output.Append(joinsBefore ? ligature[1] : ligature[0]);
                        i = line.IndexOf(next, i + 1);
                        continue;
                    }
                }

                var following = Neighbour(line, i, +1);
                var joinsAfter = forms[2] != '\0' && following != '\0' && Forms.ContainsKey(following);
                var form = joinsBefore ? (joinsAfter ? forms[3] : forms[1]) : (joinsAfter ? forms[2] : forms[0]);
                output.Append(form != '\0' ? form : forms[0]);
            }

            return output.ToString();
        }

        // Next letter in the given direction, skipping vowel marks; '\0' at the edge or a non-letter.
        private static char Neighbour(string line, int index, int step)
        {
            for (var i = index + step; i >= 0 && i < line.Length; i += step)
            {
                if (IsHaraka(line[i]))
                    continue;
                return Forms.ContainsKey(line[i]) ? line[i] : '\0';
            }

            return '\0';
        }

        private static bool IsLeftToRight(char c) =>
            (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '{' || c == '}' || (c > '¿' && c < '؀');

        /// <summary>Reverses for display; Latin/digit runs (with inner spaces, dots, colons) keep their order.</summary>
        private static string Visual(string line)
        {
            var runs = new List<string>();
            var i = 0;
            while (i < line.Length)
            {
                if (IsLeftToRight(line[i]))
                {
                    var start = i;
                    var end = i;
                    while (i < line.Length && (IsLeftToRight(line[i]) || IsLtrJoiner(line, i)))
                    {
                        if (IsLeftToRight(line[i]))
                            end = i;
                        i++;
                    }

                    runs.Add(line.Substring(start, end - start + 1));
                    i = end + 1;
                }
                else
                {
                    runs.Add(Mirror(line[i]).ToString());
                    i++;
                }
            }

            runs.Reverse();
            return string.Concat(runs);
        }

        // Spaces and number punctuation stay inside a Latin run when more Latin follows.
        private static bool IsLtrJoiner(string line, int i)
        {
            var c = line[i];
            if (c != ' ' && c != '.' && c != ':' && c != ',' && c != '/' && c != '-' && c != '+' && c != '%')
                return false;
            for (var j = i + 1; j < line.Length; j++)
            {
                if (IsLeftToRight(line[j]))
                    return true;
                if (IsArabic(line[j]))
                    return false;
            }

            return false;
        }

        private static char Mirror(char c) => c switch
        {
            '(' => ')',
            ')' => '(',
            '[' => ']',
            ']' => '[',
            '«' => '»',
            '»' => '«',
            _ => c
        };
    }
}
