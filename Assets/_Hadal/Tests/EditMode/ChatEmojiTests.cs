using System;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Simulation;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hadal.Tests.EditMode
{
    /// <summary>Emoji in chat: the sanitiser keeps them, the glyph mapping and the bundled emoji font draw them.</summary>
    public sealed class ChatEmojiTests
    {
        private static string Cp(params int[] codepoints) => string.Concat(codepoints.Select(char.ConvertFromUtf32));

        private static readonly string Smiley = Cp(0x1F642);
        private static readonly string Heart = Cp(0x2764, 0xFE0F);
        private static readonly string Family = Cp(0x1F468, 0x200D, 0x1F469, 0x200D, 0x1F467);
        private static readonly string Flag = Cp(0x1F1F9, 0x1F1F7);

        private static bool IsGlyph(char c) => c > Emoji.Filler && c <= (char)0xF8FF;

        [Test]
        public void Sanitiser_keeps_emoji_and_drops_control_characters()
        {
            var rules = new ChatCatalogData { maxLength = 200 };
            Assert.AreEqual($"emoji test {Smiley}", SocialRules.CleanMessage($"  emoji\ttest {Smiley}\n", rules));
            Assert.AreEqual($"{Heart} {Family} {Flag}", SocialRules.CleanMessage($"{Heart} {Family} {Flag}", rules));
            // Half an emoji cannot travel as UTF-8.
            Assert.AreEqual("ab", SocialRules.CleanMessage("a" + Smiley[0] + "b", rules));
        }

        [Test]
        public void Sanitiser_counts_an_emoji_as_one_character_and_never_splits_it()
        {
            var rules = new ChatCatalogData { maxLength = 10 };
            Assert.AreEqual(1, SocialRules.TextLength(Smiley));
            var cleaned = SocialRules.CleanMessage(new string('a', 9) + Smiley + Smiley, rules);
            Assert.AreEqual(new string('a', 9) + Smiley, cleaned);
            // Ten emoji fit a ten-character limit although they are twenty UTF-16 units.
            var ten = string.Concat(Enumerable.Repeat(Smiley, 10));
            Assert.AreEqual(ten, SocialRules.CleanMessage(ten + "x", rules));
        }

        [Test]
        public void Glyph_mapping_keeps_the_length_and_draws_one_glyph_per_emoji()
        {
            Assert.AreSame("plain text", Emoji.ToGlyphs("plain text"));
            Assert.AreEqual("#1", Emoji.ToGlyphs("#1"), "digits and '#' stay text");

            foreach (var emoji in new[] { Smiley, Heart, Family, Flag, Cp(0x23, 0xFE0F, 0x20E3), Cp(0x1F44D, 0x1F3FD) })
            {
                var glyphs = Emoji.ToGlyphs("a" + emoji + "b");
                Assert.AreEqual(emoji.Length + 2, glyphs.Length);
                Assert.AreEqual('a', glyphs[0]);
                Assert.IsTrue(IsGlyph(glyphs[1]), $"no glyph for U+{char.ConvertToUtf32(emoji, 0):X}");
                Assert.IsTrue(glyphs.Skip(2).Take(emoji.Length - 1).All(c => c == Emoji.Filler));
                Assert.AreEqual('b', glyphs[glyphs.Length - 1]);
            }

            // An emoji the font lacks, or a stray joiner, leaves no box behind.
            Assert.AreEqual("x" + Emoji.Filler + Emoji.Filler, Emoji.ToGlyphs("x" + Cp(0x10FFFD)));
            Assert.AreEqual(Smiley.Length + 1, Emoji.ToGlyphs(Cp(0x200D) + Smiley).Length);
        }

        [Test]
        public void Quick_pick_emoji_all_have_glyphs()
        {
            Assert.GreaterOrEqual(Emoji.Common.Length, 40);
            foreach (var emoji in Emoji.Common)
                Assert.IsTrue(IsGlyph(Emoji.ToGlyphs(emoji)[0]), emoji);
        }

        [Test]
        public void Ui_fonts_fall_back_to_the_bundled_emoji_font()
        {
            var emojiFont = Emoji.Font;
            Assert.IsNotNull(emojiFont);
            var glyph = Emoji.ToGlyphs(Smiley)[0];
            Assert.IsTrue(emojiFont.HasCharacter(glyph));

            foreach (var path in new[] { "Fonts/Fredoka-SemiBold", "Fonts/HadalSansWorld-Regular" })
            {
                var font = Resources.Load<Font>(path);
                Assert.IsNotNull(font, path);
                CollectionAssert.Contains(font.fontNames, "Hadal Emoji", path);
            }
        }

        [Test]
        public void Emoji_text_lays_out_an_emoji_as_a_visible_character()
        {
            var go = new GameObject("EmojiTextTest", typeof(RectTransform));
            try
            {
                var label = go.AddComponent<EmojiText>();
                label.font = Resources.Load<Font>("Fonts/Fredoka-SemiBold");
                label.fontSize = 40;
                label.text = "a" + Smiley + "b";
                Assert.AreEqual("a" + Smiley + "b", label.text, "the real characters stay in the text");

                var settings = label.GetGenerationSettings(new Vector2(1000, 200));
                var generator = new TextGenerator();
                generator.Populate(Emoji.ToGlyphs(label.text), settings);
                var chars = generator.characters;
                Assert.GreaterOrEqual(chars.Count, 4);
                // The emoji advances the pen (it is drawn); its filler does not.
                Assert.Greater(chars[1].charWidth, 10f, "emoji glyph width");
                Assert.AreEqual(0f, chars[2].charWidth, 0.01f, "filler width");

                // Without the mapping, legacy Text has nothing to draw for a surrogate pair.
                var raw = new TextGenerator();
                raw.Populate("a" + Smiley + "b", settings);
                Debug.Log($"[Hadal] raw surrogate widths: {raw.characters[1].charWidth} {raw.characters[2].charWidth}; mapped: {chars[1].charWidth}");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
