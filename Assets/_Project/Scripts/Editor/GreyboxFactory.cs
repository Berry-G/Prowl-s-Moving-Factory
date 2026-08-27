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
            public GameObject EnemyPrefab;
            public GameObject AllyPrefab;
            public GameObject VillagePrefab;
            public StageDefinition Stage;
            public EnemyDefinition EnemyDef;
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

            result.EnemyDef = CreateEnemyDefinition();
            result.UnitDef = CreateUnitDefinition();
            result.Stage = CreateStageDefinition(result.EnemyDef);

            result.EscorteePrefab = CreateEscorteePrefab();
            result.MotherPrefab = CreateMotherPrefab();
            result.EnemyPrefab = CreateEnemyPrefab(result);
            result.AllyPrefab = CreateAllyPrefab(result);
            result.VillagePrefab = CreateVillagePrefab(result.UnitDef);

            WireDefinitionPrefabs(result.EnemyDef, result.EnemyPrefab,
                                  result.UnitDef, result.AllyPrefab);

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

        private static GameObject CreateEnemyPrefab(Result r)
        {
            var go = new GameObject("Enemy_Basic");
            AddSprite(go, r.Circle, new Color(0.9f, 0.15f, 0.15f), "Actors", 0.4f);
            go.AddComponent<Health>();
            go.AddComponent<Attacker>();
            go.AddComponent<Enemy>();
            return SaveAsPrefab(go, $"{PrefabRoot}/Enemy_Basic.prefab");
        }

        private static GameObject CreateAllyPrefab(Result r)
        {
            var go = new GameObject("Ally_Basic");
            // 행군 중 연한 파랑 → 배치 후 진한 파랑은 런타임(AllyUnit)이 바꾼다.
            AddSprite(go, r.Square, new Color(0.55f, 0.75f, 1f), "Actors", 0.5f);

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
            return SaveAsPrefab(go, $"{PrefabRoot}/Ally_Basic.prefab");
        }

        private static GameObject CreateVillagePrefab(UnitDefinition unitDef)
        {
            var go = new GameObject("Village");
            AddSprite(go, GreyboxSprites.GetOrCreateSquare(), new Color(0.2f, 0.75f, 0.3f), "Deploy", 1.4f);
            var village = go.AddComponent<Village>();

            var so = new SerializedObject(village);
            var array = so.FindProperty("_hireableUnits");
            array.arraySize = 1;
            array.GetArrayElementAtIndex(0).objectReferenceValue = unitDef;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SaveAsPrefab(go, $"{PrefabRoot}/Village.prefab");
        }

        private static EnemyDefinition CreateEnemyDefinition()
        {
            string path = $"{DataRoot}/Enemies/Enemy_Basic.asset";
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def != null) return def;

            def = ScriptableObject.CreateInstance<EnemyDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("_moveSpeed").floatValue = 2.0f;
            so.FindProperty("_maxHealth").floatValue = 20f;
            so.FindProperty("_attackRange").floatValue = 1.0f;
            so.FindProperty("_attackDamage").floatValue = 5f;
            so.FindProperty("_attackInterval").floatValue = 1.0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static UnitDefinition CreateUnitDefinition()
        {
            string path = $"{DataRoot}/Units/Ally_Basic.asset";
            var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
            if (def != null) return def;

            def = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(def);
            so.FindProperty("_displayName").stringValue = "Ally_Basic";
            so.FindProperty("_moveSpeed").floatValue = 2.5f;
            so.FindProperty("_maxHealth").floatValue = 50f;
            so.FindProperty("_attackRange").floatValue = 3.5f;
            so.FindProperty("_attackDamage").floatValue = 10f;
            so.FindProperty("_attackInterval").floatValue = 0.8f;
            so.FindProperty("_hireCost").intValue = 50;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static StageDefinition CreateStageDefinition(EnemyDefinition enemyDef)
        {
            const string path = StageAssetPath;
            var def = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<StageDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            var so = new SerializedObject(def);
            so.FindProperty("_escorteeSpeed").floatValue = 1.2f;
            so.FindProperty("_escorteeMaxHealth").floatValue = 100f;
            so.FindProperty("_motherSpeed").floatValue = 0.8f;
            so.FindProperty("_motherSpawnDelay").floatValue = 5f;
            so.FindProperty("_motherSpawnInterval").floatValue = 3f;
            so.FindProperty("_motherSpawnEnemy").objectReferenceValue = enemyDef;
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
