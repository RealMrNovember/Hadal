using UnityEngine;

namespace Hadal.Gameplay.Alliance
{
    public class AllianceCityView : MonoBehaviour
    {
        [SerializeField] private GameObject _megaShield;
        [SerializeField] private GameObject _megaReactor;
        [SerializeField] private GameObject _defenseCannons;
        [SerializeField] private GameObject _tradeStation;

        public void ApplyStructureState(bool megaShield, bool megaReactor, bool defenseCannons, bool tradeStation)
        {
            if (_megaShield != null) _megaShield.SetActive(megaShield);
            if (_megaReactor != null) _megaReactor.SetActive(megaReactor);
            if (_defenseCannons != null) _defenseCannons.SetActive(defenseCannons);
            if (_tradeStation != null) _tradeStation.SetActive(tradeStation);
        }
    }
}
