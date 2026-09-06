/** Importer 회귀 테스트. 임시 폴더 → SO 30개 필드 대조. 실패 문서는 에셋 0건. */
using System.IO; using System.Linq; using NUnit.Framework;
using UnityEditor; using UnityEngine; using UnityEngine.TestTools;
using PMF.Data; using PMF.EditorTools.Authoring;
namespace PMF.Tests {
    public class ImporterTests {
        const string TempDir = "Assets/_Project/Scripts/Tests/Authoring/Temp";
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
            var r = AuthoringImporter.Import("Assets/_Project/Data/Stages/Stage_Greybox.toon",
                "Assets/_Project/Scripts/Tests/Authoring/Temp");
            Assert.That(r.Ok, Is.True, "seed import failed: " + (r.Ok ? "" : r.Message));
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/_Project/Scripts/Tests/Authoring/Temp/Stage_Greybox.asset");
            var map = AssetDatabase.LoadAssetAtPath<MapDefinition>("Assets/_Project/Scripts/Tests/Authoring/Temp/Stage_Greybox.map.asset");
            var path = AssetDatabase.LoadAssetAtPath<PathDefinition>("Assets/_Project/Scripts/Tests/Authoring/Temp/Stage_Greybox.path.asset");
            Assert.That(stage, Is.Not.Null); Assert.That(map, Is.Not.Null); Assert.That(path, Is.Not.Null);
            Assert.That(map.Width, Is.EqualTo(32)); Assert.That(map.Height, Is.EqualTo(18));
            Assert.That(map.Origin.x, Is.EqualTo(-16f).Within(0.001f)); Assert.That(map.Origin.y, Is.EqualTo(-9f).Within(0.001f));
            Assert.That(path.Nodes.Length, Is.GreaterThan(0)); Assert.That(path.Edges.Length, Is.GreaterThan(0));
            Assert.That(stage.EscorteeSpeed, Is.EqualTo(0.42f).Within(0.001f));
            Assert.That(stage.EscorteeMaxHealth, Is.EqualTo(100f).Within(0.001f));
            Assert.That(stage.MotherSpeed, Is.EqualTo(0.28f).Within(0.001f));
            Assert.That(stage.MotherSpawnDelay, Is.EqualTo(5f).Within(0.001f));
            Assert.That(stage.SpawnVolleyCount, Is.EqualTo(4));
            Assert.That(stage.SpawnVolleySpacing, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(stage.SpawnRestSeconds, Is.EqualTo(8.2f).Within(0.001f));
var table = stage.SpawnTable;
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table[0].Weight, Is.EqualTo(70)); Assert.That(table[1].Weight, Is.EqualTo(30));
            var so = new SerializedObject(stage);
            var ac = so.FindProperty("_enemyHealthByProgress").animationCurveValue;
            Assert.That(ac.length, Is.EqualTo(2));
            Assert.That(ac.keys[0].value, Is.EqualTo(1f).Within(0.001f));
            Assert.That(stage.BurstDuration, Is.EqualTo(10f).Within(0.001f));
            Assert.That(stage.BurstVolleyCount, Is.EqualTo(6));
            Assert.That(stage.BurstRestSeconds, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(stage.StartingResource, Is.EqualTo(150));
            Assert.That(stage.ShortcutCost, Is.EqualTo(120));
            var diffs = stage.Difficulties;
            Assert.That(diffs.Count, Is.EqualTo(3));
            Assert.That(diffs[0].DisplayName, Is.EqualTo("쉬움"));
            Assert.That(stage.UiSlowMotionScale, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(stage.ShotLineSeconds, Is.EqualTo(0.07f).Within(0.001f));
            Assert.That(stage.MagicMissileSpeed, Is.EqualTo(8f).Within(0.001f));
            Assert.That(stage.HitFlashSeconds, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(stage.DebrisCount, Is.EqualTo(5));
            Assert.That(stage.DebrisSeconds, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(stage.MasterVolume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(so.FindProperty("_resourcePerSecond").floatValue, Is.EqualTo(8f).Within(0.001f));
        }
        [Test]
        public void InvalidFixture_NoAssetsCreated() {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("V-M03"));
            var before = AssetDatabase.FindAssets("t:StageDefinition", new[] {"Assets/_Project/Data/Stages"});
            var r = AuthoringImporter.Import(Path.Combine("Assets/_Project/Scripts/Tests/Authoring/Fixtures", "V-M03.toon"), TempDir);
            Assert.That(r.Ok, Is.False, "V-M03 should fail validation");
            Assert.That(AssetDatabase.FindAssets("t:StageDefinition", new[]{"Assets/_Project/Data/Stages"}).Length, Is.EqualTo(before.Length));
        }
    }
}