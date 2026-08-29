using UnityEditor;
using UnityEngine;
using PMF.Actors;
using PMF.Combat;
using PMF.Data;

namespace PMF.EditorTools
{
    /// <summary>그레이박스 프리팹과 ScriptableObject 에셋을 만들고 참조를 엮는다.</summary>
    internal static class GreyboxFactory
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs";
        private const string DataRoot = "Assets/_Project/Data";

        /// <summary>StageDefinition 에셋 경로. SceneParts 가 참조 유실 시 여기서 다시 읽는다.</summary>
        internal const string StageAssetPath = DataRoot + "/Stages/Stage_Greybox.asset";

        internal struct Result
        {
            public GameObject EscorteePrefab;
            public GameObject MotherPrefab;
            public GameObject EnemyPrefab;      // Robot_Walker
            public GameObject ScoutPrefab;      // Robot_Scout (Walker 의 프리팹 변형 — 색만 다르다)
            public GameObject AllyPrefab;       // Ally_CatFolk
            public GameObject RatPrefab;        // Ally_RatFolk
            public GameObject VillagePrefab;
            public StageDefinition Stage;
            public EnemyDefinition EnemyDef;    // Robot_Walker
            public EnemyDefinition ScoutDef;    // Robot_Scout
            public UnitDefinition RatDef;       // Ally_RatFolk
            public UnitDefinition UnitDef;
            public Sprite Square;
            public Sprite Circle;
        }

        /// <summary>P-20 시작값 (튜닝 대상 — SO 에서만 바꾼다).</summary>
        internal static Result BuildAll()
        {
            var result = new Result();
            EnsureFolders();

            result.Square = GreyboxSprites.GetOrCreateSquare();
            result.Circle = GreyboxSprites.GetOrCreateCircle();

            result.EnemyDef = CreateWalkerDefinition();
            result.ScoutDef = CreateScoutDefinition();
            result.UnitDef = LoadOrCreateUnit("Ally_CatFolk", "고양이 수인", 1.6f, 7f, 0.45f, 45);
            result.RatDef = LoadOrCreateUnit("Ally_RatFolk", "쥐 수인", 6f, 26f, 1.8f, 110);
            result.Stage = CreateStageDefinition(result.EnemyDef, result.ScoutDef);

            result.EscorteePrefab = CreateEscorteePrefab();
            result.MotherPrefab = CreateMotherPrefab();
            result.EnemyPrefab = CreateWalkerPrefab(result);
            result.ScoutPrefab = CreateScoutPrefabVariant(result.EnemyPrefab);
            result.AllyPrefab = LoadOrCreateAllyPrefab(result, "Ally_CatFolk", new Color(0.55f, 0.75f, 1f));
            result.RatPrefab = LoadOrCreateAllyPrefab(result, "Ally_RatFolk", new Color(0.75f, 0.7f, 1f));
            result.VillagePrefab = CreateVillagePrefab(result.UnitDef, result.RatDef);

            WireDefinitionPrefabs(result.EnemyDef, result.EnemyPrefab,
                                  result.UnitDef, result.AllyPrefab);
            WirePrefab(result.ScoutDef, result.ScoutPrefab);
            WireUnitPrefab(result.RatDef, result.RatPrefab);

            AssetDatabase.SaveAssets();
            return result;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(PrefabRoot))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            foreach (var sub in new[] { "Units", "Enemies", "Stages" })
            {
                string p = $"{DataRoot}/{sub}";
                if (!AssetDatabase.IsValidFolder(p))
                    AssetDatabase.CreateFolder(DataRoot, sub);
            }
        }

        private static void WireDefinitionPrefabs(EnemyDefinition enemyDef, GameObject enemyPrefab,
                                                  UnitDefinition unitDef, GameObject allyPrefab)
        {
            var so = new SerializedObject(enemyDef);
            so.FindProperty("_prefab").objectReferenceValue = enemyPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(unitDef);
            so.FindProperty("_prefab").objectReferenceValue = allyPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePrefab(EnemyDefinition def, GameObject prefab)
        {
            var so = new SerializedObject(def);
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireUnitPrefab(UnitDefinition def, GameObject prefab)
        {
            var so = new SerializedObject(def);
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SaveAsPrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static SpriteRenderer AddSprite(GameObject go, Sprite sprite, Color color,
                                                string sortingLayer, float scale)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            return renderer;
        }

        private static GameObject CreateEscorteePrefab()
        {
            var go = new GameObject("Escortee");
            AddSprite(go, GreyboxSprites.GetOrCreateHeart(), new Color(0.95f, 0.35f, 0.55f), "Actors", 0.9f);
            go.AddComponent<Health>();
            go.AddComponent<Escortee>();
            return SaveAsPrefab(go, $"{PrefabRoot}/Escortee.prefab");
        }

        private static GameObject CreateMotherPrefab()
        {
            var go = new GameObject("Mother");
            // 진한 자주색 큰 사각 — 스폰 지점임이 한눈에 보이게 유닛보다 크게.
            var purple = new Color(0.42f, 0.12f, 0.55f);
            AddSprite(go, GreyboxSprites.GetOrCreateSquare(), purple, "Actors", 1.5f);
            go.AddComponent<MotherSpawner>();
            return SaveAsPrefab(go, $"{PrefabRoot}/Mother.prefab");
        }

        private static GameObject CreateWalkerPrefab(Result r)
        {
            var go = new GameObject("Robot_Walker");
            AddSprite(go, r.Circle, new Color(0.9f, 0.15f, 0.15f), "Actors", 0.4f);
            go.AddComponent<Health>();
            go.AddComponent<Attacker>();
            go.AddComponent<Enemy>();
            return SaveAsPrefab(go, $"{PrefabRoot}/Robot_Walker.prefab");
        }

        /// <summary>Scout 는 Walker 의 <b>프리팹 변형</b>이다. 색만 오버라이드한다 (G-18 함정).</summary>
        private static GameObject CreateScoutPrefabVariant(GameObject walkerPrefab)
        {
            string path = $"{PrefabRoot}/Robot_Scout.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(walkerPrefab);
            inst.name = "Robot_Scout";
            inst.GetComponent<SpriteRenderer>().color = new Color(1f, 0.62f, 0.16f);   // 주황 — 빠른 개체
            var variant = PrefabUtility.SaveAsPrefabAsset(inst, path);
            Object.DestroyImmediate(inst);
            return variant;
        }

        /// <summary>병종 프리팹도 G-01 에서 2종으로 나뉘었다. 있으면 읽고, 없을 때만 만든다.</summary>
        private static GameObject LoadOrCreateAllyPrefab(Result r, string prefabName, Color color)
        {
            string path = $"{PrefabRoot}/{prefabName}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;   // 있으면 손대지 않는다

            var go = new GameObject(prefabName);
            AddSprite(go, r.Square, color, "Actors", 0.5f);

            var line = go.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = new Color(0.7f, 0.85f, 1f, 0.8f);
            line.startWidth = line.endWidth = 0.06f;
            line.positionCount = 0;
            line.sortingLayerName = "Path";
            line.enabled = false;

            go.AddComponent<Health>();
            go.AddComponent<Attacker>();
            go.AddComponent<AllyUnit>();
            return SaveAsPrefab(go, path);
        }

        private static GameObject CreateVillagePrefab(UnitDefinition catDef, UnitDefinition ratDef)
        {
            var go = new GameObject("Village");
            AddSprite(go, GreyboxSprites.GetOrCreateSquare(), new Color(0.2f, 0.75f, 0.3f), "Deploy", 1.4f);
            var village = go.AddComponent<Village>();

            // 두 마을 모두 병종 2종을 다 고용할 수 있다 (G-01). 자리 차이는 위치가 만든다.
            var so = new SerializedObject(village);
            var array = so.FindProperty("_hireableUnits");
            array.arraySize = 2;
            array.GetArrayElementAtIndex(0).objectReferenceValue = catDef;
            array.GetArrayElementAtIndex(1).objectReferenceValue = ratDef;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAsPrefab(go, $"{PrefabRoot}/Village.prefab");
        }

        /// <summary>느리고 단단한 개체. 근접(고양이)이 회전율로 녹이는 대상 (G-18).</summary>
        private static EnemyDefinition CreateWalkerDefinition()
        {
            string path = $"{DataRoot}/Enemies/Robot_Walker.asset";
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def != null) return def;

            def = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("_displayName").stringValue = "워커";
            so.FindProperty("_moveSpeed").floatValue = 2.0f;
            so.FindProperty("_maxHealth").floatValue = 20f;
            so.FindProperty("_attackRange").floatValue = 1.0f;
            so.FindProperty("_attackDamage").floatValue = 5f;
            so.FindProperty("_attackInterval").floatValue = 1.0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        /// <summary>빠르고 약한 개체. 원거리(쥐)가 넓게 커버해야 잡힌다 (G-18).
        /// 근접만으로 못 막는 것은 버그가 아니라 의도된 압박이다 — 근접 사거리를 올려 해결하지 마라.</summary>
        private static EnemyDefinition CreateScoutDefinition()
        {
            string path = $"{DataRoot}/Enemies/Robot_Scout.asset";
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def != null) return def;

            def = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("_displayName").stringValue = "스카우트";
            so.FindProperty("_moveSpeed").floatValue = 3.4f;
            so.FindProperty("_maxHealth").floatValue = 10f;
            so.FindProperty("_attackRange").floatValue = 1.0f;
            so.FindProperty("_attackDamage").floatValue = 4f;
            so.FindProperty("_attackInterval").floatValue = 1.0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        /// <summary>병종 정의는 G-01 에서 2종(고양이·쥐)으로 확정됐다. 여기서 만들지 않고 <b>있는 것을 읽는다</b>.
        /// 없으면 최소 뼈대만 만든다 — 수치는 SO 에서 저작한다.</summary>
        private static UnitDefinition LoadOrCreateUnit(string assetName, string displayName,
                                                       float range, float damage, float interval, int cost)
        {
            string path = $"{DataRoot}/Units/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
            if (def != null) return def;   // 있으면 손대지 않는다 — 튜닝값 보호

            def = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_moveSpeed").floatValue = 1.12f;
            so.FindProperty("_maxHealth").floatValue = 50f;
            so.FindProperty("_attackRange").floatValue = range;
            so.FindProperty("_attackDamage").floatValue = damage;
            so.FindProperty("_attackInterval").floatValue = interval;
            so.FindProperty("_hireCost").intValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static StageDefinition CreateStageDefinition(EnemyDefinition walkerDef, EnemyDefinition scoutDef)
        {
            const string path = StageAssetPath;
            var def = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);

            // ⚠️ 이미 있으면 <b>손대지 않는다.</b>
            // 여기 적힌 숫자는 "처음 만들 때의 시작값"이지 정답이 아니다. 밸런스는 SO 에서 튜닝하며(G-21),
            // 씬을 다시 만들 때마다 덮어쓰면 그 튜닝이 조용히 날아간다.
            // 2026-08-29 실제로 당했다: 씬 재구축 한 번에 G-21 속도 조정이 전부 원복됐다.
            if (def != null) return def;

            def = ScriptableObject.CreateInstance<StageDefinition>();
            AssetDatabase.CreateAsset(def, path);

            var so = new SerializedObject(def);
            so.FindProperty("_escorteeSpeed").floatValue = 1.2f;
            so.FindProperty("_escorteeMaxHealth").floatValue = 100f;
            so.FindProperty("_motherSpeed").floatValue = 0.8f;
            so.FindProperty("_motherSpawnDelay").floatValue = 5f;
            // 스폰 리듬 (G-19) — 묶음 3마리 + 휴지. 평균 밀도는 옛 3초 등간격(0.333마리/초)과 같게 잡는다:
            // 사이클 = (3-1)×0.4 + 8.2 = 9.0초에 3마리 → 0.333마리/초.
            so.FindProperty("_spawnVolleyCount").intValue = 3;
            so.FindProperty("_spawnVolleySpacing").floatValue = 0.4f;
            so.FindProperty("_spawnRestSeconds").floatValue = 8.2f;

            // 버스트 (G-20) — 보호대상이 N06·N10 을 통과하면 모체가 멈추고 생산에 몰빵한다.
            var triggers = so.FindProperty("_burstTriggerNodeIds");
            triggers.arraySize = 2;
            triggers.GetArrayElementAtIndex(0).stringValue = "N06";
            triggers.GetArrayElementAtIndex(1).stringValue = "N10";
            so.FindProperty("_burstDuration").floatValue = 10f;
            so.FindProperty("_burstVolleyCount").intValue = 4;
            so.FindProperty("_burstRestSeconds").floatValue = 2.5f;
            so.FindProperty("_burstRecoverySpeedMultiplier").floatValue = 1.6f;
            so.FindProperty("_burstRecoverySeconds").floatValue = 4f;
            // 스폰 테이블 (G-17) — 워커 70 / 스카우트 30 (G-18).
            var table = so.FindProperty("_spawnTable");
            table.arraySize = 2;
            var walker = table.GetArrayElementAtIndex(0);
            walker.FindPropertyRelative("_enemy").objectReferenceValue = walkerDef;
            walker.FindPropertyRelative("_weight").intValue = 70;
            var scout = table.GetArrayElementAtIndex(1);
            scout.FindPropertyRelative("_enemy").objectReferenceValue = scoutDef;
            scout.FindPropertyRelative("_weight").intValue = 30;
            so.FindProperty("_motherFollowsPath").boolValue = true;
            so.FindProperty("_startingResource").intValue = 150;
            so.FindProperty("_resourcePerSecond").floatValue = 8f;
            so.FindProperty("_shortcutCost").intValue = 120;
            so.FindProperty("_alliesCanDieWhileMarching").boolValue = false;
            so.FindProperty("_enemiesTargetAllies").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }
    }
}
