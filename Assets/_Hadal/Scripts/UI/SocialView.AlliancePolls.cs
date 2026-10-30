using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Alliance polls and event time-slot votes (docs/29 §5), Last Asylum's card style: the head card with "New vote" for
    /// whoever may open one, then every open vote — its question, the options as selectable rows with progress bars and
    /// (unless anonymous) who chose them, Vote / Change vote and Close — then the recent results. Time slots read in the
    /// phone's time with their server time. "New vote" opens the create dialog over the blurred page (UiFrames.Dim +
    /// Dialog); the duty picker of the member card lives here too.
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabPolls = 11;

        /// <summary>The options picked on each open vote before Vote is tapped.</summary>
        private readonly Dictionary<string, HashSet<int>> _pollPicks = new Dictionary<string, HashSet<int>>();

        private readonly PollDraft _draft = new PollDraft();
        private GameObject _pollDialog;
        private GameObject _dutyDialog;

        private AlliancePollDto[] PollList => View.AlliancePolls?.Polls ?? Array.Empty<AlliancePollDto>();

        /// <summary>Open votes the player has not answered (the hub tile's red dot).</summary>
        private int PollsWaiting() => View.MyAlliance == null ? 0 : PollList.Count(p => AlliancePollRules.AwaitsVote(p, NowMs));

        private bool MayOpenPoll() => AlliancePermissions.CanRunPolls(View.MyRank(), View.MyRole());

        private bool MayOpenTimeVote() => AlliancePermissions.CanRunEvents(View.MyRank(), View.MyRole());

        private bool MayClose(AlliancePollDto p) => p.CreatorId == View.PlayerId
            || (p.Kind == AlliancePollRules.KindSlots ? MayOpenTimeVote() : MayOpenPoll());

        private static string Weekday(int day) => Loc.T("event.weekday." + day);

        private string SlotText(long atMs) => AlliancePollText.Slot(atMs, EventCalendarText.LocalOffsetMs(NowMs), Weekday);

        private void RenderAlliancePolls()
        {
            var polls = PollList;
            var open = polls.Where(p => AlliancePollRules.IsOpen(p, NowMs)).ToList();
            var closed = polls.Where(p => !AlliancePollRules.IsOpen(p, NowMs)).ToList();

            var head = InkCard(220, UiFrames.Highlight);
            HudIconImage(head.transform, "g_info", 0.02f, 0.3f, 0.16f, 0.95f);
            Ink(head.transform, Loc.T("alliance.tile.polls"), UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.19f, 0.66f, 0.97f, 0.96f, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ink(head.transform, Loc.T("alliance.polls.intro"), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.19f, 0.3f, 0.97f, 0.66f);
            var canOpen = MayOpenPoll() || MayOpenTimeVote();
            if (canOpen)
                CardButton(head.transform, Loc.T("alliance.polls.new"), open.Count < AlliancePollRules.MaxOpen, OpenPollDialog, UiKit.Primary, 0.3f, 0.03f, 0.7f, 0.27f);

            Band($"{Loc.T("alliance.polls.open")} ({open.Count}/{AlliancePollRules.MaxOpen})");
            if (open.Count == 0)
                InkNote(Loc.T("alliance.polls.none"));
            foreach (var p in open)
                PollCard(p, true);

            if (closed.Count == 0)
                return;
            Band(Loc.T("alliance.polls.recent"));
            foreach (var p in closed)
                PollCard(p, false);
        }

        /// <summary>One vote: the head (kind, question, who, time left, voters), the option rows, then the buttons.</summary>
        private void PollCard(AlliancePollDto p, bool open)
        {
            var slots = p.Kind == AlliancePollRules.KindSlots;
            var title = slots
                ? $"{Loc.T("alliance.poll.kind.slots")} · {Loc.T("alliance.poll.purpose." + p.Purpose)}"
                : Loc.T("alliance.poll.kind.poll");
            var question = p.Question ?? string.Empty; // a time vote's note may be empty
            var hasQuestion = !string.IsNullOrEmpty(question);
            var head = InkCard(hasQuestion ? 200 : 140, open ? (Color?)null : UiFrames.CardAlt);
            HudIconImage(head.transform, slots ? "g_rally" : "g_info", 0.02f, 0.18f, 0.12f, 0.82f);
            var top = hasQuestion ? 0.68f : 0.52f;
            Ink(head.transform, title, UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.14f, top, 0.97f, 0.97f, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (hasQuestion)
            {
                var text = Ink(head.transform, question, UiKit.TypeScale.Body + 2, UiFrames.Ink, 0.14f, 0.34f, 0.97f, 0.7f, TextAnchor.MiddleLeft, FontStyle.Bold);
                text.richText = false;
                text.Fit(16, UiKit.TypeScale.Body + 2);
            }

            var when = open
                ? Loc.F("alliance.poll.ends", Span(p.ClosesAtMs - NowMs))
                : Loc.F("alliance.poll.ended", DateTimeOffset.FromUnixTimeMilliseconds(p.ClosedAtMs).ToLocalTime().ToString("dd.MM HH:mm"));
            var rules = new List<string> { Loc.T(p.Multiple ? "alliance.poll.multiple" : "alliance.poll.single") };
            if (p.Anonymous)
                rules.Add(Loc.T("alliance.poll.anonymous"));
            var meta = Ink(head.transform, $"{Loc.F("alliance.poll.by", p.CreatorName)} · {when} · {Loc.F("alliance.poll.voters", p.Voters)} · {string.Join(" · ", rules)}",
                UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.14f, 0.04f, 0.97f, hasQuestion ? 0.34f : 0.5f, TextAnchor.MiddleLeft);
            meta.richText = false;
            meta.Fit(14, UiKit.TypeScale.Caption);

            var picks = Picks(p);
            var options = p.Options ?? Array.Empty<AlliancePollOptionDto>();
            var best = options.Length == 0 ? 0 : options.Max(o => o.Votes);
            for (var i = 0; i < options.Length; i++)
                OptionRow(p, i, options[i], open, picks.Contains(i), best);

            if (!open)
            {
                var result = p.Winner > 0
                    ? slots ? Loc.F("alliance.poll.fixed", SlotText(p.WinnerAtMs)) : Loc.F("alliance.poll.winner", options[p.Winner - 1].Text)
                    : Loc.T("alliance.poll.nowinner");
                var card = InkCard(100, p.Winner > 0 ? UiFrames.RankMine : UiFrames.CardAlt);
                var line = Ink(card.transform, result, UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.05f, 0.96f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                line.richText = false;
                line.Fit(16, UiKit.TypeScale.Body);
                return;
            }

            var actions = InkCard(110, UiFrames.CardAlt);
            var voted = p.MyVotes != null && p.MyVotes.Length > 0;
            var changed = !SameChoices(picks, p.MyVotes);
            var id = p.Id;
            var canClose = MayClose(p);
            CardButton(actions.transform, voted ? Loc.T("alliance.poll.change") : Loc.T("alliance.poll.vote"), picks.Count > 0 && changed, () =>
            {
                if (!_pollPicks.TryGetValue(id, out var chosen) || chosen.Count == 0)
                {
                    Toast(Loc.T("alliance.poll.pick"), UiKit.Amber);
                    return;
                }

                _commands.Dispatch(new VoteAlliancePollCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), PollId = id, Choices = chosen.OrderBy(c => c).ToArray()
                });
            }, UiKit.Ok, 0.03f, 0.12f, canClose ? 0.6f : 0.97f, 0.88f);
            if (canClose)
                CardButton(actions.transform, Loc.T("alliance.poll.close"), true, () => _commands.Dispatch(new CloseAlliancePollCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), PollId = id
                }), UiKit.Danger, 0.63f, 0.12f, 0.97f, 0.88f);
        }

        /// <summary>
        /// One option as a selectable row: the radio (or tick) mark, the answer or the time, the bar with votes and share,
        /// and the first voters' names when the vote is not anonymous. The leader of a finished vote is green.
        /// </summary>
        private void OptionRow(AlliancePollDto p, int index, AlliancePollOptionDto option, bool open, bool picked, int best)
        {
            var names = p.Anonymous ? string.Empty : AlliancePollText.Names(option.VoterNames ?? Array.Empty<string>());
            var won = !open && p.Winner == index + 1;
            var row = InkCard(names.Length > 0 ? 170 : 130, picked ? UiFrames.Highlight : won ? UiFrames.RankMine : (Color?)null);
            var mine = p.MyVotes != null && p.MyVotes.Contains(index);

            var mark = UiKit.Box("Mark", row.transform, picked ? UiFrames.InkGood : UiFrames.Band);
            mark.sprite = p.Multiple ? UiKit.Rounded : UiKit.Circle;
            mark.type = p.Multiple ? Image.Type.Sliced : Image.Type.Simple;
            mark.preserveAspect = !p.Multiple;
            mark.raycastTarget = false;
            mark.rectTransform.Place(0.025f, 0.3f, 0.085f, 0.7f);
            if (picked || (!open && mine))
                UiKit.Label("Tick", mark.transform, "✓", UiKit.TypeScale.Body, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);

            var top = names.Length > 0 ? 0.62f : 0.52f;
            var label = p.Kind == AlliancePollRules.KindSlots ? SlotText(option.AtMs) : option.Text;
            var text = Ink(row.transform, label, UiKit.TypeScale.Body, UiFrames.Ink, 0.11f, top, 0.97f, 0.97f, TextAnchor.MiddleLeft,
                FontStyle.Bold);
            text.richText = false;
            text.Fit(16, UiKit.TypeScale.Body);

            var share = p.Voters > 0 ? (float)option.Votes / p.Voters : 0f;
            var bar = Skin.Bar(row.transform, share, won || (open && option.Votes == best && best > 0) ? "gold" : "green");
            bar.Place(0.11f, names.Length > 0 ? 0.36f : 0.1f, 0.97f, top - 0.06f);
            UiKit.Stroked(UiKit.Label("Value", bar, AlliancePollText.Share(option.Votes, p.Voters), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold))
                .rectTransform.Place(0, 0, 1, 1);
            if (names.Length > 0)
            {
                var who = Ink(row.transform, names, UiKit.TypeScale.Micro, UiFrames.InkSoft, 0.11f, 0.03f, 0.97f, 0.33f, TextAnchor.MiddleLeft);
                who.richText = false;
            }

            if (!open)
                return;
            var id = p.Id;
            var multiple = p.Multiple;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            button.onClick.AddListener(() =>
            {
                var chosen = Picks(id, multiple, null);
                if (multiple)
                {
                    if (!chosen.Remove(index))
                        chosen.Add(index);
                }
                else
                {
                    chosen.Clear();
                    chosen.Add(index);
                }

                GameAudio.Play(Sfx.UiToggle);
                Render(force: false);
            });
        }

        /// <summary>What is picked on a vote: the player's own choices until they tap something else.</summary>
        private HashSet<int> Picks(AlliancePollDto p) => Picks(p.Id, p.Multiple, p.MyVotes);

        private HashSet<int> Picks(string pollId, bool multiple, int[] mine)
        {
            if (!_pollPicks.TryGetValue(pollId, out var picks))
                _pollPicks[pollId] = picks = new HashSet<int>(mine ?? Array.Empty<int>());
            return picks;
        }

        private static bool SameChoices(HashSet<int> picks, int[] mine) =>
            picks.Count == (mine?.Length ?? 0) && (mine ?? Array.Empty<int>()).All(picks.Contains);

        /// <summary>A vote went through or the votes changed: picks start again from the player's own ballot.</summary>
        private void ForgetPollPicks() => _pollPicks.Clear();

        // --- Create dialog -------------------------------------------------------------------

        private void OpenPollDialog()
        {
            var kind = MayOpenPoll() ? AlliancePollRules.KindPoll : AlliancePollRules.KindSlots;
            _draft.Reset(kind, NowMs, EventCalendarText.LocalOffsetMs(NowMs));
            BuildPollDialog();
        }

        private void ClosePollDialog()
        {
            if (_pollDialog != null)
                Destroy(_pollDialog);
            _pollDialog = null;
        }

        /// <summary>
        /// The form over the blurred page: the kind tabs (for whoever may open both), then question and answers, or event,
        /// note and times; the voting time, the switches, what is still wrong, and Start vote. Rebuilt on every structural
        /// change; typing only updates the draft.
        /// </summary>
        private void BuildPollDialog()
        {
            ClosePollDialog();
            var dim = UiFrames.Dim(_canvas.transform, ClosePollDialog);
            dim.name = "PollDialog";
            _pollDialog = dim.gameObject;
            var card = UiKit.Box("Card", dim, UiFrames.Navy);
            card.rectTransform.Place(0.04f, 0.2f, 0.96f, 0.95f);
            card.gameObject.AddComponent<CanvasGroup>();
            var parts = UiFrames.Dialog(card.rectTransform, ClosePollDialog);
            parts.Title.text = Loc.T("alliance.polls.new");
            var list = UiKit.ScrollList("Form", parts.Body, 14f);
            ((RectTransform)list.parent).Place(0.03f, 0.02f, 0.97f, 0.98f);
            var slots = _draft.Kind == AlliancePollRules.KindSlots;

            if (MayOpenPoll() && MayOpenTimeVote())
            {
                UiFrames.Tabs(list, new[] { Loc.T("alliance.poll.kind.poll"), Loc.T("alliance.poll.kind.slots") }, slots ? 1 : 0, i =>
                {
                    _draft.Reset(i == 1 ? AlliancePollRules.KindSlots : AlliancePollRules.KindPoll, NowMs, EventCalendarText.LocalOffsetMs(NowMs));
                    BuildPollDialog();
                }).Row(96);
            }

            if (slots)
            {
                Label(list, Loc.T("alliance.poll.event"));
                var purpose = UiFrames.CardBox(list, null, "Purpose").Row(110);
                Ink(purpose.transform, Loc.T("alliance.poll.purpose." + _draft.Purpose), UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.1f, 0.6f, 0.9f,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                CardButton(purpose.transform, "▶", true, () => { _draft.StepPurpose(); BuildPollDialog(); }, UiKit.TabOn, 0.75f, 0.12f, 0.97f, 0.88f);
                Field(list, Loc.T("alliance.poll.note.hint"), _draft.Question, AlliancePollRules.MaxQuestion, v => _draft.Question = v);
                Label(list, Loc.T("alliance.poll.slots.rule"));
                for (var i = 0; i < _draft.Slots.Count; i++)
                    SlotRow(list, i);
            }
            else
            {
                Label(list, Loc.T("alliance.poll.question"));
                Field(list, Loc.T("alliance.poll.question.hint"), _draft.Question, AlliancePollRules.MaxQuestion, v => _draft.Question = v);
                for (var i = 0; i < _draft.Options.Count; i++)
                {
                    var index = i;
                    var row = UiKit.Rect("Option", list).Row(100);
                    var input = UiKit.Input("Text", row, Loc.F("alliance.poll.option", i + 1), characterLimit: AlliancePollRules.MaxOption);
                    ((RectTransform)input.transform).Place(0, 0.04f, _draft.CanRemoveOption ? 0.84f : 1f, 0.96f);
                    input.text = _draft.Options[i];
                    input.onValueChanged.AddListener(v => _draft.Options[index] = v);
                    if (_draft.CanRemoveOption)
                        CardButton(row, "−", true, () => { _draft.RemoveOption(index); BuildPollDialog(); }, UiKit.TabOn, 0.86f, 0.06f, 1f, 0.94f);
                }
            }

            if (_draft.CanAddOption)
            {
                var add = UiKit.Rect("Add", list).Row(96);
                CardButton(add, Loc.T(slots ? "alliance.poll.addSlot" : "alliance.poll.addOption"), true, () => { _draft.AddOption(); BuildPollDialog(); },
                    UiKit.TabOn, 0.25f, 0.06f, 0.75f, 0.94f);
            }

            var hours = UiFrames.CardBox(list, null, "Hours").Row(110);
            Ink(hours.transform, Loc.T("alliance.poll.hours"), UiKit.TypeScale.Body, UiFrames.Ink, 0.04f, 0.1f, 0.45f, 0.9f, TextAnchor.MiddleLeft, FontStyle.Bold);
            CardButton(hours.transform, "−", _draft.Hours > PollDraft.HourSteps[0], () => { _draft.StepHours(-1); BuildPollDialog(); }, UiKit.TabOn, 0.47f, 0.12f, 0.6f, 0.88f);
            Ink(hours.transform, Loc.F("alliance.poll.hoursValue", _draft.Hours), UiKit.TypeScale.Body, UiFrames.Ink, 0.6f, 0.1f, 0.84f, 0.9f, TextAnchor.MiddleCenter, FontStyle.Bold);
            CardButton(hours.transform, "+", _draft.Hours < PollDraft.HourSteps[PollDraft.HourSteps.Length - 1], () => { _draft.StepHours(1); BuildPollDialog(); },
                UiKit.TabOn, 0.84f, 0.12f, 0.97f, 0.88f);

            UiFrames.ToggleRow(list, null, Loc.T("alliance.poll.multiple"), () => _draft.Multiple, v => _draft.Multiple = v).Row(96);
            if (!slots)
                UiFrames.ToggleRow(list, null, Loc.T("alliance.poll.anonymous"), () => _draft.Anonymous, v => _draft.Anonymous = v).Row(96);

            var submit = UiKit.Rect("Submit", list).Row(120);
            CardButton(submit, Loc.T("alliance.poll.create"), true, () =>
            {
                var problem = _draft.Problem(NowMs);
                if (problem != SocialRejectReason.None)
                {
                    Toast(Loc.Reason(problem.ToString()), UiKit.Danger);
                    return;
                }

                _commands.Dispatch(_draft.Command(NewId(), NextSequence()));
            }, UiKit.Ok, 0.15f, 0.08f, 0.85f, 0.92f);
        }

        /// <summary>One offered time: the time in the phone's time and server time, day and half-hour arrows, remove.</summary>
        private void SlotRow(RectTransform list, int index)
        {
            var row = UiFrames.CardBox(list, null, "Slot").Row(170);
            var text = Ink(row.transform, $"{Loc.F("alliance.poll.slot", index + 1)}  {SlotText(_draft.Slots[index])}", UiKit.TypeScale.Caption + 2, UiFrames.Ink,
                0.03f, 0.55f, 0.97f, 0.97f, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.Fit(14, UiKit.TypeScale.Caption + 2);
            var steps = new (string Label, long Delta)[]
            {
                ("−24h", -PollDraft.DayMs), ("+24h", PollDraft.DayMs), ("−30m", -PollDraft.SlotStepMs), ("+30m", PollDraft.SlotStepMs)
            };
            var width = _draft.CanRemoveOption ? 0.19f : 0.235f;
            for (var s = 0; s < steps.Length; s++)
            {
                var delta = steps[s].Delta;
                CardButton(row.transform, steps[s].Label, true, () => { _draft.ShiftSlot(index, delta); BuildPollDialog(); }, UiKit.TabOn,
                    0.03f + s * (width + 0.01f), 0.06f, 0.03f + s * (width + 0.01f) + width, 0.5f);
            }

            if (_draft.CanRemoveOption)
                CardButton(row.transform, "−", true, () => { _draft.RemoveOption(index); BuildPollDialog(); }, UiKit.Danger, 0.83f, 0.06f, 0.97f, 0.5f);
        }

        private static void Label(RectTransform list, string text) =>
            UiKit.Label("Label", list, text, UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.LowerLeft, FontStyle.Bold).Row(56);

        private static void Field(RectTransform list, string hint, string value, int limit, Action<string> changed)
        {
            var input = UiKit.Input("Field", list, hint, characterLimit: limit).Row(100);
            input.text = value ?? string.Empty;
            input.onValueChanged.AddListener(v => changed(v));
        }

        // --- Duty picker (member card) -------------------------------------------------------

        /// <summary>The leader gives an officer one duty: a row per duty with what it allows, the current one ticked.</summary>
        private void OpenDutyPicker(AllianceMemberDto member)
        {
            CloseDutyPicker();
            var dim = UiFrames.Dim(_canvas.transform, CloseDutyPicker);
            dim.name = "DutyPicker";
            _dutyDialog = dim.gameObject;
            var roles = AlliancePermissions.Roles;
            const float header = 0.055f, step = 0.085f;
            var height = header + roles.Length * step + 0.03f;
            var card = UiKit.Box("Card", dim, UiFrames.Navy);
            card.rectTransform.Place(0.05f, 0.5f - height / 2f, 0.95f, 0.5f + height / 2f);
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().From = 0.94f;
            var parts = UiFrames.Dialog(card.rectTransform, CloseDutyPicker);
            parts.Title.text = $"{Loc.T("alliance.duty.pick")} · {member.Name}";
            parts.Title.richText = false;
            var inner = height - header;
            var id = member.PlayerId;
            for (var i = 0; i < roles.Length; i++)
            {
                var role = roles[i];
                var current = (member.Role ?? string.Empty) == role;
                var top = 1f - (0.012f + i * step) / inner;
                var row = UiFrames.CardBox(parts.Body, current ? UiFrames.Highlight : (Color?)null, "Duty");
                row.rectTransform.Place(0.03f, top - (step - 0.01f) / inner, 0.97f, top);
                HudIconImage(row.transform, DutyIcon(role), 0.02f, 0.15f, 0.14f, 0.85f);
                Ink(row.transform, (role.Length == 0 ? Loc.T("alliance.duty.none") : Loc.T("alliance.duty." + role)) + (current ? "  ✓" : string.Empty),
                    UiKit.TypeScale.Body, UiFrames.Ink, 0.17f, 0.5f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
                var grants = Ink(row.transform, Loc.T("alliance.duty.grants." + (role.Length == 0 ? "none" : role)), UiKit.TypeScale.Caption, UiFrames.InkSoft,
                    0.17f, 0.05f, 0.97f, 0.5f, TextAnchor.MiddleLeft);
                grants.Fit(14, UiKit.TypeScale.Caption);
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                button.onClick.AddListener(() =>
                {
                    CloseDutyPicker();
                    if (!current)
                        SetRank(id, AlliancePermissions.Officer, role);
                });
            }
        }

        private void CloseDutyPicker()
        {
            if (_dutyDialog != null)
                Destroy(_dutyDialog);
            _dutyDialog = null;
        }

        private static string DutyIcon(string role) => role switch
        {
            AlliancePermissions.War => "g_attack",
            AlliancePermissions.Logistics => "queue_build",
            AlliancePermissions.Diplomacy => "g_chat",
            AlliancePermissions.Scout => "g_scout",
            _ => "g_profile"
        };
    }
}
