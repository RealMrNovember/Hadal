using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// UI Text that draws emoji (<see cref="Emoji.ToGlyphs"/> at mesh time; <see cref="Text.text"/> keeps the real
    /// characters). On the built-in font, which takes no bundled fallback, text with emoji switches to the bundled
    /// world font that has the emoji font as its fallback.
    /// </summary>
    public sealed class EmojiText : Text
    {
        private Font _baseFont;

        public override string text
        {
            get => base.text;
            set
            {
                base.text = value;
                PickFont();
            }
        }

        private void PickFont()
        {
            if (_baseFont == null)
                _baseFont = font;
            var wanted = _baseFont != null && _baseFont == UiKit.BuiltinFont && Emoji.Has(m_Text) ? UiKit.FontFor("ar") : _baseFont;
            if (wanted != null && font != wanted)
                font = wanted;
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            var raw = m_Text;
            m_Text = Emoji.ToGlyphs(raw);
            try
            {
                base.OnPopulateMesh(toFill);
            }
            finally
            {
                m_Text = raw;
            }
        }

        public override float preferredWidth
        {
            get
            {
                var raw = m_Text;
                m_Text = Emoji.ToGlyphs(raw);
                try
                {
                    return base.preferredWidth;
                }
                finally
                {
                    m_Text = raw;
                }
            }
        }

        public override float preferredHeight
        {
            get
            {
                var raw = m_Text;
                m_Text = Emoji.ToGlyphs(raw);
                try
                {
                    return base.preferredHeight;
                }
                finally
                {
                    m_Text = raw;
                }
            }
        }
    }
}
