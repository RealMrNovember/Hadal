using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Data.Config;
using Hadal.Data.Models;

namespace Hadal.Gameplay.Buildings
{
    public class BuildingView : MonoBehaviour
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private int _level = 1;
        [SerializeField] private PolarGridSlotId _slotId;
        [SerializeField] private float _rotationDegrees;

        private BuildingDefinitionSO _definition;

        public string DefinitionId => _definitionId;
        public int Level => _level;
        public PolarGridSlotId SlotId => _slotId;

        public void Bind(BuildingDefinitionSO definition, BuildingInstanceData instance, Vector3 position, Quaternion rotation)
        {
            _definition = definition;
            _definitionId = definition.Id;
            _level = instance.Level;
            _slotId = instance.SlotId;
            _rotationDegrees = instance.RotationDegrees;
            transform.SetPositionAndRotation(position, rotation);
        }

        public void SetLevel(int level)
        {
            _level = Mathf.Clamp(level, 1, _definition != null ? _definition.MaxLevel : level);
        }
    }
}
