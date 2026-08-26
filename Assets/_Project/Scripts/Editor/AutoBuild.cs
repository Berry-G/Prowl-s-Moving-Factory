using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PMF.EditorTools
{
    /// <summary>
    /// 도메인 리로드 후 1회 자동 구축.
    /// 마커 파일(Logs/pmf-greybox-build.flag)이 없을 때만 씬을 만든다 — 사용자의 수동 편집을 덮지 않는다.
    /// 씬 구축 후 EditMode 테스트를 실행해 결과를 로그+파일로 남긴다.
    /// 재구축이 필요하면 메뉴 PMF/Delete Build Marker 실행 후 아무 스크립트나 저장(리컴파일).
    /// </summary>
    [InitializeOnLoad]
    internal static class AutoBuild
    {
        private const string MarkerRelative = "Logs/pmf-greybox-build.flag";
        private const string TestResultRelative = "Logs/pmf-editmode-results.txt";

        static AutoBuild()
        {
            EditorApplication.delayCall += RunOnce;
        }

        private static void RunOnce()
        {
            try
            {
                if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string marker = Path.Combine(projectRoot, MarkerRelative);

                if (!File.Exists(marker))
                {
                    SceneBootstrap.CreateGreyboxScene();
                    Directory.CreateDirectory(Path.GetDirectoryName(marker));
                    File.WriteAllText(marker, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    Debug.Log("[AutoBuild] 그레이박스 씬 자동 구축 완료 (마커 기록됨)");
                }

                RunEditModeTests();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutoBuild] 실패: {e}");
            }
        }

        private static void RunEditModeTests()
        {
            try
            {
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                var filter = new Filter { testMode = TestMode.EditMode };
                api.RegisterCallbacks(new TestResultLogger());
                api.Execute(new ExecutionSettings(filter));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AutoBuild] 테스트 실행 실패: {e.Message}");
            }
        }

        private sealed class TestResultLogger : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"EditMode 테스트 결과 — Pass={result.PassCount} Fail={result.FailCount} Skip={result.SkipCount}");
                sb.AppendLine($"실행 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                CollectFailures(result, sb, 0);

                Debug.Log($"[PMF.Tests] {sb.ToString().TrimEnd()}");
                try
                {
                    string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    File.WriteAllText(Path.Combine(projectRoot, TestResultRelative), sb.ToString());
                }
                catch { /* 파일 기록 실패는 무시 */ }
            }

            private static void CollectFailures(ITestResultAdaptor result, StringBuilder sb, int depth)
            {
                if (result.HasChildren)
                {
                    foreach (var child in result.Children)
                        CollectFailures(child, sb, depth + 1);
                    return;
                }

                if (!result.HasChildren && (result.FailCount > 0 ||
                        result.ResultState != "Passed" && result.ResultState != "Skipped"))
                    sb.AppendLine($"FAIL: {result.FullName} — {result.Message}");
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite) return;
                if (result.FailCount > 0)
                    Debug.LogError($"[PMF.Tests] FAIL {result.FullName}: {result.Message}");
            }
        }
    }
}
