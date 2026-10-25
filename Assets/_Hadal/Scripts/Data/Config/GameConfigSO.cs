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
        [Tooltip("Game gateway. hadal.cicibyte.com is the public teaser site; the game lives on hadal-api.")]
        [SerializeField] private string _gatewayUrl = "wss://hadal-api.cicibyte.com/gateway";
        [SerializeField] private float _networkHeartbeatInterval = 15f;

        [Tooltip("Editor/development builds only: run the in-process stub server instead of connecting to the gateway.")]
        [SerializeField] private bool _useLocalSimulation;

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
        public string GatewayUrl => DevOverride("HADAL_GATEWAY_URL") ?? _gatewayUrl;

        /// <summary>
        /// Stub server mode (Phase 1 dev loop). Never true in a release build — the server owns
        /// placement and the economy there.
        /// </summary>
        public bool UseLocalSimulation
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var env = DevOverride("HADAL_LOCAL_SIM");
                return env != null ? env == "1" : _useLocalSimulation;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// HTTP origin of the Auth API and version manifest, derived from the gateway URL
        /// (wss://host/gateway → https://host) so the two can never point at different servers.
        /// </summary>
        public string ApiBaseUrl
        {
            get
            {
                var url = GatewayUrl ?? string.Empty;
                if (url.StartsWith("wss://", System.StringComparison.OrdinalIgnoreCase))
                    url = "https://" + url.Substring(6);
                else if (url.StartsWith("ws://", System.StringComparison.OrdinalIgnoreCase))
                    url = "http://" + url.Substring(5);

                url = url.TrimEnd('/');
                return url.EndsWith("/gateway", System.StringComparison.OrdinalIgnoreCase)
                    ? url.Substring(0, url.Length - "/gateway".Length)
                    : url;
            }
        }

        /// <summary>Editor/development: separate stored login per profile (tests, several local players).</summary>
        public string AuthProfile => DevOverride("HADAL_AUTH_PROFILE");

        /// <summary>Editor/development: forget the stored login and start a brand-new guest account.</summary>
        public bool FreshAccount => DevOverride("HADAL_FRESH_ACCOUNT") == "1";

        public float NetworkHeartbeatInterval => _networkHeartbeatInterval;
        public AssetCatalogSO AssetCatalog => _assetCatalog;

        public ResourceDatabaseSO ResourceDatabase => _resourceDatabase;
        public BuildingDatabaseSO BuildingDatabase => _buildingDatabase;
        public HeroDatabaseSO HeroDatabase => _heroDatabase;
        public EnemyDatabaseSO EnemyDatabase => _enemyDatabase;
        public DepthZoneDatabaseSO DepthZoneDatabase => _depthZoneDatabase;
        public PressureDatabaseSO PressureDatabase => _pressureDatabase;

        private static string DevOverride(string variable)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var value = System.Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrWhiteSpace(value) ? null : value;
#else
            return null;
#endif
        }
    }
}
