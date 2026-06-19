using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.Tick;
using Hadal.Data.Config;
using Hadal.Data.Events;
using Hadal.Data.Models;
using Hadal.Managers.Base;
using VContainer;

namespace Hadal.Managers
{
    public class ExpeditionManager : ManagerBase, IGameTickable
    {
        [SerializeField] private ExpeditionStartedEventSO _expeditionStartedEvent;

        private readonly List<ExpeditionRun> _activeExpeditions = new();
        private int _maxHeroes;
        private IPressureService _pressureService;
        private IResourceService _resourceService;
        private TickManager _tickManager;

        public int TickPriority => 20;

        protected override void OnInitialize(GameConfigSO config)
        {
            _maxHeroes = config.MaxHeroesPerExpedition;
        }

        [Inject]
        public void InjectServices(IPressureService pressureService, IResourceService resourceService, TickManager tickManager)
        {
            _pressureService = pressureService;
            _resourceService = resourceService;
            _tickManager = tickManager;
        }

        public override void OnPostInject()
        {
            _tickManager?.Register(this);
        }

        public bool TryStartExpedition(ExpeditionParty party, ExpeditionZoneDefinitionSO zone)
        {
            if (zone == null || party.HeroIds == null || party.HeroIds.Length > _maxHeroes)
                return false;

            var depth = Config.DepthZoneDatabase?.Get(zone.DepthZone)?.MinDepthMeters ?? 1000f;
            if (_pressureService != null)
            {
                var pressure = _pressureService.EvaluateDepth(depth);
                if (!pressure.IsSurvivable)
                    return false;
            }

            _activeExpeditions.Add(new ExpeditionRun
            {
                Party = party,
                Zone = zone,
                RemainingSeconds = zone.DurationSeconds,
                IsActive = true
            });

            _expeditionStartedEvent?.Raise(party);
            return true;
        }

        public void Tick(float deltaTime)
        {
            for (var i = _activeExpeditions.Count - 1; i >= 0; i--)
            {
                var run = _activeExpeditions[i];
                if (!run.IsActive)
                    continue;

                run.RemainingSeconds -= deltaTime;
                _activeExpeditions[i] = run;

                if (run.RemainingSeconds <= 0f)
                    CompleteExpedition(i);
            }
        }

        private void CompleteExpedition(int index)
        {
            var run = _activeExpeditions[index];
            run.IsActive = false;
            _activeExpeditions[index] = run;

            if (_resourceService == null)
                return;

            foreach (var reward in run.Zone.Rewards)
                _resourceService.Add(reward.Type, reward.Amount);
        }

        public IReadOnlyList<ExpeditionRun> ActiveExpeditions => _activeExpeditions;

        protected override void OnShutdown()
        {
            if (_tickManager != null)
                _tickManager.Unregister(this);

            _activeExpeditions.Clear();
        }
    }

    public struct ExpeditionRun
    {
        public ExpeditionParty Party;
        public ExpeditionZoneDefinitionSO Zone;
        public float RemainingSeconds;
        public bool IsActive;
    }
}
