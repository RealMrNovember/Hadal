using System;
using System.IO;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// A client-only stand-in march for seeing the incoming-attack alarm without anyone attacking (QA, screenshots).
    /// Started by a file only adb can write: <c>adb shell "echo attack 45 &gt; /sdcard/Android/data/&lt;package&gt;/files/alarm_demo"</c>
    /// with a kind (attack, rally, scout, shield) and the seconds until "impact". The file is removed when read; nothing
    /// is sent to the server and no battle happens.
    /// </summary>
    public static class AlarmDemo
    {
        public const string FileName = "alarm_demo";

        private static IncomingDto[] _incoming;
        private static long _endMs;
        private static float _nextPoll;

        public static bool Active => _incoming != null;

        /// <summary>The demo's shield: "shield" demos are blocked by a shield far in the future.</summary>
        public static long ShieldUntilMs { get; private set; }

        /// <summary>The stand-in marches while a demo runs, otherwise null.</summary>
        public static IncomingDto[] Poll(long nowMs)
        {
            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + 1f;
                TryReadFile(nowMs);
            }

            if (_incoming != null && nowMs > _endMs)
                Stop();
            return _incoming;
        }

        /// <summary>Starts a demo: <paramref name="kind"/> attack, rally, scout or shield, landing in <paramref name="seconds"/>.</summary>
        public static void Start(string kind, float seconds, long nowMs)
        {
            var arrives = nowMs + (long)(Mathf.Max(1f, seconds) * 1000f);
            var march = new IncomingDto
            {
                MarchId = "demo-" + nowMs,
                Kind = kind == "scout" ? PvpRules.Scout : kind == "rally" ? PvpRules.Rally : PvpRules.Attack,
                AttackerId = "demo",
                AttackerName = "Kraken",
                AttackerTag = "ABY",
                TargetKind = PvpRules.TargetBase,
                ArrivesAtMs = arrives
            };
            _incoming = new[] { march };
            ShieldUntilMs = kind == "shield" ? arrives + 3_600_000 : 0;
            _endMs = arrives + 4000;
        }

        public static void Stop()
        {
            _incoming = null;
            ShieldUntilMs = 0;
        }

        private static void TryReadFile(long nowMs)
        {
            try
            {
                var path = Path.Combine(Application.persistentDataPath, FileName);
                if (!File.Exists(path))
                    return;
                var parts = File.ReadAllText(path).Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                File.Delete(path);
                if (parts.Length == 0 || parts[0] == "stop")
                {
                    Stop();
                    return;
                }

                var seconds = parts.Length > 1 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 45f;
                Start(parts[0].ToLowerInvariant(), seconds, nowMs);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AlarmDemo] " + e.Message);
            }
        }
    }
}
