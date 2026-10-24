#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
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

        private static ResourceDatabaseSO CreateResourceDatabase()
        {
            var db = GetOrCreate<ResourceDatabaseSO>(SettingsPath + "/ResourceDatabase.asset");

            // PLACEHOLDER BALANCE: starting grants and storage caps are not specified in the design
            // docs yet. They exist so the Sprint 3 economy loop has something to run on.
            var resources = new[]
            {
                CreateResource("Resource_Oxygen", ResourceType.Oxygen, "Oxygen", "Base survival resource.", 500, 5000, true),
                CreateResource("Resource_Energy", ResourceType.Energy, "Energy", "Reactor output.", 200, 2000, false),
                CreateResource("Resource_Food", ResourceType.Food, "Food", "Crew sustenance.", 300, 3000, true),
                CreateResource("Resource_Biomass", ResourceType.Biomass, "Biomass", "Organic material.", 100, 2000, false),
                CreateResource("Resource_Titanium", ResourceType.Titanium, "Titanium", "Construction alloy.", 150, 5000, false),
                CreateResource("Resource_Hadalite", ResourceType.Hadalite, "Hadalite", "Deep abyss crystal.", 0, 1000, false)
            };

            SetObjectList(db, "_resources", resources);
            return db;
        }

        private static ResourceDefinitionSO CreateResource(string file, ResourceType type, string name, string desc,
            long start, long capacity, bool critical)
        {
            var asset = GetOrCreate<ResourceDefinitionSO>(SettingsPath + "/" + file + ".asset");
            var so = new SerializedObject(asset);
            // intValue writes the enum's numeric value; enumValueIndex is the declaration index,
            // which silently diverges from the wire value once the enum has gaps.
            so.FindProperty("_type").intValue = (int)type;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_startingAmount").longValue = start;
            so.FindProperty("_maxCapacity").longValue = capacity;
            so.FindProperty("_isCritical").boolValue = critical;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static BuildingDatabaseSO CreateBuildingDatabase()
        {
            var db = GetOrCreate<BuildingDatabaseSO>(SettingsPath + "/BuildingDatabase.asset");

            var reactor = CreateBuilding("core_reactor", "Core Reactor", BuildingCategory.Core, GridSlotType.Core, 0, 0);
            var oxygen = CreateBuilding("oxygen_plant", "Oxygen Plant", BuildingCategory.Production, GridSlotType.Production, 1, 1);
            var food = CreateBuilding("food_lab", "Food Lab", BuildingCategory.Production, GridSlotType.Production, 1, 1);
            var bio = CreateBuilding("bio_lab", "Bio Lab", BuildingCategory.Production, GridSlotType.Production, 1, 1);

            // PLACEHOLDER BALANCE (per simulation tick at level 1 — see SimulationTick.IntervalSeconds).
            SetEconomy(reactor, production: new[] { (ResourceType.Energy, 4L) });
            SetEconomy(oxygen, production: new[] { (ResourceType.Oxygen, 3L) },
                consumption: new[] { (ResourceType.Energy, 1L) }, costs: new[] { (ResourceType.Titanium, 50L) });
            SetEconomy(food, production: new[] { (ResourceType.Food, 2L) },
                consumption: new[] { (ResourceType.Energy, 1L) }, costs: new[] { (ResourceType.Titanium, 40L) });
            SetEconomy(bio, production: new[] { (ResourceType.Biomass, 1L) },
                consumption: new[] { (ResourceType.Energy, 1L) }, costs: new[] { (ResourceType.Titanium, 60L) });

            var buildings = new[]
            {
                reactor, oxygen, food, bio,
                CreateBuilding("research_center", "Research Center", BuildingCategory.Research, GridSlotType.Research, 2, 2),
                CreateBuilding("drone_factory", "Drone Factory", BuildingCategory.Infrastructure, GridSlotType.Research, 2, 2),
                CreateBuilding("submarine_dock", "Submarine Dock", BuildingCategory.Infrastructure, GridSlotType.Research, 2, 2),
                CreateBuilding("military_dome", "Military Dome", BuildingCategory.Military, GridSlotType.Military, 3, 3),
                CreateBuilding("shield_generator", "Shield Generator", BuildingCategory.Military, GridSlotType.Military, 3, 3),
                CreateBuilding("radar_center", "Radar Center", BuildingCategory.Military, GridSlotType.Military, 3, 3),
                CreateBuilding("mega_dome", "Mega Dome", BuildingCategory.Dome, GridSlotType.MegaDome, 4, 12, DomeType.Mega),
                CreateBuilding("alliance_hub", "Alliance Hub", BuildingCategory.Infrastructure, GridSlotType.Alliance, 4, 12),
                CreateBuilding("decoration_spire", "Decoration Spire", BuildingCategory.Infrastructure, GridSlotType.Decoration, 4, 12)
            };

            SetObjectList(db, "_buildings", buildings);
            return db;
        }

        private static BuildingDefinitionSO CreateBuilding(string id, string name, BuildingCategory cat, GridSlotType slotType, int minRing, int maxRing, DomeType dome = DomeType.None)
        {
            var asset = GetOrCreate<BuildingDefinitionSO>(SettingsPath + "/Building_" + id + ".asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_category").enumValueIndex = (int)cat;
            so.FindProperty("_domeType").enumValueIndex = (int)dome;
            so.FindProperty("_requiredSlotType").enumValueIndex = (int)slotType;
            so.FindProperty("_minRing").intValue = minRing;
            so.FindProperty("_maxRing").intValue = maxRing;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static void SetEconomy(BuildingDefinitionSO building,
            (ResourceType type, long amount)[] production = null,
            (ResourceType type, long amount)[] consumption = null,
            (ResourceType type, long amount)[] costs = null)
        {
            var so = new SerializedObject(building);
            WriteAmounts(so.FindProperty("_productionPerTick"), production);
            WriteAmounts(so.FindProperty("_consumptionPerTick"), consumption);
            WriteAmounts(so.FindProperty("_buildCosts"), costs);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteAmounts(SerializedProperty list, (ResourceType type, long amount)[] amounts)
        {
            amounts ??= System.Array.Empty<(ResourceType, long)>();
            list.arraySize = amounts.Length;
            for (var i = 0; i < amounts.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Type").intValue = (int)amounts[i].type;
                element.FindPropertyRelative("Amount").longValue = amounts[i].amount;
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
            var config = GetOrCreate<CircularGridConfigSO>(SettingsPath + "/CircularGridConfig.asset");
            var so = new SerializedObject(config);
            so.FindProperty("_startingUnlockedRings").intValue = 4;
            so.FindProperty("_maxRings").intValue = 12;
            so.FindProperty("_rotationSnapDegrees").floatValue = 60f;

            var rings = so.FindProperty("_rings");
            rings.arraySize = 5;

            WriteRing(rings.GetArrayElementAtIndex(0), 0, 0f, 1, 0f, GridSlotType.Core, false,
                new[] { ("core_reactor", GridSlotType.Core, 0) });

            WriteRing(rings.GetArrayElementAtIndex(1), 1, 5f, 3, 0f, GridSlotType.Production, false,
                new[] { ("oxygen_plant", GridSlotType.Production, 0), ("food_lab", GridSlotType.Production, 1), ("bio_lab", GridSlotType.Production, 2) });

            WriteRing(rings.GetArrayElementAtIndex(2), 2, 10f, 3, 0f, GridSlotType.Research, false,
                new[] { ("research_center", GridSlotType.Research, 0), ("drone_factory", GridSlotType.Research, 1), ("submarine_dock", GridSlotType.Research, 2) });

            WriteRing(rings.GetArrayElementAtIndex(3), 3, 15f, 3, 0f, GridSlotType.Military, false,
                new[] { ("military_dome", GridSlotType.Military, 0), ("shield_generator", GridSlotType.Military, 1), ("radar_center", GridSlotType.Military, 2) });

            WriteRing(rings.GetArrayElementAtIndex(4), 4, 22f, 6, 0f, GridSlotType.Universal, true,
                System.Array.Empty<(string, GridSlotType, int)>());

            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static void WriteRing(SerializedProperty ringProp, int ringIndex, float radius, int sectorCount,
            float angleOffset, GridSlotType defaultType, bool expandable, (string buildingId, GridSlotType slotType, int sector)[] sectors)
        {
            ringProp.FindPropertyRelative("_ringIndex").intValue = ringIndex;
            ringProp.FindPropertyRelative("_radius").floatValue = radius;
            ringProp.FindPropertyRelative("_sectorCount").intValue = sectorCount;
            ringProp.FindPropertyRelative("_startAngleOffset").floatValue = angleOffset;
            ringProp.FindPropertyRelative("_defaultSlotType").enumValueIndex = (int)defaultType;
            ringProp.FindPropertyRelative("_expandable").boolValue = expandable;

            var sectorArray = ringProp.FindPropertyRelative("_sectors");
            sectorArray.arraySize = sectors.Length;
            for (var i = 0; i < sectors.Length; i++)
            {
                var sectorProp = sectorArray.GetArrayElementAtIndex(i);
                sectorProp.FindPropertyRelative("_sectorIndex").intValue = sectors[i].sector;
                sectorProp.FindPropertyRelative("_slotType").enumValueIndex = (int)sectors[i].slotType;
                sectorProp.FindPropertyRelative("_preferredBuildingId").stringValue = sectors[i].buildingId;
            }
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
