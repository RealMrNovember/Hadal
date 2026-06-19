using UnityEngine;

namespace Hadal.Data.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Hadal/Config/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Performance")]
        [SerializeField] private MobilePerformanceConfigSO _mobilePerformanceConfig;

        [Header("Circular Grid")]
        [SerializeField] private CircularGridConfigSO _circularGridConfig;

        [Header("Base")]
        [SerializeField] private float _baseTickIntervalSeconds = 1f;

        [Header("Pressure")]
        [SerializeField] private float _oxygenDepletionWarningThreshold = 0.15f;
        [SerializeField] private float _criticalPressureMultiplier = 1.5f;

        [Header("Expedition")]
        [SerializeField] private int _maxHeroesPerExpedition = 3;
        [SerializeField] private float _expeditionBaseDurationSeconds = 300f;

        [Header("Networking")]
        [SerializeField] private string _gatewayUrl = "wss://gateway.hadal.game";
        [SerializeField] private float _networkHeartbeatInterval = 15f;

        [Header("Addressables")]
        [SerializeField] private AssetCatalogSO _assetCatalog;

        [Header("References")]
        [SerializeField] private ResourceDatabaseSO _resourceDatabase;
        [SerializeField] private BuildingDatabaseSO _buildingDatabase;
        [SerializeField] private HeroDatabaseSO _heroDatabase;
        [SerializeField] private EnemyDatabaseSO _enemyDatabase;
        [SerializeField] private DepthZoneDatabaseSO _depthZoneDatabase;
        [SerializeField] private PressureDatabaseSO _pressureDatabase;

        public MobilePerformanceConfigSO MobilePerformanceConfig => _mobilePerformanceConfig;
        public CircularGridConfigSO CircularGridConfig => _circularGridConfig;
        public int TargetFrameRate => _mobilePerformanceConfig != null ? _mobilePerformanceConfig.TargetFrameRate : 60;
        public bool EnableVSync => _mobilePerformanceConfig != null && _mobilePerformanceConfig.EnableVSync;
        public float BaseTickIntervalSeconds => _baseTickIntervalSeconds;
        public float OxygenDepletionWarningThreshold => _oxygenDepletionWarningThreshold;
        public float CriticalPressureMultiplier => _criticalPressureMultiplier;
        public int MaxHeroesPerExpedition => _maxHeroesPerExpedition;
        public float ExpeditionBaseDurationSeconds => _expeditionBaseDurationSeconds;
        public string GatewayUrl => _gatewayUrl;
        public float NetworkHeartbeatInterval => _networkHeartbeatInterval;
        public AssetCatalogSO AssetCatalog => _assetCatalog;

        public ResourceDatabaseSO ResourceDatabase => _resourceDatabase;
        public BuildingDatabaseSO BuildingDatabase => _buildingDatabase;
        public HeroDatabaseSO HeroDatabase => _heroDatabase;
        public EnemyDatabaseSO EnemyDatabase => _enemyDatabase;
        public DepthZoneDatabaseSO DepthZoneDatabase => _depthZoneDatabase;
        public PressureDatabaseSO PressureDatabase => _pressureDatabase;
    }
}
