using UnityEngine;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.UI
{
    public class UIManager : ManagerBase
    {
        [SerializeField] private Canvas _rootCanvas;
        [SerializeField] private UIViewBase _currentView;

        public Canvas RootCanvas => _rootCanvas;

        protected override void OnInitialize(GameConfigSO config)
        {
            if (_rootCanvas == null)
            {
                var canvasGo = new GameObject("UI_RootCanvas");
                canvasGo.transform.SetParent(transform);
                _rootCanvas = canvasGo.AddComponent<Canvas>();
                _rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
        }

        public void ShowView(UIViewBase view)
        {
            if (_currentView != null)
                _currentView.Hide();

            _currentView = view;
            _currentView?.Show();
        }

        protected override void OnShutdown()
        {
            if (_currentView != null)
            {
                _currentView.Hide();
                _currentView = null;
            }
        }
    }
}
