#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            var bootstrap = bootstrapGo.AddComponent<GameBootstrap>();

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

            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(SettingsPath + "/GameConfig.asset");
            var so = new SerializedObject(bootstrap);
            so.FindProperty("_gameConfig").objectReferenceValue = config;
            so.FindProperty("_poolRoot").objectReferenceValue = poolRoot.transform;
            so.FindProperty("_managers").arraySize = managers.Length;
            for (var i = 0; i < managers.Length; i++)
                so.FindProperty("_managers").GetArrayElementAtIndex(i).objectReferenceValue = managers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(ScenesPath);
            EditorSceneManager.SaveScene(scene, ScenesPath + "/Bootstrap.unity");
            Debug.Log("[Hadal] Bootstrap scene created.");
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
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                var folder = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
                    AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static ResourceDatabaseSO CreateResourceDatabase()
        {
            var db = GetOrCreate<ResourceDatabaseSO>(SettingsPath + "/ResourceDatabase.asset");
            db.GetType(); // keep reference

            CreateResource("Resource_Oxygen", ResourceType.Oxygen, "Oxygen", "Base survival resource.", 500, true);
            CreateResource("Resource_Energy", ResourceType.Energy, "Energy", "Reactor output.", 200, false);
            CreateResource("Resource_Food", ResourceType.Food, "Food", "Crew sustenance.", 300, true);
            CreateResource("Resource_Biomass", ResourceType.Biomass, "Biomass", "Organic material.", 100, false);
            CreateResource("Resource_Titanium", ResourceType.Titanium, "Titanium", "Construction alloy.", 150, false);
            CreateResource("Resource_Hadalite", ResourceType.Hadalite, "Hadalite", "Deep abyss crystal.", 0, false);

            EditorUtility.SetDirty(db);
            return db;
        }

        private static void CreateResource(string file, ResourceType type, string name, string desc, long start, bool critical)
        {
            var asset = GetOrCreate<ResourceDefinitionSO>(SettingsPath + "/" + file + ".asset");
            var so = new SerializedObject(asset);
            so.FindProperty("_type").enumValueIndex = (int)type;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_startingAmount").longValue = start;
            so.FindProperty("_isCritical").boolValue = critical;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BuildingDatabaseSO CreateBuildingDatabase()
        {
            var db = GetOrCreate<BuildingDatabaseSO>(SettingsPath + "/BuildingDatabase.asset");
            CreateBuilding("core_reactor", "Core Reactor", BuildingCategory.Core, GridSlotType.Core, 0, 0);
            CreateBuilding("oxygen_plant", "Oxygen Plant", BuildingCategory.Production, GridSlotType.Production, 1, 1);
            CreateBuilding("food_lab", "Food Lab", BuildingCategory.Production, GridSlotType.Production, 1, 1);
            CreateBuilding("bio_lab", "Bio Lab", BuildingCategory.Production, GridSlotType.Production, 1, 1);
            CreateBuilding("research_center", "Research Center", BuildingCategory.Research, GridSlotType.Research, 2, 2);
            CreateBuilding("drone_factory", "Drone Factory", BuildingCategory.Infrastructure, GridSlotType.Research, 2, 2);
            CreateBuilding("submarine_dock", "Submarine Dock", BuildingCategory.Infrastructure, GridSlotType.Research, 2, 2);
            CreateBuilding("military_dome", "Military Dome", BuildingCategory.Military, GridSlotType.Military, 3, 3);
            CreateBuilding("shield_generator", "Shield Generator", BuildingCategory.Military, GridSlotType.Military, 3, 3);
            CreateBuilding("radar_center", "Radar Center", BuildingCategory.Military, GridSlotType.Military, 3, 3);
            CreateBuilding("mega_dome", "Mega Dome", BuildingCategory.Dome, GridSlotType.MegaDome, 4, 12, DomeType.Mega);
            CreateBuilding("alliance_hub", "Alliance Hub", BuildingCategory.Infrastructure, GridSlotType.Alliance, 4, 12);
            CreateBuilding("decoration_spire", "Decoration Spire", BuildingCategory.Infrastructure, GridSlotType.Decoration, 4, 12);
            EditorUtility.SetDirty(db);
            return db;
        }

        private static void CreateBuilding(string id, string name, BuildingCategory cat, GridSlotType slotType, int minRing, int maxRing, DomeType dome = DomeType.None)
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
