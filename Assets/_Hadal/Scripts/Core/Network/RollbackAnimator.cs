using System.Collections.Generic;
using System.Linq;
using Hadal.Core.Events;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Smoothly lerps visual cache building state toward server-authoritative values.
    /// </summary>
    public sealed class RollbackAnimator : IRollbackAnimator
    {
        private const float DefaultDurationSeconds = 0.3f;

        private readonly IStateSyncService _stateSync;
        private readonly ILocalEventBus _localBus;
        private readonly List<RollbackJob> _jobs = new();

        public RollbackAnimator(IStateSyncService stateSync, ILocalEventBus localBus)
        {
            _stateSync = stateSync;
            _localBus = localBus;
        }

        public void RequestBuildingRollback(BuildingSaveEntry authoritative, string commandId)
        {
            var buildings = _stateSync.View.Data.buildings?.ToList() ?? new List<BuildingSaveEntry>();
            var index = buildings.FindIndex(b => b.instanceKey == authoritative.instanceKey);

            BuildingSaveEntry from = index >= 0 ? buildings[index] : default;
            _jobs.Add(new RollbackJob
            {
                InstanceKey = authoritative.instanceKey,
                From = from,
                To = authoritative,
                Elapsed = 0f,
                Duration = DefaultDurationSeconds
            });

            _localBus.Publish(new RollbackVisualRequestedEvent
            {
                EntityId = authoritative.instanceKey,
                DurationSeconds = DefaultDurationSeconds
            });

            Debug.Log($"[RollbackAnimator] Rollback requested for command={commandId} entity={authoritative.instanceKey}");
        }

        public void Tick(float deltaTime)
        {
            if (_jobs.Count == 0)
                return;

            var buildings = _stateSync.View.Data.buildings?.ToList() ?? new List<BuildingSaveEntry>();

            for (var i = _jobs.Count - 1; i >= 0; i--)
            {
                var job = _jobs[i];
                job.Elapsed += deltaTime;
                var t = Mathf.Clamp01(job.Elapsed / job.Duration);

                var lerped = new BuildingSaveEntry
                {
                    instanceKey = job.To.instanceKey,
                    definitionId = job.To.definitionId,
                    level = job.To.level,
                    cellRing = Mathf.RoundToInt(Mathf.Lerp(job.From.cellRing, job.To.cellRing, t)),
                    cellSector = Mathf.RoundToInt(Mathf.Lerp(job.From.cellSector, job.To.cellSector, t)),
                    cellX = Mathf.RoundToInt(Mathf.Lerp(job.From.cellX, job.To.cellX, t)),
                    cellY = Mathf.RoundToInt(Mathf.Lerp(job.From.cellY, job.To.cellY, t)),
                    rotation = Mathf.Lerp(job.From.rotation, job.To.rotation, t)
                };

                var index = buildings.FindIndex(b => b.instanceKey == job.InstanceKey);
                if (index >= 0)
                    buildings[index] = lerped;
                else
                    buildings.Add(lerped);

                if (t >= 1f)
                    _jobs.RemoveAt(i);
                else
                    _jobs[i] = job;
            }

            _stateSync.View.Data.buildings = buildings.ToArray();
        }

        private struct RollbackJob
        {
            public string InstanceKey;
            public BuildingSaveEntry From;
            public BuildingSaveEntry To;
            public float Elapsed;
            public float Duration;
        }
    }
}
