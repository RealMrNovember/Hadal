using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Models;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class HeroManager : ManagerBase, ISaveParticipant
    {
        private HeroDatabaseSO _database;
        private readonly List<HeroInstance> _roster = new();

        protected override void OnInitialize(GameConfigSO config)
        {
            _database = config.HeroDatabase;
        }

        public HeroInstance? RecruitHero(string definitionId)
        {
            var def = _database?.GetById(definitionId);
            if (def == null)
                return null;

            var instance = new HeroInstance
            {
                InstanceId = System.Guid.NewGuid().ToString("N"),
                DefinitionId = definitionId,
                Level = 1,
                Experience = 0,
                Faction = def.Faction
            };

            _roster.Add(instance);
            return instance;
        }

        public HeroDefinitionSO GetDefinition(string definitionId) => _database?.GetById(definitionId);

        public IReadOnlyList<HeroInstance> Roster => _roster;

        public void CaptureSave(SaveGameData data)
        {
            var entries = new HeroSaveEntry[_roster.Count];
            for (var i = 0; i < _roster.Count; i++)
            {
                var hero = _roster[i];
                entries[i] = new HeroSaveEntry
                {
                    instanceId = hero.InstanceId,
                    definitionId = hero.DefinitionId,
                    level = hero.Level,
                    experience = hero.Experience,
                    faction = (int)hero.Faction
                };
            }

            data.heroes = entries;
        }

        public void RestoreSave(SaveGameData data)
        {
            _roster.Clear();
            if (data.heroes == null)
                return;

            foreach (var entry in data.heroes)
            {
                _roster.Add(new HeroInstance
                {
                    InstanceId = entry.instanceId,
                    DefinitionId = entry.definitionId,
                    Level = entry.level,
                    Experience = entry.experience,
                    Faction = (Data.Enums.FactionType)entry.faction
                });
            }
        }

        protected override void OnShutdown() => _roster.Clear();
    }
}
