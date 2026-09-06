/**
 * 목적: .toon 파일 → ScriptableObject(StageDefinition + MapDefinition + PathDefinition) 임포트.
 * 왜 이 구조인가: 검증 통과 전에 AssetDatabase 를 한 번도 안 만지는 것이 핵심 설계.
 *   그래야 "전부 성공하거나 전부 취소" 가 보장된다. 쓰기 단계 예외는 버그이므로 git 복구 메시지를 남긴다.
 * 바꾸면 안 되는 것: 검증 실패 시 에셋을 건드리는 것. _resourcePerSecond 는 임포터가 건드리지 않는다 (난이도 표 폴백).
 * 근거: SDD-05 §6 [D-05-06], SDD-02 §4 [D-02-05]
 */
using System; using System.Collections.Generic; using System.IO; using System.Linq;
using UnityEditor; using UnityEngine; using PMF.Data; using PMF.Grid;
namespace PMF.EditorTools.Authoring {
    public struct ImportResult { public bool Ok; public string Message; public List<Issue> Issues; }
    public static class AuthoringImporter {
        const string Dir = "Assets/_Project/Data/Stages";
        static readonly Dictionary<string,Difficulty> S2D = new(){{"Easy",Difficulty.Easy},{"Normal",Difficulty.Normal},{"Hard",Difficulty.Hard}};
        public static ImportResult Import(string toonPath, string outputBasePath = null) {
            var issues = new List<Issue>();
            string text;
            try { text = File.ReadAllText(toonPath); } catch (Exception ex) { return Fail("읽기 실패: " + ex.Message, issues); }
            var rr = StageDocumentReader.Read(text);
            if (!rr.Ok) { issues.AddRange(rr.Issues); return Fail("문서 읽기 실패", issues); }
            var d = rr.Doc;
            var enemyNames = new HashSet<string>();
            var enemyDefs = new Dictionary<string, EnemyDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:EnemyDefinition")) {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var n = Path.GetFileNameWithoutExtension(p);
                enemyNames.Add(n);
                var ed = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(p);
                if (ed != null) enemyDefs[n] = ed;
            }
            issues.AddRange(AuthoringValidator.Validate(d, enemyNames));
            var errs = issues.Where(i => i.Severity == "error").ToList();
            if (errs.Count > 0) {
                foreach (var e in errs) Debug.LogError("[Importer] " + e.Id + " " + e.Path + " - " + e.Message);
                return Fail(errs.Count + " problems. Import aborted.", issues);
            }
            try {
                string bp = (outputBasePath ?? Dir) + "/" + d.Name;
                var mapDef = LoadOrCreate<MapDefinition>(bp + ".map.asset");
                var cells = new byte[d.Map.Width * d.Map.Height];
                for (int i = 0; i < cells.Length; i++) cells[i] = (byte)d.Map.Cells[i];
                mapDef.Set(d.Map.Width, d.Map.Height, new Vector2(d.Map.OriginX, d.Map.OriginY), cells);
                EditorUtility.SetDirty(mapDef);
                var pathDef = LoadOrCreate<PathDefinition>(bp + ".path.asset");
                var nds = new PathDefinition.NodeDef[d.Path.Nodes.Length];
                for (int i = 0; i < nds.Length; i++) nds[i] = new PathDefinition.NodeDef { Id = d.Path.Nodes[i].Id, Role = d.Path.Nodes[i].Role, X = d.Path.Nodes[i].X, Y = d.Path.Nodes[i].Y };
                var eds = new PathDefinition.EdgeDef[d.Path.Edges.Length];
                for (int i = 0; i < eds.Length; i++) eds[i] = new PathDefinition.EdgeDef { From = d.Path.Edges[i].From, To = d.Path.Edges[i].To, Allowed = d.Path.Edges[i].Allowed, Bidirectional = d.Path.Edges[i].Bidirectional, Shortcut = d.Path.Edges[i].Shortcut };
                pathDef.Set(nds, eds);
                EditorUtility.SetDirty(pathDef);
                var stageDef = LoadOrCreate<StageDefinition>(bp + ".asset");
                var so = new SerializedObject(stageDef);
                so.FindProperty("_escorteeSpeed").floatValue = d.Escortee.Speed;
                so.FindProperty("_escorteeMaxHealth").floatValue = d.Escortee.MaxHealth;
                so.FindProperty("_motherSpeed").floatValue = d.Mother.Speed;
                so.FindProperty("_motherSpawnDelay").floatValue = d.Mother.SpawnDelay;
                so.FindProperty("_motherFollowsPath").boolValue = d.Mother.FollowsPath;
                so.FindProperty("_spawnVolleyCount").intValue = d.Spawn.VolleyCount;
                so.FindProperty("_spawnVolleySpacing").floatValue = d.Spawn.VolleySpacing;
                so.FindProperty("_spawnRestSeconds").floatValue = d.Spawn.RestSeconds;
                so.FindProperty("_spawnTelegraphSeconds").floatValue = d.Spawn.TelegraphSeconds;
var tp = so.FindProperty("_spawnTable"); tp.ClearArray(); tp.arraySize = d.Spawn.Table.Length;
                for (int i = 0; i < d.Spawn.Table.Length; i++) {
                    var r = tp.GetArrayElementAtIndex(i);
                    r.FindPropertyRelative("_enemy").objectReferenceValue = enemyDefs.GetValueOrDefault(d.Spawn.Table[i].Enemy);
                    r.FindPropertyRelative("_weight").intValue = d.Spawn.Table[i].Weight;
                }
                var cp = so.FindProperty("_enemyHealthByProgress");
                if (d.Spawn.HealthByProgress.Length > 0) {
                    var ks = new Keyframe[d.Spawn.HealthByProgress.Length];
                    for (int i = 0; i < ks.Length; i++) ks[i] = new Keyframe(d.Spawn.HealthByProgress[i].T, d.Spawn.HealthByProgress[i].Mul);
                    var ac = new AnimationCurve(ks);
                    for (int i = 0; i < ac.length; i++) { AnimationUtility.SetKeyLeftTangentMode(ac, i, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(ac, i, AnimationUtility.TangentMode.Linear); }
                    cp.animationCurveValue = ac;
                }
                so.FindProperty("_burstDuration").floatValue = d.Burst.Duration;
                so.FindProperty("_burstVolleyCount").intValue = d.Burst.VolleyCount;
                so.FindProperty("_burstRestSeconds").floatValue = d.Burst.RestSeconds;
                so.FindProperty("_burstRecoverySpeedMultiplier").floatValue = d.Burst.RecoverySpeedMultiplier;
                so.FindProperty("_burstRecoverySeconds").floatValue = d.Burst.RecoverySeconds;
                var trp = so.FindProperty("_burstTriggerNodeIds"); trp.ClearArray(); trp.arraySize = d.Burst.TriggerNodeIds.Length;
                for (int i = 0; i < d.Burst.TriggerNodeIds.Length; i++) trp.GetArrayElementAtIndex(i).stringValue = d.Burst.TriggerNodeIds[i];
                so.FindProperty("_startingResource").intValue = d.Economy.StartingResource;
                so.FindProperty("_shortcutCost").intValue = d.Economy.ShortcutCost;
                var dp = so.FindProperty("_difficulties"); dp.ClearArray(); dp.arraySize = d.Economy.Difficulties.Length;
                for (int i = 0; i < d.Economy.Difficulties.Length; i++) {
                    var el = dp.GetArrayElementAtIndex(i); var drr = d.Economy.Difficulties[i];
                    el.FindPropertyRelative("_difficulty").enumValueIndex = (int)S2D.GetValueOrDefault(drr.Difficulty, Difficulty.Normal);
                    el.FindPropertyRelative("_displayName").stringValue = drr.DisplayName;
                    el.FindPropertyRelative("_killReward").floatValue = drr.KillReward;
                    el.FindPropertyRelative("_resourcePerSecond").floatValue = drr.ResourcePerSecond;
                }
                so.FindProperty("_uiSlowMotionScale").floatValue = d.Presentation.UiSlowMotionScale;
                so.FindProperty("_shotLineSeconds").floatValue = d.Presentation.ShotLineSeconds;
                so.FindProperty("_magicMissileSpeed").floatValue = d.Presentation.MagicMissileSpeed;
                so.FindProperty("_hitFlashSeconds").floatValue = d.Presentation.HitFlashSeconds;
                so.FindProperty("_debrisCount").intValue = d.Presentation.DebrisCount;
                so.FindProperty("_debrisSeconds").floatValue = d.Presentation.DebrisSeconds;
                so.FindProperty("_healthBarHideWhenFull").boolValue = d.Presentation.HealthBarHideWhenFull;
                so.FindProperty("_masterVolume").floatValue = d.Presentation.MasterVolume;
                so.FindProperty("_alliesCanDieWhileMarching").boolValue = d.Toggles.AlliesCanDieWhileMarching;
                so.FindProperty("_enemiesTargetAllies").boolValue = d.Toggles.EnemiesTargetAllies;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(stageDef);
                AssetDatabase.SaveAssets();
            } catch (Exception ex) {
                Debug.LogError("[Importer] Write failed - git checkout Data/Stages: " + ex.Message);
                return Fail("Write failed: " + ex.Message, issues);
            }
            return new ImportResult { Ok = true, Message = "Import completed - " + d.Name + ". Rebuild scene and remeasure income ceiling.", Issues = issues };
        }
        static T LoadOrCreate<T>(string path) where T : ScriptableObject {
            var dir = Path.GetDirectoryName(path); if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var ex = AssetDatabase.LoadAssetAtPath<T>(path); if (ex != null) return ex;
            var so = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(so, path); return so;
        }
        static ImportResult Fail(string msg, List<Issue> issues) => new ImportResult { Ok = false, Message = msg, Issues = issues };
    }
}