using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Emoji for legacy UI Text. Text looks glyphs up one UTF-16 unit at a time, so surrogate pairs and emoji
    /// sequences never reach a font. The bundled Hadal Emoji font (art/ui/make_emoji_font.py, Noto Emoji, OFL) has
    /// every emoji at a Private Use Area codepoint: <see cref="ToGlyphs"/> swaps a sequence for that character plus
    /// zero-width fillers, so the string keeps its length and caret indices stay valid. Chat text on the wire stays
    /// plain Unicode.
    /// </summary>
    public static class Emoji
    {
        public const char Filler = '\uE000';

        /// <summary>The quick-pick grid next to the chat input.</summary>
        public static readonly string[] Common =
        {
            "😀", "😂", "🤣", "😊", "😍", "😘", "😎", "🤔", "😅", "😉",
            "🙂", "😢", "😭", "😡", "😱", "😴", "🤝", "👍", "👎", "👏",
            "🙏", "💪", "👋", "👌", "✌️", "❤️", "💔", "🔥", "⭐", "🎉",
            "🏆", "💯", "⚔️", "🛡️", "💣", "🚀", "🌊", "🐙", "🦈", "💎"
        };

        private sealed class Node
        {
            public Dictionary<char, Node> Next;
            public char Glyph;
        }

        private static Node _root;
        private static Font _font;

        /// <summary>The emoji font (fallback of the UI fonts; see HadalIconImporter).</summary>
        public static Font Font => _font != null ? _font : _font = Resources.Load<Font>("Fonts/HadalEmoji-Regular");

        private static Node Root
        {
            get
            {
                if (_root != null)
                    return _root;
                _root = new Node();
                var map = Resources.Load<TextAsset>("Fonts/HadalEmoji-map");
                if (map == null)
                    return _root;
                foreach (var line in map.text.Split('\n'))
                {
                    if (line.Length == 0 || line[0] == '#')
                        continue;
                    var parts = line.Trim().Split(' ');
                    if (parts.Length < 2)
                        continue;
                    var glyph = (char)System.Convert.ToInt32(parts[0], 16);
                    var node = _root;
                    for (var i = 1; i < parts.Length; i++)
                    {
                        foreach (var unit in char.ConvertFromUtf32(System.Convert.ToInt32(parts[i], 16)))
                        {
                            node.Next ??= new Dictionary<char, Node>();
                            if (!node.Next.TryGetValue(unit, out var child))
                                node.Next[unit] = child = new Node();
                            node = child;
                        }
                    }

                    if (node.Glyph == 0)
                        node.Glyph = glyph;
                }

                return _root;
            }
        }

        private static bool Invisible(char c) => c == '\u200D' || c == '\uFE0E' || c == '\uFE0F' || c == '\u20E3';

        /// <summary>Whether the text holds anything <see cref="ToGlyphs"/> would change.</summary>
        public static bool Has(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            var root = Root;
            foreach (var c in text)
            {
                if (char.IsSurrogate(c) || Invisible(c) || (c >= 0x2000 && root.Next != null && root.Next.ContainsKey(c)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The text with every emoji sequence as its font glyph followed by fillers (same length). Stray joiners,
        /// variation selectors and unknown surrogates become fillers too, so nothing draws as an empty box.
        /// </summary>
        public static string ToGlyphs(string text)
        {
            if (!Has(text))
                return text;

            var root = Root;
            var output = text.ToCharArray();
            var i = 0;
            while (i < text.Length)
            {
                // Longest match from i; FE0F inside a sequence is skipped (the map has none).
                var node = root;
                var end = -1;
                var glyph = '\0';
                for (var j = i; j < text.Length; j++)
                {
                    if (text[j] == '\uFE0F' && j > i)
                        continue;
                    if (node.Next == null || !node.Next.TryGetValue(text[j], out node))
                        break;
                    if (node.Glyph != 0)
                    {
                        end = j + 1;
                        glyph = node.Glyph;
                    }
                }

                // A trailing FE0F belongs to the emoji before it.
                while (end > 0 && end < text.Length && text[end] == '\uFE0F')
                    end++;

                if (end > i)
                {
                    output[i] = glyph;
                    for (var k = i + 1; k < end; k++)
                        output[k] = Filler;
                    i = end;
                    continue;
                }

                if (char.IsSurrogate(text[i]) || Invisible(text[i]))
                    output[i] = Filler;
                i++;
            }

            return new string(output);
        }
    }
}
