using System.Collections.Generic;
using UnityEngine;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class AllianceManager : ManagerBase
    {
        private readonly Dictionary<string, AllianceData> _alliances = new();
        private string _playerAllianceId;

        protected override void OnInitialize(GameConfigSO config) { }

        public bool CreateAlliance(string allianceId, string displayName)
        {
            if (_alliances.ContainsKey(allianceId))
                return false;

            _alliances[allianceId] = new AllianceData
            {
                Id = allianceId,
                DisplayName = displayName,
                MemberIds = new List<string>()
            };

            return true;
        }

        public bool JoinAlliance(string allianceId, string playerId)
        {
            if (!_alliances.TryGetValue(allianceId, out var alliance))
                return false;

            if (!alliance.MemberIds.Contains(playerId))
                alliance.MemberIds.Add(playerId);

            _playerAllianceId = allianceId;
            return true;
        }

        public string PlayerAllianceId => _playerAllianceId;
        public IReadOnlyDictionary<string, AllianceData> Alliances => _alliances;

        protected override void OnShutdown()
        {
            _alliances.Clear();
            _playerAllianceId = null;
        }
    }

    public class AllianceData
    {
        public string Id;
        public string DisplayName;
        public List<string> MemberIds = new();
        public bool HasMegaShield;
        public bool HasMegaReactor;
        public bool HasDefenseCannons;
        public bool HasTradeStation;
    }
}
