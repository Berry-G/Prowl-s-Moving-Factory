/**
 * 목적: AuthoringImporter 의 회귀 테스트. 30개 SO 필드가 DTO 와 일치, _resourcePerSecond 는 안 바뀜, 검증 실패 문서는 에셋 0건.
 * 왜 이 구조인가: TempDir 에서만 돌고 TearDown 이 TempDir 을 지워 git status 를 깨끗하게 유지.
 *   enemy 참조(파일 이름→에셋 guid)가 제일 깨지기 쉬운 지점이므로 SpawnEntry.Enemy 를 반드시 확인.
 * 바꾸면 안 되는 것: _resourcePerSecond 불변식 검증을 약하게 만드는 것. Data/Stages/ 실제 에셋을 건드리는 것.
 * 근거: SDD-05 §11 [D-05-11], SDD-02 §4 [D-02-05]
 */
using System.IO; using System.Linq; using System.Text.RegularExpressions;
using NUnit.Framework; using UnityEditor; using UnityEngine; using UnityEngine.TestTools;
using PMF.Data; using PMF.EditorTools.Authoring;
namespace PMF.Tests {
    public class ImporterTests {
        const string TempDir = "Assets/_Project/Scripts/Tests/Authoring/Temp";
        const string SeedPath = "Assets/_Project/Data/Stages/Stage_Greybox.toon";
        const string FxDir = "Assets/_Project/Scripts/Tests/Authoring/Fixtures";
        [SetUp] public void EnsureTemp() {
            if (!AssetDatabase.IsValidFolder(TempDir)) {
                var parent = Path.GetDirectoryName(TempDir).Replace("\\","/");
                var name = Path.GetFileName(TempDir);
                AssetDatabase.CreateFolder(parent, name);
            }
            AssetDatabase.Refresh();
        }
        [TearDown] public void CleanTemp() {
            if (AssetDatabase.IsValidFolder(TempDir))
                AssetDatabase.DeleteAsset(TempDir);
            AssetDatabase.Refresh();
        }
[Test]
        public void SeedImport_All30FieldsMatch() {
            var r = AuthoringImporter.Import(SeedPath, TempDir + "/SeedTest");
            Assert.That(r.Ok, Is.True, "seed: " + r.Message);
            AssetDatabase.Refresh();
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(TempDir + "/SeedTest/Stage_Greybox.asset");
            var map = AssetDatabase.LoadAssetAtPath<MapDefinition>(TempDir + "/SeedTest/Stage_Greybox.map.asset");
            var pdef = AssetDatabase.LoadAssetAtPath<PathDefinition>(TempDir + "/SeedTest/Stage_Greybox.path.asset");
            Assert.That(stage, Is.Not.Null); Assert.That(map, Is.Not.Null); Assert.That(pdef, Is.Not.Null);
            Assert.That(map.Width, Is.EqualTo(32)); Assert.That(map.Height, Is.EqualTo(18));
            Assert.That(map.Origin.x, Is.EqualTo(-16f).Within(0.001f)); Assert.That(map.Origin.y, Is.EqualTo(-9f).Within(0.001f));
            Assert.That(pdef.Nodes.Length, Is.EqualTo(16)); Assert.That(pdef.Edges.Length, Is.EqualTo(18));
            Assert.That(stage.EscorteeSpeed, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(stage.EscorteeMaxHealth, Is.EqualTo(100f).Within(0.001f));
            Assert.That(stage.MotherSpeed, Is.EqualTo(0.28f).Within(0.001f));
            Assert.That(stage.MotherSpawnDelay, Is.EqualTo(5f).Within(0.001f));
            Assert.That(stage.MotherFollowsPath, Is.True);
            Assert.That(stage.SpawnVolleyCount, Is.EqualTo(4));
            Assert.That(stage.SpawnVolleySpacing, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(stage.SpawnRestSeconds, Is.EqualTo(8.2f).Within(0.001f));
            Assert.That(stage.SpawnTelegraphSeconds, Is.EqualTo(0.6f).Within(0.001f));
            var table = stage.SpawnTable;
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table[0].Weight, Is.EqualTo(70)); Assert.That(table[0].Enemy, Is.Not.Null);
            Assert.That(table[0].Enemy.DisplayName, Is.EqualTo("워커"));
            Assert.That(table[1].Weight, Is.EqualTo(30)); Assert.That(table[1].Enemy, Is.Not.Null);
            Assert.That(table[1].Enemy.DisplayName, Is.EqualTo("스카우트"));
            var so = new SerializedObject(stage);
            var ac = so.FindProperty("_enemyHealthByProgress").animationCurveValue;
            Assert.That(ac.length, Is.EqualTo(2));
            Assert.That(ac.keys[0].time, Is.EqualTo(0f).Within(0.001f)); Assert.That(ac.keys[0].value, Is.EqualTo(1f).Within(0.001f));
            Assert.That(ac.keys[1].time, Is.EqualTo(1f).Within(0.001f)); Assert.That(ac.keys[1].value, Is.EqualTo(1f).Within(0.001f));
            Assert.That(AnimationUtility.GetKeyLeftTangentMode(ac,0), Is.EqualTo(AnimationUtility.TangentMode.Linear));
            Assert.That(AnimationUtility.GetKeyRightTangentMode(ac,0), Is.EqualTo(AnimationUtility.TangentMode.Linear));
            Assert.That(AnimationUtility.GetKeyLeftTangentMode(ac,1), Is.EqualTo(AnimationUtility.TangentMode.Linear));
            Assert.That(AnimationUtility.GetKeyRightTangentMode(ac,1), Is.EqualTo(AnimationUtility.TangentMode.Linear));
            Assert.That(stage.BurstDuration, Is.EqualTo(10f).Within(0.001f));
            Assert.That(stage.BurstVolleyCount, Is.EqualTo(6));
            Assert.That(stage.BurstRestSeconds, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(stage.BurstRecoverySpeedMultiplier, Is.EqualTo(1.6f).Within(0.001f));
            Assert.That(stage.BurstRecoverySeconds, Is.EqualTo(4f).Within(0.001f));
            Assert.That(stage.BurstTriggerNodeIds.Count, Is.EqualTo(2));
            Assert.That(stage.BurstTriggerNodeIds[0], Is.EqualTo("N06"));
            Assert.That(stage.BurstTriggerNodeIds[1], Is.EqualTo("N10"));
            Assert.That(stage.StartingResource, Is.EqualTo(150)); Assert.That(stage.ShortcutCost, Is.EqualTo(120));
            var diffs = stage.Difficulties; Assert.That(diffs.Count, Is.EqualTo(3));
            Assert.That(diffs[0].DisplayName, Is.EqualTo("쉬움")); Assert.That(diffs[0].KillReward, Is.EqualTo(4.5f).Within(0.001f)); Assert.That(diffs[0].ResourcePerSecond, Is.EqualTo(3f).Within(0.001f));
            Assert.That(diffs[1].DisplayName, Is.EqualTo("보통")); Assert.That(diffs[1].KillReward, Is.EqualTo(4.5f).Within(0.001f)); Assert.That(diffs[1].ResourcePerSecond, Is.EqualTo(0f).Within(0.001f));
            Assert.That(diffs[2].DisplayName, Is.EqualTo("어려움")); Assert.That(diffs[2].KillReward, Is.EqualTo(3f).Within(0.001f)); Assert.That(diffs[2].ResourcePerSecond, Is.EqualTo(0f).Within(0.001f));
            Assert.That(stage.UiSlowMotionScale, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(stage.ShotLineSeconds, Is.EqualTo(0.07f).Within(0.001f));
            Assert.That(stage.MagicMissileSpeed, Is.EqualTo(8f).Within(0.001f));
            Assert.That(stage.HitFlashSeconds, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(stage.DebrisCount, Is.EqualTo(5)); Assert.That(stage.DebrisSeconds, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(stage.HealthBarHideWhenFull, Is.True);
            Assert.That(stage.MasterVolume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(stage.AlliesCanDieWhileMarching, Is.False);
            Assert.That(stage.EnemiesTargetAllies, Is.False);
        }
[Test]
        public void ResourcePerSecond_InvariantAfterReimport() {
            var r1 = AuthoringImporter.Import(SeedPath, TempDir + "/RpsTest");
            Assert.That(r1.Ok, Is.True, "first: " + r1.Message);
            AssetDatabase.Refresh();
            var stagePath = TempDir + "/RpsTest/Stage_Greybox.asset";
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(stagePath);
            var so = new SerializedObject(stage);
            so.FindProperty("_resourcePerSecond").floatValue = 99f;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var r2 = AuthoringImporter.Import(SeedPath, TempDir + "/RpsTest");
            Assert.That(r2.Ok, Is.True, "reimport: " + r2.Message);
            AssetDatabase.Refresh();
            var so2 = new SerializedObject(AssetDatabase.LoadAssetAtPath<StageDefinition>(stagePath));
            Assert.That(so2.FindProperty("_resourcePerSecond").floatValue, Is.EqualTo(99f).Within(0.001f), "_resourcePerSecond 가 임포터에 의해 덮어쓰여졌다");
        }
        [Test]
        public void MultipleInvalidFixtures_NoAssetsCreated([Values("V-M03","V-B01","V-P02","V-S02")] string fid) {
            LogAssert.Expect(LogType.Error, new Regex(fid));
            var beforeCount = AssetDatabase.FindAssets("t:StageDefinition t:MapDefinition t:PathDefinition", new[]{TempDir}).Length;
            var r = AuthoringImporter.Import(FxDir + "/" + fid + ".toon", TempDir + "/" + fid);
            Assert.That(r.Ok, Is.False, fid + " should fail validation");
            var afterCount = AssetDatabase.FindAssets("t:StageDefinition t:MapDefinition t:PathDefinition", new[]{TempDir}).Length;
            Assert.That(afterCount, Is.EqualTo(beforeCount), fid + ": 검증 실패 후 에셋 생성됨");
        }
    }
} 
