/**
 * 목적: StageDocumentReader 의 decode 소관 4규칙(V-F01/V-F03/V-M01/V-M02) 검증.
 * 왜 이 구조인가: TS decode.ts 와 같은 규칙이 C# 쪽에서도 같은 메시지를 내는지 확인한다.
 * 바꾸면 안 되는 것: 픽스처 파일을 고치지 마라 — 원본을 고쳐 sync 해야 한다.
 * 근거: SDD-05 §11 [D-05-11], SDD-02 §5 [D-02-06]
 */
using System.IO;
using NUnit.Framework;
using PMF.EditorTools.Authoring;

namespace PMF.Tests
{
    public class StageDocumentReaderTests
    {
        const string FDir = "Assets/_Project/Scripts/Tests/Authoring/Fixtures";
        const string SeedPath = "Assets/_Project/Data/Stages/Stage_Greybox.toon";
        static string R(string n) => File.ReadAllText(Path.Combine(FDir, n));

        [Test]
        public void MinimalToon_ReadSuccess_NoIssues()
        {
            var r = StageDocumentReader.Read(R("minimal.toon"));
            Assert.That(r.Ok, Is.True);
            Assert.That(r.Issues.Count, Is.EqualTo(0));
        }

        [Test]
        public void V_F01_ReturnsSchemaError()
        {
            var r = StageDocumentReader.Read(R("V-F01.toon"));
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Issues.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(r.Issues[0].Id, Is.EqualTo("V-F01"));
        }

        [Test]
        public void V_F03_ReturnsFieldMissingError()
        {
            var r = StageDocumentReader.Read(R("V-F03.toon"));
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Issues[0].Id, Is.EqualTo("V-F03"));
        }

        [Test]
        public void V_M01_ReturnsRowCountError()
        {
            var r = StageDocumentReader.Read(R("V-M01.toon"));
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Issues[0].Id, Is.EqualTo("V-M01"));
        }

        [Test]
        public void V_M02_ReturnsUnknownCharError()
        {
            var r = StageDocumentReader.Read(R("V-M02.toon"));
            Assert.That(r.Ok, Is.False);
            Assert.That(r.Issues[0].Id, Is.EqualTo("V-M02"));
        }

        [Test]
        public void SeedFile_ReadSuccess_NoIssues()
        {
            var text = File.ReadAllText(SeedPath);
            var r = StageDocumentReader.Read(text);
            Assert.That(r.Ok, Is.True);
            Assert.That(r.Issues.Count, Is.EqualTo(0));
            Assert.That(r.Doc.Name, Is.EqualTo("Stage_Greybox"));
            Assert.That(r.Doc.Map.Width, Is.EqualTo(32));
            Assert.That(r.Doc.Map.Height, Is.EqualTo(18));
            Assert.That(r.Doc.Path.Nodes.Length, Is.EqualTo(16));
            Assert.That(r.Doc.Path.Edges.Length, Is.EqualTo(18));
            Assert.That(r.Doc.Spawn.VolleyCount, Is.EqualTo(4));
            Assert.That(r.Doc.Spawn.VolleySpacing, Is.EqualTo(0.4f));
            Assert.That(r.Doc.Spawn.RestSeconds, Is.EqualTo(8.2f));
            Assert.That(r.Doc.Burst.Duration, Is.EqualTo(10f));
            Assert.That(r.Doc.Burst.VolleyCount, Is.EqualTo(6));
            Assert.That(r.Doc.Economy.StartingResource, Is.EqualTo(150));
            Assert.That(r.Doc.Economy.ShortcutCost, Is.EqualTo(120));
            Assert.That(r.Doc.Escortee.Speed, Is.EqualTo(0.42f));
            Assert.That(r.Doc.Escortee.MaxHealth, Is.EqualTo(100f));
            Assert.That(r.Doc.Mother.Speed, Is.EqualTo(0.28f));
            Assert.That(r.Doc.Mother.SpawnDelay, Is.EqualTo(5f));
            Assert.That(r.Doc.Mother.FollowsPath, Is.True);
            Assert.That(r.Doc.Economy.Difficulties.Length, Is.EqualTo(3));
            Assert.That(r.Doc.Presentation.DebrisCount, Is.EqualTo(5));
            Assert.That(r.Doc.Presentation.MasterVolume, Is.EqualTo(1f));
            Assert.That(r.Doc.Toggles.AlliesCanDieWhileMarching, Is.False);
        }
    }
}