using Hadal.Game.Onboarding;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// The first-session guide (docs/34): a short chain of steps claimed like chapter tasks. The server decides when a
    /// step is done; its rewards go straight to the wallet so the next step is never blocked. Accounts from before the
    /// guide that are past its milestone skip it; earlier ones are enrolled on their next login.
    /// </summary>
    public sealed partial class PlayerSession
    {
        /// <summary>Where guide events go (the funnel log); set by the world. Called under the session lock: must not block.</summary>
        internal Action<OnboardingEvent>? OnboardingLog { get; set; }

        private QuestData[] GuideSteps => _catalog.Quests.onboarding ?? Array.Empty<QuestData>();

        /// <summary>On login: a new account logs its start, an account from before the guide is skipped or enrolled. Caller holds the lock.</summary>
        private void EnsureOnboarding(DateTimeOffset now)
        {
            if (_state.OnboardingStatus == OnboardingStatus.Unknown)
            {
                if (GuideSteps.Length == 0 || OnboardingRules.ShouldSkip(ReactorLevel(), _state.ChapterIndex))
                {
                    _state.OnboardingStatus = OnboardingStatus.Skipped;
                    LogGuide(-1, "skipped", "existing", now);
                }
                else
                {
                    _state.OnboardingStatus = OnboardingStatus.Active;
                    _state.OnboardingIndex = 0;
                    _state.OnboardingStartedAt = now;
                    LogGuide(OnboardingRules.StartStep, "start", "existing", now);
                }

                _state.IsDirty = true;
                _questsDirty = true;
            }
            else if (_state.OnboardingNew)
            {
                _state.OnboardingStartedAt ??= now;
                LogGuide(OnboardingRules.StartStep, "start", "new", now);
            }

            _state.OnboardingNew = false;

            // A catalog with fewer steps than the player has claimed: the guide is over.
            if (_state.OnboardingStatus == OnboardingStatus.Active && _state.OnboardingIndex >= GuideSteps.Length)
            {
                _state.OnboardingStatus = OnboardingStatus.Done;
                _state.IsDirty = true;
            }
        }

        /// <summary>Claims the current guide step. Caller holds the lock (command dispatch).</summary>
        private void ClaimGuideStep(ClaimQuestCommand command, QuestData step, int index, DateTimeOffset now)
        {
            var verdict = _state.OnboardingStatus != OnboardingStatus.Active || index != _state.OnboardingIndex ? QuestRejectReason.NotCurrent
                : !QuestRules.IsDone(step, Context(false)) ? QuestRejectReason.NotDone
                : QuestRejectReason.None;
            if (verdict != QuestRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            _state.OnboardingIndex = index + 1;
            if (_state.OnboardingIndex >= GuideSteps.Length)
                _state.OnboardingStatus = OnboardingStatus.Done;
            LogGuide(index + 1, step.id, string.Empty, now);
            GrantAndReport(command.CommandId, command.ClientSequence, QuestRules.Resolve(_catalog, step.rewards, Random.Shared.Next), now, capsules: false);
        }

        /// <summary>The current guide step for the quest state; null once done or skipped.</summary>
        private void FillGuide(QuestStateDto dto)
        {
            var steps = GuideSteps;
            dto.GuideTotal = steps.Length;
            if (_state.OnboardingStatus != OnboardingStatus.Active || _state.OnboardingIndex >= steps.Length)
                return;
            var step = steps[_state.OnboardingIndex];
            dto.Guide = new QuestProgressDto { Id = step.id, Progress = QuestRules.Progress(step, Context(false)), Target = Math.Max(1, step.count) };
            dto.GuideNumber = _state.OnboardingIndex + 1;
        }

        private void LogGuide(int step, string stepId, string cohort, DateTimeOffset now)
        {
            var seconds = _state.OnboardingStartedAt is { } started ? (int)Math.Clamp((now - started).TotalSeconds, 0, int.MaxValue) : 0;
            OnboardingLog?.Invoke(new OnboardingEvent(PublicId, step, stepId, cohort, now, seconds));
        }
    }
}
