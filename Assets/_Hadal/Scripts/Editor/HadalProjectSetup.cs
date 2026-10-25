#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Events;
using Hadal.Data.Models;
using Hadal.Gameplay.Bootstrap;
using Hadal.Managers;
using Hadal.Managers.Base;
using Hadal.Managers.Bootstrap;
using Hadal.UI;

namespace Hadal.EditorTools
{
    public static class HadalProjectSetup
    {
        private const string Root = "Assets/_Hadal";
        private const string SettingsPath = Root + "/Settings/ScriptableObjects";
        private const string EventsPath = SettingsPath + "/Events";
        private const string ScenesPath = Root + "/Scenes";

        [MenuItem("Hadal/Setup/Create Full Project Data")]
        public static void CreateFullProjectData()
        {
            EnsureFolders();
            var resources = CreateResourceDatabase();
            var buildings = CreateBuildingDatabase();
            var heroes = CreateHeroDatabase();
            var enemies = CreateEnemyDatabase();
            var depthZones = CreateDepthZoneDatabase();
            var pressure = CreatePressureDatabase();
            CreateEventChannels();
            var mobilePerf = CreateMobilePerformanceConfig();
            var assetCatalog = CreateAssetCatalog();
            var gridConfig = CreateCircularGridConfig();
            CreateGameConfig(resources, buildings, heroes, enemies, depthZones, pressure, mobilePerf, assetCatalog, gridConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hadal] ScriptableObject data created under " + SettingsPath);
        }

        [MenuItem("Hadal/Setup/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            CreateFullProjectData();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapGo = new GameObject("GameBootstrap");
            var bootstrap = bootstrapGo.AddComponent<ProjectLifetimeScope>();

            var managersRoot = new GameObject("Managers");
            managersRoot.transform.SetParent(bootstrapGo.transform);

            var poolRoot = new GameObject("PoolRoot");
            poolRoot.transform.SetParent(bootstrapGo.transform);

            bootstrapGo.AddComponent<GameStateRegistrar>();

            var managers = new ManagerBase[]
            {
                CreateManager<TickManager>(managersRoot, 5),
                CreateManager<TimeManager>(managersRoot, 0),
                CreateManager<SaveManager>(managersRoot, 10),
                CreateManager<NetworkManager>(managersRoot, 20),
                CreateManager<GameManager>(managersRoot, 30),
                CreateManager<ResourceManager>(managersRoot, 40),
                CreateManager<GridManager>(managersRoot, 50),
                CreateManager<BuildingManager>(managersRoot, 60),
                CreateManager<PressureManager>(managersRoot, 70),
                CreateManager<HeroManager>(managersRoot, 80),
                CreateManager<ExpeditionManager>(managersRoot, 90),
                CreateManager<CombatManager>(managersRoot, 100),
                CreateManager<MapManager>(managersRoot, 110),
                CreateManager<AllianceManager>(managersRoot, 120),
                CreateManager<InventoryManager>(managersRoot, 130),
                CreateManager<AudioManager>(managersRoot, 140),
                CreateManager<UIManager>(managersRoot, 150)
            };

            var hud = CreateResourceHud(bootstrapGo.transform);

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(SettingsPath + "/GameConfig.asset");
            var so = new SerializedObject(bootstrap);
            so.FindProperty("_gameConfig").objectReferenceValue = config;
            so.FindProperty("_poolRoot").objectReferenceValue = poolRoot.transform;
            so.FindProperty("_managers").arraySize = managers.Length;
            for (var i = 0; i < managers.Length; i++)
                so.FindProperty("_managers").GetArrayElementAtIndex(i).objectReferenceValue = managers[i];

            // VContainer only injects scene MonoBehaviours it is told about; [Inject] on the HUD
            // is otherwise never called and the HUD stays blank.
            var autoInject = so.FindProperty("autoInjectGameObjects");
            autoInject.arraySize = 1;
            autoInject.GetArrayElementAtIndex(0).objectReferenceValue = hud;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(ScenesPath);
            EditorSceneManager.SaveScene(scene, ScenesPath + "/Bootstrap.unity");

            // First (and for now only) scene in player builds.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenesPath + "/Bootstrap.unity", true) };
            Debug.Log("[Hadal] Bootstrap scene created.");
        }

        private static readonly ResourceType[] HudResources =
        {
            ResourceType.Oxygen, ResourceType.Energy, ResourceType.Food,
            ResourceType.Biomass, ResourceType.Titanium, ResourceType.Hadalite
        };

        private static GameObject CreateResourceHud(Transform parent)
        {
            var canvasGo = new GameObject("ResourceHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var layout = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            layout.transform.SetParent(canvasGo.transform, false);
            var layoutRect = (RectTransform)layout.transform;
            layoutRect.anchorMin = layoutRect.anchorMax = layoutRect.pivot = new Vector2(0f, 1f);
            layoutRect.anchoredPosition = new Vector2(16f, -16f);
            layoutRect.sizeDelta = new Vector2(320f, 28f * HudResources.Length);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var view = canvasGo.AddComponent<ResourceHudView>();
            var so = new SerializedObject(view);
            var rows = so.FindProperty("_rows");
            rows.arraySize = HudResources.Length;

            for (var i = 0; i < HudResources.Length; i++)
            {
                var rowGo = new GameObject(HudResources[i].ToString(), typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rowGo.transform.SetParent(layout.transform, false);

                CreateLabel(rowGo.transform, "Name", HudResources[i].ToString(), font);
                var amount = CreateLabel(rowGo.transform, "Amount", "0", font);
                var rate = CreateLabel(rowGo.transform, "Rate", string.Empty, font);

                var row = rows.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("type").intValue = (int)HudResources[i];
                row.FindPropertyRelative("amountLabel").objectReferenceValue = amount;
                row.FindPropertyRelative("rateLabel").objectReferenceValue = rate;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return canvasGo;
        }

        private static Text CreateLabel(Transform parent, string name, string text, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.fontSize = 18;
            label.text = text;
            label.color = Color.white;
            return label;
        }

        private static T CreateManager<T>(GameObject parent, int priority) where T : ManagerBase
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(parent.transform);
            var manager = go.AddComponent<T>();
            var so = new SerializedObject(manager);
            so.FindProperty("_priority").intValue = priority;
            so.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root + "/Art/Buildings");
            EnsureFolder(Root + "/Art/Characters");
            EnsureFolder(Root + "/Art/Environment");
            EnsureFolder(Root + "/Art/FX");
            EnsureFolder(Root + "/Addressables");
            EnsureFolder(Root + "/Prefabs/Buildings");
            EnsureFolder(Root + "/Prefabs/Characters");
            EnsureFolder(Root + "/Prefabs/UI");
            EnsureFolder(SettingsPath);
            EnsureFolder(EventsPath);
            EnsureFolder(ScenesPath);
            EnsureFolder(Root + "/Settings/URP");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folder = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folder))
                return;

            // CreateFolder only creates one level; build missing parents first (clean clone).
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private const string CatalogPath = "shared/catalog/catalog.json";

        private static GameCatalog _catalog;

        /// <summary>
        /// Resources, buildings and grid layout come from the shared catalog — the same file the
        /// server validates and simulates with — so client data cannot drift from server rules.
        /// </summary>
        private static GameCatalog Catalog
        {
            get
            {
                if (_catalog != null)
                    return _catalog;

                var path = Path.Combine(Directory.GetCurrentDirectory(), CatalogPath);
                if (!File.Exists(path))
                    throw new FileNotFoundException("Shared game catalog not found.", path);

                _catalog = GameCatalog.Create(JsonUtility.FromJson<GameCatalogData>(File.ReadAllText(path)));
                return _catalog;
            }
        }

        private static ResourceDatabaseSO CreateResourceDatabase()
        {
            var db = GetOrCreate<ResourceDatabaseSO>(SettingsPath + "/ResourceDatabase.asset");
            var resources = new List<Object>();

            foreach (var data in Catalog.Data.resources)
            {
                GameCatalog.TryParseResource(data.type, out var type);
                var asset = GetOrCreate<ResourceDefinitionSO>(SettingsPath + "/Resource_" + type + ".asset");
                var so = new SerializedObject(asset);
                // intValue writes the enum's numeric value; enumValueIndex is the declaration index,
                // which silently diverges from the wire value once the enum has gaps.
                so.FindProperty("_type").intValue = (int)type;
                so.FindProperty("_displayName").stringValue = data.displayName;
                so.FindProperty("_description").stringValue = data.description;
                so.FindProperty("_startingAmount").longValue = data.startingAmount;
                so.FindProperty("_maxCapacity").longValue = data.maxCapacity;
                so.FindProperty("_isCritical").boolValue = data.critical;
                so.ApplyModifiedPropertiesWithoutUndo();
                resources.Add(asset);
            }

            SetObjectList(db, "_resources", resources.ToArray());
            return db;
        }

        private static BuildingDatabaseSO CreateBuildingDatabase()
        {
            var db = GetOrCreate<BuildingDatabaseSO>(SettingsPath + "/BuildingDatabase.asset");
            var buildings = new List<Object>();

            foreach (var data in Catalog.Data.buildings)
            {
                Catalog.TryGetBuilding(data.id, out var definition);
                var asset = GetOrCreate<BuildingDefinitionSO>(SettingsPath + "/Building_" + data.id + ".asset");
                var so = new SerializedObject(asset);
                so.FindProperty("_id").stringValue = data.id;
                so.FindProperty("_displayName").stringValue = data.displayName;
                so.FindProperty("_category").intValue = (int)ParseClientEnum<BuildingCategory>(data.category, data.id);
                so.FindProperty("_domeType").intValue = (int)ParseClientEnum<DomeType>(data.dome, data.id);
                so.FindProperty("_requiredSlotType").intValue = (int)definition.SlotType;
                so.FindProperty("_minRing").intValue = definition.MinRing;
                so.FindProperty("_maxRing").intValue = definition.MaxRing;
                WriteAmounts(so.FindProperty("_productionPerTick"), definition.Production);
                WriteAmounts(so.FindProperty("_consumptionPerTick"), definition.Consumption);
                WriteAmounts(so.FindProperty("_buildCosts"), definition.BuildCost);
                so.ApplyModifiedPropertiesWithoutUndo();
                buildings.Add(asset);
            }

            SetObjectList(db, "_buildings", buildings.ToArray());
            return db;
        }

        private static T ParseClientEnum<T>(string value, string context) where T : struct
        {
            if (System.Enum.TryParse<T>(string.IsNullOrEmpty(value) ? "None" : value, out var parsed))
                return parsed;

            throw new System.FormatException($"Catalog building '{context}': unknown {typeof(T).Name} '{value}'.");
        }

        private static void WriteAmounts(SerializedProperty list, IReadOnlyList<KeyValuePair<ResourceType, long>> amounts)
        {
            list.arraySize = amounts.Count;
            for (var i = 0; i < amounts.Count; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Type").intValue = (int)amounts[i].Key;
                element.FindPropertyRelative("Amount").longValue = amounts[i].Value;
            }
        }

        private static void SetObjectList(Object owner, string propertyName, Object[] items)
        {
            var so = new SerializedObject(owner);
            var list = so.FindProperty(propertyName);
            list.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }

        private static HeroDatabaseSO CreateHeroDatabase()
        {
            var db = GetOrCreate<HeroDatabaseSO>(SettingsPath + "/HeroDatabase.asset");
            var aelis = GetOrCreate<HeroDefinitionSO>(SettingsPath + "/Hero_Aelis.asset");
            var so = new SerializedObject(aelis);
            so.FindProperty("_id").stringValue = "aelis";
            so.FindProperty("_displayName").stringValue = "Aelis";
            so.FindProperty("_heroClass").enumValueIndex = (int)HeroClass.Scientist;
            so.FindProperty("_faction").enumValueIndex = (int)FactionType.Deepborn;
            so.FindProperty("_rarity").enumValueIndex = (int)HeroRarity.Legendary;
            so.FindProperty("_lore").stringValue = "Revived by Hadalite crystals after a fatal expedition.";
            so.FindProperty("_skillName").stringValue = "Ocean Memory";
            so.FindProperty("_ultimateName").stringValue = "Abyss Bloom";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            return db;
        }

        private static EnemyDatabaseSO CreateEnemyDatabase()
        {
            var db = GetOrCreate<EnemyDatabaseSO>(SettingsPath + "/EnemyDatabase.asset");
            CreateEnemy("abyss_crawler", EnemyType.AbyssCrawler, "Abyss Crawler", false, DepthZone.MidAbyss);
            CreateEnemy("siren_swarm", EnemyType.SirenSwarm, "Siren Swarm", false, DepthZone.DarkAbyss);
            CreateEnemy("iron_leviathan", EnemyType.IronLeviathan, "Iron Leviathan", true, DepthZone.ForbiddenAbyss);
            CreateEnemy("the_forgotten", EnemyType.TheForgotten, "The Forgotten", false, DepthZone.HadalTrench);
            CreateEnemy("titan_of_hadal", EnemyType.TitanOfHadal, "Titan of Hadal", true, DepthZone.TheCore);
            EditorUtility.SetDirty(db);
            return db;
        }

        private static void CreateEnemy(string id, EnemyType type, string name, bool boss, DepthZone zone)
        {
            var asset = GetOrCreate<EnemyDefinitionSO>(SettingsPath + "/Enemy_" + id + ".asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_type").enumValueIndex = (int)type;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_isBoss").boolValue = boss;
            so.FindProperty("_minZone").enumValueIndex = (int)zone;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DepthZoneDatabaseSO CreateDepthZoneDatabase()
        {
            var db = GetOrCreate<DepthZoneDatabaseSO>(SettingsPath + "/DepthZoneDatabase.asset");
            CreateDepthZone(DepthZone.SafeZone, "Safe Zone", 0, 1000, 1f, 0.5f, false);
            CreateDepthZone(DepthZone.MidAbyss, "Mid Abyss", 1000, 3000, 1.2f, 1f, false);
            CreateDepthZone(DepthZone.DarkAbyss, "Dark Abyss", 3000, 5000, 1.5f, 1.5f, false);
            CreateDepthZone(DepthZone.ForbiddenAbyss, "Forbidden Abyss", 5000, 7000, 2f, 2f, true);
            CreateDepthZone(DepthZone.HadalTrench, "Hadal Trench", 7000, 11000, 3f, 3f, true);
            CreateDepthZone(DepthZone.TheCore, "The Core", 11000, 12000, 5f, 5f, true);
            EditorUtility.SetDirty(db);
            return db;
        }

        private static void CreateDepthZone(DepthZone zone, string name, float min, float max, float res, float danger, bool sonar)
        {
            var asset = GetOrCreate<DepthZoneDefinitionSO>(SettingsPath + "/Zone_" + zone + ".asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_zone").enumValueIndex = (int)zone;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_minDepthMeters").floatValue = min;
            so.FindProperty("_maxDepthMeters").floatValue = max;
            so.FindProperty("_resourceMultiplier").floatValue = res;
            so.FindProperty("_dangerMultiplier").floatValue = danger;
            so.FindProperty("_sonarOnly").boolValue = sonar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PressureDatabaseSO CreatePressureDatabase()
        {
            var db = GetOrCreate<PressureDatabaseSO>(SettingsPath + "/PressureDatabase.asset");
            CreatePressureTier(PressureTier.Tier1000m, 1000, 100, 200, 100);
            CreatePressureTier(PressureTier.Tier3000m, 3000, 350, 500, 250);
            CreatePressureTier(PressureTier.Tier5000m, 5000, 650, 900, 450);
            CreatePressureTier(PressureTier.Tier7000m, 7000, 1000, 1400, 700);
            CreatePressureTier(PressureTier.Tier11000m, 11000, 1800, 2200, 1200);
            EditorUtility.SetDirty(db);
            return db;
        }

        private static void CreatePressureTier(PressureTier tier, float depth, float pressure, float hull, float shield)
        {
            var asset = GetOrCreate<PressureTierDefinitionSO>(SettingsPath + "/Pressure_" + tier + ".asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_tier").enumValueIndex = (int)tier;
            so.FindProperty("_depthMeters").floatValue = depth;
            so.FindProperty("_pressureValue").floatValue = pressure;
            so.FindProperty("_requiredHullStrength").floatValue = hull;
            so.FindProperty("_requiredPressureShield").floatValue = shield;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateEventChannels()
        {
            GetOrCreate<ResourceChangedEventSO>(EventsPath + "/ResourceChangedEvent.asset");
            GetOrCreate<PressureChangedEventSO>(EventsPath + "/PressureChangedEvent.asset");
            GetOrCreate<BuildingPlacedEventSO>(EventsPath + "/BuildingPlacedEvent.asset");
            GetOrCreate<ExpeditionStartedEventSO>(EventsPath + "/ExpeditionStartedEvent.asset");
            GetOrCreate<GridSlotHighlightEventSO>(EventsPath + "/GridSlotHighlightEvent.asset");
            GetOrCreate<BuildPreviewChangedEventSO>(EventsPath + "/BuildPreviewChangedEvent.asset");
        }

        private static CircularGridConfigSO CreateCircularGridConfig()
        {
            var grid = Catalog.Grid;
            var config = GetOrCreate<CircularGridConfigSO>(SettingsPath + "/CircularGridConfig.asset");
            var so = new SerializedObject(config);
            so.FindProperty("_slotYOffset").floatValue = grid.slotYOffset;
            so.FindProperty("_maxRings").intValue = grid.maxRings;
            so.FindProperty("_startingUnlockedRings").intValue = grid.startingUnlockedRings;
            so.FindProperty("_rotationSnapDegrees").floatValue = grid.rotationSnapDegrees;
            so.FindProperty("_outerRingBaseSectors").intValue = grid.outerRingBaseSectors;
            so.FindProperty("_outerRingSectorIncrement").intValue = grid.outerRingSectorIncrement;
            so.FindProperty("_outerRingMaxSectors").intValue = grid.outerRingMaxSectors;

            var rings = so.FindProperty("_rings");
            rings.arraySize = grid.rings.Length;
            for (var r = 0; r < grid.rings.Length; r++)
            {
                var ring = grid.rings[r];
                var ringProp = rings.GetArrayElementAtIndex(r);
                ringProp.FindPropertyRelative("_ringIndex").intValue = ring.ring;
                ringProp.FindPropertyRelative("_radius").floatValue = ring.radius;
                ringProp.FindPropertyRelative("_sectorCount").intValue = ring.sectorCount;
                ringProp.FindPropertyRelative("_startAngleOffset").floatValue = ring.startAngleOffset;
                ringProp.FindPropertyRelative("_defaultSlotType").intValue = (int)SlotType(ring.defaultSlotType);
                ringProp.FindPropertyRelative("_expandable").boolValue = ring.expandable;

                var sectors = ringProp.FindPropertyRelative("_sectors");
                var sectorData = ring.sectors ?? System.Array.Empty<SectorData>();
                sectors.arraySize = sectorData.Length;
                for (var s = 0; s < sectorData.Length; s++)
                {
                    var sector = sectorData[s];
                    var sectorProp = sectors.GetArrayElementAtIndex(s);
                    sectorProp.FindPropertyRelative("_sectorIndex").intValue = sector.sector;
                    sectorProp.FindPropertyRelative("_slotType").intValue = (int)SlotType(sector.slotType);
                    sectorProp.FindPropertyRelative("_preferredBuildingId").stringValue = sector.preferredBuildingId ?? string.Empty;
                    sectorProp.FindPropertyRelative("_startsLocked").boolValue = sector.startsLocked;

                    var allowed = sectorProp.FindPropertyRelative("_allowedBuildingIds");
                    var ids = sector.allowedBuildingIds ?? System.Array.Empty<string>();
                    allowed.arraySize = ids.Length;
                    for (var i = 0; i < ids.Length; i++)
                        allowed.GetArrayElementAtIndex(i).stringValue = ids[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static GridSlotType SlotType(string value)
        {
            GameCatalog.TryParseSlotType(value, out var type);
            return type;
        }

        private static void CreateGameConfig(ResourceDatabaseSO res, BuildingDatabaseSO bld,
            HeroDatabaseSO hero, EnemyDatabaseSO enemy, DepthZoneDatabaseSO zones, PressureDatabaseSO pressure,
            MobilePerformanceConfigSO mobilePerf, AssetCatalogSO assetCatalog, CircularGridConfigSO gridConfig)
        {
            var config = GetOrCreate<GameConfigSO>(SettingsPath + "/GameConfig.asset");
            var so = new SerializedObject(config);
            so.FindProperty("_mobilePerformanceConfig").objectReferenceValue = mobilePerf;
            so.FindProperty("_assetCatalog").objectReferenceValue = assetCatalog;
            so.FindProperty("_circularGridConfig").objectReferenceValue = gridConfig;
            so.FindProperty("_resourceDatabase").objectReferenceValue = res;
            so.FindProperty("_buildingDatabase").objectReferenceValue = bld;
            so.FindProperty("_heroDatabase").objectReferenceValue = hero;
            so.FindProperty("_enemyDatabase").objectReferenceValue = enemy;
            so.FindProperty("_depthZoneDatabase").objectReferenceValue = zones;
            so.FindProperty("_pressureDatabase").objectReferenceValue = pressure;
            // Written explicitly: a field default only applies to newly created assets.
            so.FindProperty("_gatewayUrl").stringValue = "wss://hadal-api.cicibyte.com/gateway";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MobilePerformanceConfigSO CreateMobilePerformanceConfig()
            => GetOrCreate<MobilePerformanceConfigSO>(SettingsPath + "/MobilePerformanceConfig.asset");

        private static AssetCatalogSO CreateAssetCatalog()
            => GetOrCreate<AssetCatalogSO>(SettingsPath + "/AssetCatalog.asset");

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
