using TMPro;

namespace Hadal.UI
{
    /// <summary>
    /// A TextMeshPro label that draws emoji: what it lays out is <see cref="Emoji.ToGlyphs"/> (each emoji sequence as
    /// the bundled emoji font's glyph plus zero-width fillers, same length), while <see cref="text"/> keeps the real
    /// characters. Every UI font has the emoji font in its fallback chain (<see cref="UiText"/>), and the equal length
    /// keeps TMP_InputField's caret and selection indices valid when this is an input's text component.
    /// </summary>
    public sealed class EmojiText : TextMeshProUGUI
    {
        private string _raw = string.Empty;

        public override string text
        {
            get => _raw;
            set
            {
                _raw = value ?? string.Empty;
                base.text = Emoji.ToGlyphs(_raw);
            }
        }
    }
}
