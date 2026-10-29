using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Shields on the map (docs/30 §3), for everyone to see: a dome over every protected base and the time left on its
    /// plate; attack, scout and rally greyed out with the reason; our own team's menu says when its target raised one.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private const float DomeRadius = 6f;
        private readonly Dictionary<string, ShieldDome> _domes = new();
        private readonly Dictionary<string, Text> _shieldLabels = new();
        private float _nextShieldTick;

        /// <summary>The kind of protection a base has now: a shield, the recovery shield or newbie protection.</summary>
        private ShieldDome.Kind Protection(WorldBaseDto b, long now) =>
            b.ShieldUntilMs > now ? (b.Recovery ? ShieldDome.Kind.Recovery : ShieldDome.Kind.Shield)
            : PvpRules.IsBeginner(b.Level) ? ShieldDome.Kind.Beginner : ShieldDome.Kind.None;

        private void UpdateShields()
        {
            if (Time.unscaledTime < _nextShieldTick)
                return;
            _nextShieldTick = Time.unscaledTime + 0.5f;
            var now = View.ServerNowMs;
            foreach (var b in View.WorldView?.Bases ?? Array.Empty<WorldBaseDto>())
            {
                if (!_bases.TryGetValue(b.PlayerId, out var entry) || entry.Model == null)
                    continue;
                if (!_domes.TryGetValue(b.PlayerId, out var dome) || dome == null)
                    _domes[b.PlayerId] = dome = ShieldDome.Create(entry.Model.transform, DomeRadius);
                var kind = Protection(b, now);
                dome.Show(kind);

                if (!_shieldLabels.TryGetValue(b.PlayerId, out var label) || label == null)
                {
                    label = UiKit.Label("Shield", entry.Plate, string.Empty, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    label.rectTransform.Place(0f, -0.6f, 1f, 0f);
                    label.raycastTarget = false;
                    label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
                    _shieldLabels[b.PlayerId] = label;
                }

                label.text = kind switch
                {
                    ShieldDome.Kind.Shield => $"<color=#7FE9FF>🛡 {Loc.F("pvp.shield.plate", Clock(b.ShieldUntilMs - now))}</color>",
                    ShieldDome.Kind.Recovery => $"<color=#FFC266>🛡 {Loc.F("pvp.shield.plate", Clock(b.ShieldUntilMs - now))}</color>",
                    ShieldDome.Kind.Beginner => $"<color=#8CF29E>{Loc.T("pvp.beginner.plate")}</color>",
                    _ => string.Empty
                };
            }

            foreach (var id in _domes.Keys.Where(id => !_bases.ContainsKey(id)).ToList())
            {
                _domes.Remove(id);
                _shieldLabels.Remove(id);
            }
        }

        private static string Clock(long ms)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
            return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
        }

        /// <summary>Why we cannot attack this base right now, or null: our newbie protection, theirs, their shield.</summary>
        private string PvpBlock(WorldBaseDto b)
        {
            var now = View.ServerNowMs;
            if (IBeginner)
                return Loc.F("pvp.block.meBeginner", PvpRules.BeginnerReactor);
            if (PvpRules.IsBeginner(b.Level))
                return Loc.T("pvp.block.beginner");
            if (b.ShieldUntilMs > now)
                return Loc.F("pvp.block.shield", Loc.Duration((b.ShieldUntilMs - now) / 1000f));
            return null;
        }

        /// <summary>Our team's menu line when the base it sails to has raised a shield: it will bounce off.</summary>
        private string TargetShieldNote(MarchDto m)
        {
            if (m.Kind is not (PvpRules.Attack or PvpRules.Scout or PvpRules.Rally))
                return string.Empty;
            var target = (View.WorldView?.Bases ?? Array.Empty<WorldBaseDto>()).FirstOrDefault(b => b.X == m.X && b.Y == m.Y);
            return target != null && target.ShieldUntilMs > m.ArrivesAtMs ? $"\n<color=#7FE9FF>🛡 {Loc.T("pvp.target.shielded")}</color>" : string.Empty;
        }
    }
}
