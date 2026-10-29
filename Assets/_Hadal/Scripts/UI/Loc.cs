using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// UI strings in the 18 launch languages (the same set as Last Asylum: Plague). Each language is
    /// a "key = value" text file in Resources/Localization; English fills any key a translation lacks.
    /// The choice is stored on the device; the first launch follows the phone's language.
    /// </summary>
    public static class Loc
    {
        public const string Fallback = "en";
        private const string PrefKey = "hadal.language";

        /// <summary>Code (file name) and the language's own name, in picker order.</summary>
        public static readonly (string Code, string Name)[] Languages =
        {
            ("en", "English"),
            ("tr", "Türkçe"),
            ("de", "Deutsch"),
            ("fr", "Français"),
            ("es", "Español"),
            ("it", "Italiano"),
            ("pt", "Português"),
            ("nl", "Nederlands"),
            ("ru", "Русский"),
            ("ar", "العربية"),
            ("ja", "日本語"),
            ("ko", "한국어"),
            ("zh-Hans", "简体中文"),
            ("zh-Hant", "繁體中文"),
            ("th", "ไทย"),
            ("vi", "Tiếng Việt"),
            ("id", "Bahasa Indonesia"),
            ("ms", "Bahasa Melayu")
        };

        private static Dictionary<string, string> _strings;
        private static Dictionary<string, string> _fallback;
        private static string _current;

        /// <summary>Active language code.</summary>
        public static string Current
        {
            get
            {
                if (_current == null)
                    Use(PlayerPrefs.GetString(PrefKey, string.Empty) is { Length: > 0 } saved ? saved : FromSystem(Application.systemLanguage));
                return _current;
            }
        }

        public static bool IsRightToLeft => Current == "ar";

        /// <summary>Switches language for this session and remembers it on the device.</summary>
        public static void SetLanguage(string code)
        {
            Use(code);
            PlayerPrefs.SetString(PrefKey, _current);
            PlayerPrefs.Save();
        }

        /// <summary>Switches language for this session only (tests).</summary>
        public static void Use(string code)
        {
            _current = Array.Exists(Languages, l => l.Code == code) ? code : Fallback;
            _fallback ??= Load(Fallback);
            _strings = _current == Fallback ? _fallback : Load(_current);
        }

        public static string NameOf(string code) => Array.Find(Languages, l => l.Code == code).Name ?? code;

        /// <summary><see cref="NameOf"/> ready for UGUI (Arabic joined and laid out right to left).</summary>
        public static string DisplayName(string code) => code == "ar" ? ArabicText.Shape(NameOf(code)) : NameOf(code);

        public static bool Has(string key)
        {
            _ = Current;
            return _strings.ContainsKey(key) || _fallback.ContainsKey(key);
        }

        public static string T(string key)
        {
            _ = Current;
            if (!_strings.TryGetValue(key, out var value) && !_fallback.TryGetValue(key, out value))
                return key;

            return IsRightToLeft ? ArabicText.Shape(value) : value;
        }

        /// <summary><see cref="T"/> with string.Format arguments; arguments are shaped with the text.</summary>
        public static string F(string key, params object[] args)
        {
            _ = Current;
            if (!_strings.TryGetValue(key, out var value) && !_fallback.TryGetValue(key, out value))
                return key;

            var text = string.Format(value, args);
            return IsRightToLeft ? ArabicText.Shape(text) : text;
        }

        /// <summary>
        /// Text for the phone's own UI (notifications, channel names): never shaped, since Android lays out Arabic itself.
        /// </summary>
        public static string Plain(string key, params object[] args)
        {
            _ = Current;
            if (!_strings.TryGetValue(key, out var value) && !_fallback.TryGetValue(key, out value))
                return key;
            return args == null || args.Length == 0 ? value : string.Format(value, args);
        }

        /// <summary>Player-facing text for a server reject reason (PlacementRejectReason / ConstructionRejectReason).</summary>
        public static string Reason(string code) => Has("reason." + code) ? T("reason." + code) : F("error.generic", code);

        public static string Building(string id) => Has("building." + id) ? T("building." + id) : id;

        public static string Resource(string type) => Has("resource." + type) ? T("resource." + type) : type;

        /// <summary>m:ss under an hour, h:mm:ss above.</summary>
        public static string Duration(float seconds)
        {
            var t = TimeSpan.FromSeconds(Mathf.Max(0f, Mathf.Ceil(seconds)));
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
        }

        public static string Error(string code) => Has("error." + code) ? T("error." + code) : F("error.generic", code);

        /// <summary>Phone language → our code; anything we do not ship falls back to English.</summary>
        public static string FromSystem(SystemLanguage language) => language switch
        {
            SystemLanguage.Turkish => "tr",
            SystemLanguage.German => "de",
            SystemLanguage.French => "fr",
            SystemLanguage.Spanish => "es",
            SystemLanguage.Italian => "it",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.Dutch => "nl",
            SystemLanguage.Russian => "ru",
            SystemLanguage.Arabic => "ar",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Korean => "ko",
            SystemLanguage.Chinese or SystemLanguage.ChineseSimplified => "zh-Hans",
            SystemLanguage.ChineseTraditional => "zh-Hant",
            SystemLanguage.Thai => "th",
            SystemLanguage.Vietnamese => "vi",
            SystemLanguage.Indonesian => "id",
            _ => Fallback
        };

        /// <summary>Parses one language file: "key = value", '#' comments, "\n" for line breaks.</summary>
        public static Dictionary<string, string> Parse(string text)
        {
            var strings = new Dictionary<string, string>();
            foreach (var raw in (text ?? string.Empty).Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                if (line.Length == 0 || line[0] == '#')
                    continue;

                var split = line.IndexOf(" = ", StringComparison.Ordinal);
                if (split <= 0)
                    continue;

                strings[line.Substring(0, split).Trim()] = line.Substring(split + 3).Replace("\\n", "\n");
            }

            return strings;
        }

        private static Dictionary<string, string> Load(string code)
        {
            var asset = Resources.Load<TextAsset>("Localization/" + code);
            if (asset != null)
                return Parse(asset.text);

            Debug.LogWarning($"[Loc] Resources/Localization/{code}.txt missing.");
            return new Dictionary<string, string>();
        }
    }
}
