using System;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Simulation;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;
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

            // The TextMeshPro fonts (Fredoka and the Noto merge) end their fallback chain with the emoji font.
            foreach (var language in new[] { "en", "ar", "ru", "ja" })
            {
                var font = UiText.FontFor(language);
                Assert.IsNotNull(font, language);
                Assert.IsTrue(font.HasCharacter(glyph, searchFallbacks: true, tryAddCharacter: true), language);
            }
        }

        [Test]
        public void Emoji_text_lays_out_an_emoji_as_a_visible_character()
        {
            var go = new GameObject("EmojiTextTest", typeof(RectTransform), typeof(Canvas)); // UGUI text builds its mesh under a canvas
            try
            {
                var label = UiKit.EmojiLabel("Label", go.transform, "a" + Smiley + "b", 40, Color.white);
                Assert.IsInstanceOf<EmojiText>(label);
                Assert.AreEqual("a" + Smiley + "b", label.text, "the real characters stay in the text");

                label.ForceMeshUpdate();
                var chars = label.textInfo.characterInfo;
                Assert.AreEqual(4, label.textInfo.characterCount, "a, the emoji glyph, its filler, b");
                // The emoji comes from the emoji font in the fallback chain and advances the pen; its filler does not.
                Assert.AreEqual(Emoji.ToGlyphs(Smiley)[0], chars[1].character);
                Assert.IsTrue(chars[1].isVisible, "emoji glyph drawn");
                StringAssert.Contains("emoji", chars[1].fontAsset.name);
                Assert.Greater(chars[1].xAdvance - chars[1].origin, 10f, "emoji glyph width");
                Assert.AreEqual(chars[2].origin, chars[2].xAdvance, 0.01f, "filler width");
                Assert.AreEqual(chars[2].xAdvance, chars[3].origin, 0.5f, "b follows the emoji");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
