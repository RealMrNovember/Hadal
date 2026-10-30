using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using Hadal.Core.Events;
using Hadal.Core.Services;
using Hadal.Core.State;
using Hadal.Core.StateSync;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Picks the music for what is on screen (title, base, world map, a fight under way) and plays the
    /// effects for what the server confirms: builds, research, marches, rewards, rejections. Timers that run
    /// out on their own (a building finishing, a march coming home) are noticed from the state.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        /// <summary>Battle music holds this long after a fight's report arrives.</summary>
        private const float BattleHoldSeconds = 6f;

        private IStateSyncService _stateSync;
        private ILocalEventBus _localBus;
        private float _nextCheck;
        private static float _battleUntil;
        private int _constructions = -1;
        private int _research = -1;
        private float _finishedByHand;
        private readonly Dictionary<string, MarchPhase> _marches = new();

        /// <summary>True while the title screen is up; the title plays its own loop.</summary>
        public static bool TitleShowing { get; set; }

        public void Initialize(IStateSyncService stateSync, ILocalEventBus localBus)
        {
            _stateSync = stateSync;
            _localBus = localBus;
            _localBus?.Subscribe<CommandResolvedEvent>(OnCommandResolved);
        }

        private void OnDestroy() => _localBus?.Unsubscribe<CommandResolvedEvent>(OnCommandResolved);

        /// <summary>A fight just resolved: keep the battle music a moment longer, then the sting.</summary>
        public static void BattleResolved(bool won)
        {
            GameAudio.Play(Sfx.Explosion);
            GameAudio.Sting(won);
            _battleUntil = Time.unscaledTime + BattleHoldSeconds;
        }

        private void OnCommandResolved(CommandResolvedEvent result)
        {
            if (result.Result == CommandResultCode.Rejected)
            {
                // Silent background requests (map views, profiles, rankings) are not the player's doing.
                if (result.Type is not (CommandType.ViewWorld or CommandType.ViewProfile or CommandType.ViewEventRanking or CommandType.ViewEventCalendar
                    or CommandType.BrowseAlliances or CommandType.ClientAuth))
                    GameAudio.Play(Sfx.Error);
                return;
            }

            var sfx = Effect(result.Type);
            if (sfx.HasValue)
                GameAudio.Play(sfx.Value);
            if (result.Type is CommandType.MarchGather or CommandType.MarchAttack)
                GameAudio.Play(Sfx.SubTravel, 0.8f);
            if (result.Type is CommandType.FinishConstruction or CommandType.FinishResearch)
                _finishedByHand = Time.unscaledTime;
        }

        public static Sfx? Effect(CommandType type) => type switch
        {
            CommandType.PlaceBuilding or CommandType.UpgradeBuilding or CommandType.UnlockRing => Sfx.BuildStart,
            CommandType.FinishConstruction => Sfx.BuildComplete,
            CommandType.StartResearch or CommandType.TrainTroops or CommandType.HealTroops or CommandType.StartSalvage => Sfx.BuildStart,
            CommandType.FinishResearch => Sfx.ResearchComplete,
            CommandType.BoostBuilding or CommandType.Overdrive or CommandType.UseSpeedup => Sfx.Speedup,
            CommandType.ClaimQuest or CommandType.ClaimEventStage or CommandType.ClaimEventRank => Sfx.Reward,
            CommandType.ClaimDailyChest or CommandType.OpenItem or CommandType.RecruitHero or CommandType.ExecuteGacha => Sfx.ChestOpen,
            CommandType.LevelUpHero or CommandType.PromoteHero => Sfx.LevelUp,
            CommandType.ClaimVipLogin or CommandType.ClaimVipChest => Sfx.VipClaim,
            CommandType.MarchGather or CommandType.MarchAttack => Sfx.MarchLaunch,
            CommandType.RecallMarch => Sfx.SonarPing,
            CommandType.Teleport => Sfx.Teleport,
            CommandType.HelpAll => Sfx.Collect,
            _ => null
        };

        private void Update()
        {
            if (Time.unscaledTime < _nextCheck)
                return;
            _nextCheck = Time.unscaledTime + 0.25f;

            var view = _stateSync?.View;
            if (view != null)
            {
                WatchTimers(view);
                WatchMarches(view);
            }

            GameAudio.SetMusic(TitleShowing ? MusicTrack.Menu
                : Fighting(view) ? MusicTrack.Battle
                : GameMode.InWorld ? MusicTrack.World
                : MusicTrack.Base);
        }

        /// <summary>A team is on its way to a fight, or one just ended.</summary>
        private bool Fighting(ClientStateView view)
        {
            if (Time.unscaledTime < _battleUntil)
                return true;
            foreach (var march in view?.Marches?.Marches ?? System.Array.Empty<MarchDto>())
            {
                if (march.Kind == "attack" && march.Phase == MarchPhase.Outbound)
                    return true;
            }

            return false;
        }

        private void WatchTimers(ClientStateView view)
        {
            var constructions = 0;
            foreach (var building in view.Data.buildings ?? System.Array.Empty<Hadal.Data.Models.BuildingSaveEntry>())
            {
                if (building.IsUnderConstruction)
                    constructions++;
            }

            var research = view.ActiveResearch.Count;
            var byHand = Time.unscaledTime - _finishedByHand < 3f;
            if (_constructions >= 0 && constructions < _constructions && !byHand)
                GameAudio.Play(Sfx.BuildComplete);
            if (_research >= 0 && research < _research && !byHand)
                GameAudio.Play(Sfx.ResearchComplete);
            _constructions = constructions;
            _research = research;
        }

        private void WatchMarches(ClientStateView view)
        {
            var marches = view.Marches?.Marches;
            if (marches == null)
                return;

            var seen = new HashSet<string>();
            foreach (var march in marches)
            {
                seen.Add(march.Id);
                if (_marches.TryGetValue(march.Id, out var before) && before == MarchPhase.Outbound && march.Phase != MarchPhase.Outbound)
                    GameAudio.Play(march.Kind == "attack" ? Sfx.BattleHit : Sfx.SonarPing);
                _marches[march.Id] = march.Phase;
            }

            // A march gone from the list is home: its cargo is in the stores.
            var home = false;
            foreach (var id in new List<string>(_marches.Keys))
            {
                if (seen.Contains(id))
                    continue;
                _marches.Remove(id);
                home = true;
            }

            if (home)
                GameAudio.Play(Sfx.Collect);
        }
    }
}
