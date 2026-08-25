using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PMF.EditorTools
{
    /// <summary>
    /// P-02 부트스트랩: Stage_Greybox 씬을 생성하고 빌드 씬 목록 0번에 등록한다.
    /// 배치 실행: Unity.exe -batchmode -quit -executeMethod PMF.EditorTools.SceneBootstrap.CreateGreyboxScene
    /// 에디터 메뉴로도 실행 가능하다 (PMF/Create Greybox Scene).
    /// </summary>
    public static class SceneBootstrap
    {
        private const string ScenePath = "Assets/_Project/Scenes/Stage_Greybox.unity";

        [MenuItem("PMF/Create Greybox Scene")]
        public static void CreateGreyboxScene()
        {
            // 프로젝트 기본 동작 모드가 2D(m_DefaultBehaviorMode: 1)이므로 새 씬은 2D로 만들어진다.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 그레이박스 카메라는 직교 투영. 위치는 32x18 맵 중앙 근처(P-03에서 확정).
            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.orthographic = true;
                camera.transform.position = new Vector3(16f, 9f, -10f);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Build Profiles 의 씬 목록(인덱스 0)에 등록한다.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log($"[SceneBootstrap] {ScenePath} 생성 및 빌드 씬 목록 0번 등록 완료");
        }
    }
}
