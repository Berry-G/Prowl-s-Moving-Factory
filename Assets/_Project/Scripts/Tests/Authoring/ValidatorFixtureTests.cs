/**
 * 목적: 검증 픽스처 28개가 각각 자기 규칙 ID 하나만 내는지, minimal.toon 은 이슈 0인지 검증.
 *   규칙 24개가 실제로 실행되는지 감사.
 * 왜 이 구조인가: TS tests/fixtures.test.ts 의 C# 포팅. DECODE_BLOCKED + COMPANION + 감사.
 * 바꾸면 안 되는 것: DECODE_BLOCKED 4개뿐. COMPANION 은 논리적 필연만. 감사 실패 무시 금지.
 * 근거: SDD-06 §3 [D-06-03], SDD-05 §11 [D-05-11]
 */
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PMF.EditorTools.Authoring;
namespace PMF.Tests
{
    public class ValidatorFixtureTests
    {
        const string Dir = "Assets/_Project/Scripts/Tests/Authoring/Fixtures";
        static string R(string n) => File.ReadAllText(Path.Combine(Dir, n));
        static readonly HashSet<string> Enemies = new() { "Robot_Walker", "Robot_Scout" };
        static readonly HashSet<string> DB = new() { "V-F01","V-F03","V-M01","V-M02" };
        static readonly Dictionary<string,string[]> C = new()
        {
            {"V-M03",new[]{"V-M06"}},{"V-M04",new[]{"V-M07"}},
        };
        static readonly string[] All = {"V-F01","V-F02","V-F03","V-M01","V-M02","V-M03","V-M04","V-M05","V-M06","V-M07","V-M08",
            "V-P01","V-P02","V-P03","V-P04","V-P05","V-P06","V-P07","V-P08","V-P09",
            "V-S01","V-S02","V-S03","V-S04","V-S05","V-B01","V-B02","V-B03"};
        static bool Sig(string s) => s != "info";

        [Test]
        public void Minimal_HasNoErrorWarning()
        {
            var r = StageDocumentReader.Read(R("minimal.toon"));
            Assert.That(r.Ok, Is.True); var ids = AuthoringValidator.Validate(r.Doc, Enemies);
            var sig = ids.Where(i => Sig(i.Severity)).ToList();
            Assert.That(sig, Is.Empty, $"minimal: {string.Join(",",sig.Select(i=>i.Id))}");
            Assert.That(ids.Where(i=>i.Severity=="info").Select(i=>i.Id), Is.EquivalentTo(new[]{"V-M06"}));
        }

        static string[] Fx => new[]{
            "V-B01","V-B02","V-B03","V-F01","V-F02","V-F03",
            "V-M01","V-M02","V-M03","V-M04","V-M05","V-M06","V-M07","V-M08",
            "V-P01","V-P02","V-P03","V-P04","V-P05","V-P06","V-P07","V-P08","V-P09",
            "V-S01","V-S02","V-S03","V-S04","V-S05"};

        [TestCaseSource(nameof(Fx))]
        public void InvalidFixture_EmitsItsOwnId(string fid)
        {
            var rr = StageDocumentReader.Read(R(fid+".toon"));
            if(!rr.Ok)
            {
                Assert.That(DB.Contains(fid), Is.True, $"{fid}: decode 거부. decode 소관 규칙이 아님");
                Assert.That(rr.Issues.Select(i=>i.Id), Does.Contain(fid));
                return;
            }
            Assert.That(DB.Contains(fid), Is.False, $"{fid}: decode 소관인데 통과");
            var ids = new HashSet<string>(AuthoringValidator.Validate(rr.Doc,Enemies).Where(i=>Sig(i.Severity)).Select(i=>i.Id));
            Assert.That(ids, Does.Contain(fid), $"{fid}: 자기 규칙 미발화");
            var allowed = new HashSet<string>{fid};
            if(C.TryGetValue(fid,out var cp)) foreach(var c in cp) allowed.Add(c);
            var extra = ids.Where(id=>!allowed.Contains(id)).ToList();
            Assert.That(extra, Is.Empty, $"{fid}: 추가 {string.Join(",",extra)}");
        }

        [Test]
        public void Audit_AllRulesFire()
        {
            var fired = new HashSet<string>();
            var mr = StageDocumentReader.Read(R("minimal.toon"));
            if(mr.Ok) foreach(var i in AuthoringValidator.Validate(mr.Doc,Enemies)) fired.Add(i.Id);
            foreach(var fid in Fx)
            {
                var rr = StageDocumentReader.Read(R(fid+".toon"));
                if(!rr.Ok) continue; // decode 실패 = 구조 깨짐 = 규칙 실행 불가. 발화로 세지 않는다.
                foreach(var i in AuthoringValidator.Validate(rr.Doc,Enemies)) fired.Add(i.Id);
            }
            var never = All.Where(id=>!fired.Contains(id)).ToList();
            Assert.That(never, Is.EquivalentTo(DB), $"미발화: {string.Join(",",never)}");
        }
    }
}