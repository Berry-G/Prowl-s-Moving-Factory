using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PMF.Grid;
using PMF.Pathing;

namespace PMF.EditorTools
{
    /// <summary>
    /// Stage_Greybox 씬 전체를 코드로 구축한다 (P-02/P-04/P-05 저작 포함).
    /// 배치 실행: Unity.exe -batchmode -quit -executeMethod PMF.EditorTools.SceneBootstrap.CreateGreyboxScene
    /// 에디터 메뉴: PMF/Create Greybox Scene
    /// </summary>
    public static class SceneBootstrap
    {
        internal const string ScenePath = "Assets/_Project/Scenes/Stage_Greybox.unity";

        [MenuItem("PMF/Create Greybox Scene")]
        public static void CreateGreyboxScene()
        {


            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 왜: NewScene이 참조 없는 SO를 해제할 수 있으므로 새 씬을 연 뒤 로드한다.
            var factory = GreyboxFactory.BuildAll();

            // 기본 씬에 들어오는 라이트는 2D 그레이박스에 불필요.
            foreach (var light in Object.FindObjectsByType<Light>())
                Object.DestroyImmediate(light.gameObject);

            SetupCamera();
            SceneParts.BuildMap(factory);
            SceneParts.BuildServices(factory);
            SceneParts.BuildPathNodes(factory);
            SceneParts.BuildActors(factory);
            SceneParts.BuildUI();
            ApplyScriptExecutionOrder();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log($"[SceneBootstrap] {ScenePath} 구축 완료");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("PMF/Delete Build Marker")]
        public static void DeleteBuildMarker()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                                       "Logs", "pmf-greybox-build.flag");
            if (File.Exists(path)) File.Delete(path);
            Debug.Log("[SceneBootstrap] 빌드 마커 삭제 — 다음 도메인 리로드 때 재구축된다");
        }

        private static void SetupCamera()
        {
            var camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null) return;
            camera.orthographic = true;
            if (camera.GetComponent<PMF.Session.StageCameraController>() == null) camera.gameObject.AddComponent<PMF.Session.StageCameraController>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographicSize = 9.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        }

        /// <summary>Script Execution Order 등록 (ARCHITECTURE §4).</summary>
        private static void ApplyScriptExecutionOrder()
        {
            SetOrder<GridSystem>(-200);
            SetOrder<PathGraph>(-190);
            SetOrder<Session.GameClock>(-180);
            SetOrder<Session.GameSession>(-170);
            // 왜: MotherSpawner.Start보다 보호대상의 세션 등록을 먼저 실행한다.
            SetOrder<PMF.Actors.Escortee>(-160);
        }

        private static void SetOrder<T>(int order) where T : MonoBehaviour
        {
            try
            {
                var temp = new GameObject("__temp_order__");
                var component = temp.AddComponent<T>();
                var script = MonoScript.FromMonoBehaviour(component);
                if (script != null)
                    MonoImporter.SetExecutionOrder(script, order);
                Object.DestroyImmediate(temp);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SceneBootstrap] 실행 순서 설정 실패 ({typeof(T).Name}): {e.Message}");
            }
        }
    }
}
