using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PMF.EditorTools.Authoring;

namespace PMF.Tests
{
    /// <summary>
    /// 익스포터 출력과 씨앗 파일의 바이트 동일 검증.
    /// 설계 정본: 에디터 레포 docs/SDD-05-저작파이프라인.md
    /// </summary>
    public class ToonExporterTests
    {
        [Test]
        public void ExporterOutput_MatchesSeedFile()
        {
            // 익스포터 실행
            ToonExporter.Export();

            // 출력 파일 읽기
            string outputPath = "Assets/_Project/Data/Stages/Stage_Greybox.toon";
            Assert.That(File.Exists(outputPath), Is.True, "익스포터가 파일을 만들지 않았습니다");

            string exported = File.ReadAllText(outputPath);

            // 에디터 레포의 씨앗 파일 읽기
            // 경로: 에디터 레포가 게임 프로젝트와 같은 부모 아래 있을 때
            string editorRepoPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..",
                "Prowl's Moving Factory Editor", "docs", "examples", "Stage_Greybox.toon"));

            if (!File.Exists(editorRepoPath))
            {
                Debug.LogWarning($"[ToonExporterTests] 씨앗 파일 없음: {editorRepoPath}. 스킵.");
                Assert.Ignore("씨앗 파일을 찾을 수 없습니다");
                return;
            }

            string seed = File.ReadAllText(editorRepoPath);
            Assert.That(exported, Is.EqualTo(seed), "익스포터 출력과 씨앗 파일이 일치하지 않습니다");

            // 바이트 수 로그
            Debug.Log($"[ToonExporterTests] 익스포터 출력 {exported.Length} 바이트 == 씨앗 {seed.Length} 바이트");
        }
    }
}