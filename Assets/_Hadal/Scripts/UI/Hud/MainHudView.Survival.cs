using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Data.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI.Hud
{
    /// <summary>
    /// The survival loop's alarms on the HUD (docs/35): the Oxygen and Energy pills pulse amber while a store is
    /// running low and red while it is empty, a banner under the header names the worst problem, and a tap on either
    /// opens a card that explains it with a "What should I do?" hint and a button to the building to upgrade. A running
    /// emergency shows the same way with Repair and Ask for help. Everything here is what the server sent; the HUD
    /// computes nothing.
    /// </summary>
    public sealed partial class MainHudView
    {
        private const int StatusLow = 1;
        private const int StatusCritical = 2;

        private readonly Dictionary<ResourceType, (Image Fill, Color Base)> _chipFills = new();
        private RectTransform _alarm;
        private Image _alarmFill;
        private TMP_Text _alarmText;
        private GameObject _survivalCard;
        private TMP_Text _survivalTimer;
        private int _survivalCardVersion = -1;
        private string _knownEmergency;
        private bool _repairAsked;

        private void BuildSurvivalAlarm()
        {
            _alarmFill = UiKit.Box("SurvivalAlarm", _lower.transform, new Color(0.55f, 0.06f, 0.05f, 0.92f));
            _alarm = _alarmFill.rectTransform;
            _alarm.Place(0.46f, 0.872f, 0.825f, 0.912f);
            _alarmFill.sprite = UiKit.Rounded;
            _alarmFill.type = Image.Type.Sliced;
            var button = _alarmFill.gameObject.AddComponent<Button>();
            button.targetGraphic = _alarmFill;
            button.onClick.AddListener(OpenSurvivalCard);
            _alarmFill.gameObject.AddComponent<PressFeedback>();
            _alarmText = Outlined(UiKit.Label("Text", _alarm, string.Empty, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            _alarmText.rectTransform.Place(0.03f, 0f, 0.97f, 1f);
            _alarmText.textWrappingMode = TextWrappingModes.Normal;
            _alarmText.enableAutoSizing = true;
            _alarmText.fontSizeMin = 14;
            _alarmText.fontSizeMax = 22;
            _alarm.gameObject.SetActive(false);
        }

        /// <summary>Remembers a resource pill's face so the alarm pulse can tint it and put it back.</summary>
        private void TrackChip(ResourceType type, RectTransform chip)
        {
            if (type is ResourceType.Oxygen or ResourceType.Energy && chip.TryGetComponent<Image>(out var image))
                _chipFills[type] = (image, image.color);
        }

        private SurvivalStateDto Survival => View.Survival;

        private bool HasEmergency => Survival != null && !string.IsNullOrEmpty(Survival.EmergencyId);

        /// <summary>Every frame: the pills pulse and the banner counts down. Cheap: no layout is rebuilt here.</summary>
        private void PulseSurvival()
        {
            var survival = Survival;
            foreach (var pair in _chipFills)
            {
                var status = survival == null ? 0 : pair.Key == ResourceType.Oxygen ? survival.Oxygen : survival.Energy;
                var (fill, face) = pair.Value;
                if (fill == null)
                    continue;
                if (status <= 0)
                {
                    fill.color = face;
                    continue;
                }

                var alarm = status >= StatusCritical ? new Color(1f, 0.25f, 0.2f, 1f) : new Color(1f, 0.7f, 0.2f, 1f);
                var speed = status >= StatusCritical ? 6f : 3f;
                fill.color = Color.Lerp(face, alarm, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * speed));
            }

            if (_alarm == null)
                return;
            var line = AlarmLine(out var critical);
            _alarm.gameObject.SetActive(line != null && !(_actions.InWorld?.Invoke() ?? false));
            if (line == null)
                return;
            _alarmText.text = line;
            var pulse = 0.82f + 0.12f * Mathf.Sin(Time.unscaledTime * (critical ? 5f : 2.5f));
            _alarmFill.color = critical ? new Color(0.62f * pulse, 0.07f, 0.05f, 0.94f) : new Color(0.62f * pulse, 0.38f * pulse, 0.05f, 0.94f);

            if (_survivalTimer != null && HasEmergency)
                _survivalTimer.text = EmergencyTimerLine();
        }

        /// <summary>The worst problem in one short line: an emergency, then empty oxygen, empty energy, then the warnings.</summary>
        private string AlarmLine(out bool critical)
        {
            critical = false;
            var s = Survival;
            if (s == null)
                return null;
            var now = Time.realtimeSinceStartup;
            if (HasEmergency)
            {
                critical = true;
                var what = Loc.T("survival.emergency." + s.EmergencyKind);
                return View.RepairEndsAt > now
                    ? $"🔧 {Loc.F("survival.alarm.repairing", what, Loc.Duration(View.RepairEndsAt - now))}"
                    : $"⚠ {what}: {Loc.Building(s.EmergencyDefinitionId)} — {Loc.T("survival.repair")}";
            }

            if (s.Oxygen >= StatusCritical)
            {
                critical = true;
                return "⚠ " + Loc.F("survival.alarm.oxygen.critical", s.OutputPenaltyPercent);
            }

            if (s.Energy >= StatusCritical)
            {
                critical = true;
                return "⚠ " + Loc.F("survival.alarm.energy.critical", s.EnergyPercent);
            }

            if (s.Oxygen == StatusLow)
                return Loc.T("survival.alarm.oxygen.low");
            if (s.Energy == StatusLow)
                return Loc.T("survival.alarm.energy.low");
            return null;
        }

        /// <summary>On every refresh: a new emergency is announced (with a buzz), an ended one too, and an open card follows the state.</summary>
        private void RefreshSurvival()
        {
            var id = HasEmergency ? Survival.EmergencyId : string.Empty;
            if (_knownEmergency != null && id != _knownEmergency)
            {
                if (id.Length > 0)
                {
                    _repairAsked = false;
#if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate();
#endif
                    _actions.Toast?.Invoke(Loc.F("survival.toast.started", Loc.T("survival.emergency." + Survival.EmergencyKind),
                        Loc.Building(Survival.EmergencyDefinitionId)), UiKit.Danger);
                }
                else
                {
                    _actions.Toast?.Invoke(Loc.T("survival.toast.resolved"), UiKit.Ok);
                }
            }

            _knownEmergency = id;
            if (_survivalCard != null && _survivalCardVersion != View.SurvivalVersion)
                OpenSurvivalCard();
        }

        /// <summary>Resource pills open this card for Oxygen and Energy when something is wrong; otherwise the usual popup.</summary>
        private bool ShowSurvivalFor(ResourceType type)
        {
            var s = Survival;
            if (s == null || (type == ResourceType.Oxygen ? s.Oxygen : type == ResourceType.Energy ? s.Energy : 0) <= 0 && !HasEmergency)
                return false;
            OpenSurvivalCard();
            return true;
        }

        private void OpenSurvivalCard()
        {
            if (_survivalCard != null)
                Destroy(_survivalCard);
            _survivalTimer = null;
            var s = Survival;
            if (s == null)
                return;
            _survivalCardVersion = View.SurvivalVersion;

            var dim = UiKit.Box("SurvivalCard", _canvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(CloseSurvivalCard);
            _survivalCard = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.06f, 0.3f, 0.94f, 0.7f);
            card.gameObject.AddComponent<Button>(); // taps on the card do not close it
            HudIcons.Image(card.transform, HasEmergency || s.Oxygen > 0 ? HudIcons.Resource(ResourceType.Oxygen) : HudIcons.Resource(ResourceType.Energy),
                0.03f, 0.83f, 0.14f, 0.97f);
            UiKit.Label("Title", card.transform, Loc.T(HasEmergency ? "survival.title.emergency" : "survival.title"), 40, UiKit.Danger,
                TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.16f, 0.83f, 0.86f, 0.97f);
            UiKit.Button("Close", card.transform, "X", CloseSurvivalCard).GetComponent<RectTransform>().Place(0.87f, 0.85f, 0.97f, 0.97f);

            var (what, hint, goTo) = Explain(s);
            UiKit.Label("What", card.transform, what, 28, UiKit.Text, TextAnchor.UpperLeft).rectTransform.Place(0.05f, 0.5f, 0.95f, 0.82f);
            UiKit.Label("HintTitle", card.transform, Loc.T("survival.hint.title"), 28, UiKit.Amber, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.42f, 0.95f, 0.5f);
            UiKit.Label("Hint", card.transform, hint, 26, UiKit.Text, TextAnchor.UpperLeft).rectTransform.Place(0.05f, 0.2f, 0.95f, 0.42f);

            if (HasEmergency)
            {
                _survivalTimer = UiKit.Label("Timer", card.transform, EmergencyTimerLine(), 24, UiKit.Muted, TextAnchor.MiddleLeft);
                _survivalTimer.rectTransform.Place(0.05f, 0.14f, 0.95f, 0.2f);
            }

            // The buttons: Repair (with its cost) or Ask for help while the crew works, and Go to the building.
            var buttons = new List<(string Text, UnityEngine.Events.UnityAction Click, Color Fill)>();
            var repairing = View.RepairEndsAt > Time.realtimeSinceStartup;
            if (HasEmergency && !repairing)
            {
                var cost = string.Join(" ", (s.RepairCost ?? System.Array.Empty<ResourceAmountEntry>())
                    .Select(c => $"{c.Value:N0} {Loc.Resource(c.Key.ToString())}"));
                buttons.Add(($"{Loc.T("survival.repair")}  {cost}", () => _actions.RepairEmergency?.Invoke(s.EmergencyId), UiKit.Ok));
            }
            else if (HasEmergency && View.MyAlliance != null && !_repairAsked)
            {
                buttons.Add((Loc.T("help.ask"), () =>
                {
                    _repairAsked = true;
                    _actions.AskRepairHelp?.Invoke(s.EmergencyId);
                    OpenSurvivalCard();
                }, UiKit.Primary));
            }

            if (!string.IsNullOrEmpty(goTo))
                buttons.Add((Loc.T("survival.go"), () => { CloseSurvivalCard(); _actions.Building?.Invoke(goTo); }, UiKit.Primary));

            for (var i = 0; i < buttons.Count; i++)
            {
                var x0 = 0.05f + i * 0.46f;
                var b = UiKit.Button("Action" + i, card.transform, buttons[i].Text, buttons[i].Click, buttons[i].Fill, 28);
                b.GetComponent<RectTransform>().Place(x0, 0.03f, x0 + 0.44f, 0.13f);
            }
        }

        private void CloseSurvivalCard()
        {
            if (_survivalCard != null)
                Destroy(_survivalCard);
            _survivalCard = null;
            _survivalTimer = null;
        }

        private string EmergencyTimerLine()
        {
            var now = Time.realtimeSinceStartup;
            return View.RepairEndsAt > now
                ? Loc.F("survival.timer.repair", Loc.Duration(View.RepairEndsAt - now))
                : View.EmergencyExpiresAt > now ? Loc.F("survival.timer.expires", Loc.Duration(View.EmergencyExpiresAt - now)) : string.Empty;
        }

        /// <summary>What is wrong, what to do about it, and the building to open (instance key; empty = none to open).</summary>
        private (string What, string Hint, string GoTo) Explain(SurvivalStateDto s)
        {
            var buildings = Buildings();
            string Lowest(params string[] ids) => buildings.Where(b => ids.Contains(b.definitionId) && b.level > 0)
                .OrderBy(b => b.IsUnderConstruction ? 1 : 0).ThenBy(b => b.level).Select(b => b.instanceKey).FirstOrDefault() ?? string.Empty;
            var reactor = buildings.FirstOrDefault(b => b.definitionId == BuildingProgression.ReactorId).instanceKey ?? string.Empty;
            var lines = new List<string>();

            if (HasEmergency)
            {
                var place = Loc.Building(s.EmergencyDefinitionId);
                lines.Add(s.EmergencyKind == EmergencyKinds.Leak
                    ? Loc.F("survival.what.leak", place, Mathf.RoundToInt(s.LeakMilliPerTick * TicksPerMinute / 1000f))
                    : Loc.F("survival.what.breakdown", place));
                return (string.Join("\n", lines.Concat(StoreLines(s))), Loc.T("survival.hint.emergency"), s.EmergencyBuildingId);
            }

            if (s.Oxygen > 0)
            {
                lines.AddRange(StoreLines(s));
                var plant = Lowest("oxygen_plant", "mega_dome");
                var hint = plant.Length > 0 ? Loc.T("survival.hint.oxygen") : Loc.T("survival.hint.oxygen.build");
                if (s.Energy > 0)
                    hint += "\n" + Loc.T("survival.hint.energy");
                return (string.Join("\n", lines), hint, plant.Length > 0 ? plant : reactor);
            }

            lines.AddRange(StoreLines(s));
            return (string.Join("\n", lines), Loc.T("survival.hint.energy"), reactor);
        }

        private static IEnumerable<string> StoreLines(SurvivalStateDto s)
        {
            if (s.Oxygen >= StatusCritical)
                yield return Loc.F("survival.what.oxygen.critical", s.OutputPenaltyPercent, s.TimePenaltyPercent);
            else if (s.Oxygen == StatusLow)
                yield return Loc.T("survival.what.oxygen.low");
            if (s.Energy >= StatusCritical)
                yield return Loc.F("survival.what.energy.critical", s.EnergyPercent);
            else if (s.Energy == StatusLow)
                yield return Loc.T("survival.what.energy.low");
        }
    }
}
