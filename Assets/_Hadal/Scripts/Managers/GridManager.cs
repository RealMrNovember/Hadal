using Hadal.Data.Models;
using Hadal.Managers.Base;
using VContainer;

namespace Hadal.Managers
{
    /// <summary>
    /// Scene grid manager hook — persistence delegated to injectable <see cref="CircularGridManager"/>.
    /// </summary>
    public class GridManager : ManagerBase, ISaveParticipant
    {
        private CircularGridManager _grid;

        protected override void OnInitialize(Data.Config.GameConfigSO config) { }

        [Inject]
        public void InjectGrid(CircularGridManager grid)
        {
            _grid = grid;
        }

        public void CaptureSave(SaveGameData data) => _grid?.CaptureSave(data);

        public void RestoreSave(SaveGameData data) => _grid?.RestoreSave(data);

        protected override void OnShutdown() => _grid = null;
    }
}
