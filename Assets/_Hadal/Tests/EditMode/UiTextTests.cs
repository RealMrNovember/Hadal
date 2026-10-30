using System.Collections.Generic;
using System.Linq;
using Hadal.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hadal.Tests.EditMode
{
    /// <summary>TextMeshPro text: the font chain draws every language, outlines are shared presets, labels keep their layout.</summary>
    public sealed class UiTextTests
    {
        // Han, kana and Hangul come from the phone's CJK font (not on the Linux build server); everything else is bundled.
        private static bool FromPhone(char c) =>
            c is >= '⺀' and <= '鿿' or >= '가' and <= '힯' or >= '豈' and <= '﫿' or >= '＀' and <= '￯'
                or >= 'ᄀ' and <= 'ᇿ' or >= '㄰' and <= '㆏';

        private static bool Invisible(char c) => char.IsControl(c) || c is ' ' or '​' or '‌' or '‍' or '‎' or '‏' or ' ';

        // Arrows, stars and other symbols come from the phone's symbol fonts, as they did for the legacy fonts.
        private static bool Symbol(char c) => char.GetUnicodeCategory(c) is System.Globalization.UnicodeCategory.MathSymbol
            or System.Globalization.UnicodeCategory.OtherSymbol;

        [Test]
        public void Every_language_draws_with_the_bundled_fonts()
        {
            var english = Loc.Parse(Resources.Load<TextAsset>("Localization/en").text);
            var previous = Loc.Current;
            try
            {
                foreach (var (code, _) in Loc.Languages)
                {
                    Loc.Use(code);
                    var font = UiText.FontFor(code);
                    Assert.IsNotNull(font, code);
                    var text = new HashSet<char>();
                    foreach (var key in english.Keys)
                        text.UnionWith(Loc.T(key));
                    text.UnionWith(Loc.DisplayName(code));
                    var needed = new string(text.Where(c => !Invisible(c) && !FromPhone(c) && !Symbol(c) && !char.IsSurrogate(c)).ToArray());
                    font.HasCharacters(needed, out uint[] missing, true, true);
                    missing ??= System.Array.Empty<uint>();
                    Assert.IsEmpty(missing, $"{code}: no glyph for {string.Join(" ", missing.Select(c => $"U+{c:X4}"))}");
                }
            }
            finally
            {
                Loc.Use(previous);
            }
        }

        [Test]
        public void Outlines_are_shared_presets_that_survive_a_font_change()
        {
            var root = new GameObject("UiTextTest", typeof(RectTransform));
            try
            {
                var a = UiKit.Stroked(UiKit.Label("A", root.transform, "Hadal", 30, Color.white));
                var b = UiKit.Stroked(UiKit.Label("B", root.transform, "Abyss", 40, Color.white));
                var plain = UiKit.Label("C", root.transform, "Plain", 30, Color.white);

                Assert.IsTrue(UiText.HasOutline(a));
                Assert.AreSame(a.fontSharedMaterial, b.fontSharedMaterial, "one material per style, not per label");
                Assert.IsFalse(UiText.HasOutline(plain));
                Assert.AreSame(plain.font.material, plain.fontSharedMaterial);

                UiText.SetFont(a, UiText.FontFor("ar"));
                Assert.AreSame(UiText.FontFor("ar"), a.font);
                Assert.IsTrue(UiText.HasOutline(a), "the outline follows the label to the new font");
                Assert.AreSame(a.font.material.mainTexture, a.fontSharedMaterial.mainTexture, "the preset draws from the new font's atlas");

                UiText.Plain(a);
                Assert.IsFalse(UiText.HasOutline(a));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Labels_keep_the_legacy_layout_rules()
        {
            var root = new GameObject("UiTextTest", typeof(RectTransform));
            try
            {
                var label = UiKit.Label("L", root.transform, "Hadal", UiKit.TypeScale.Body, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
                Assert.IsInstanceOf<TextMeshProUGUI>(label);
                Assert.AreEqual(Mathf.RoundToInt(UiKit.TypeScale.Body * UiKit.Scale), label.fontSize, 0.01f);
                Assert.AreEqual(TextAlignmentOptions.TopRight, label.alignment);
                Assert.AreEqual(FontStyles.Bold, label.fontStyle);
                Assert.AreEqual(TextWrappingModes.Normal, label.textWrappingMode);
                Assert.IsFalse(label.raycastTarget);
                Assert.Greater(label.preferredWidth, 10f, "measurable for layout");

                var button = UiKit.Button("B", root.transform, "OK", null);
                var caption = button.GetComponentInChildren<TMP_Text>();
                Assert.IsTrue(caption.enableAutoSizing, "long translations shrink to fit the button");

                var input = UiKit.Input("I", root.transform, "hint");
                Assert.IsInstanceOf<EmojiText>(input.textComponent);
                Assert.IsFalse(input.richText, "typed tags stay literal");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
