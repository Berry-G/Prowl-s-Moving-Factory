using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using PMF.Data;
using PMF.Grid;
using PMF.Pathing;
using PMF.Session;

namespace PMF.EditorTools
{
    /// <summary>씬 파트별 조립 (맵/서비스/경로노드/액터/UI). SceneBootstrap 에서 호출.</summary>
    internal static class SceneParts
    {
        private static Transform _servicesRoot;
        private static Transform _actorsRoot;

        // ---------- 맵 ----------

        internal static void BuildMap(GreyboxFactory.Result factory)
        {
            var mapRoot = new GameObject("--- Map ---");

            // GridSystem._origin 과 Grid.transform.position 은 반드시 일치.
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(mapRoot.transform);
            gridGo.transform.position = GreyboxMapData.Origin;

            var grid = gridGo.AddComponent<UnityEngine.Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var ground = MakeTilemap(gridGo.transform, "Tilemap_Ground", "Ground", 0);
            var road = MakeTilemap(gridGo.transform, "Tilemap_Road", "Path", 0);
            var buildable = MakeTilemap(gridGo.transform, "Tilemap_Buildable", "Deploy", 0);
            var villageSlot = MakeTilemap(gridGo.transform, "Tilemap_VillageSlot", "Deploy", 1);
            var blocked = MakeTilemap(gridGo.transform, "Tilemap_Blocked", "Ground", -1);

            var tileGround = GreyboxSprites.GetOrCreateTile("Tile_Ground",
                new Color(0.24f, 0.24f, 0.27f), factory.Square);
            // 채도 없는 회색은 Ground 와 명도만 다르고 색상은 같아서 눈에 잘 안 띈다 — 색조를 확실히 다르게.
            var tileRoad = GreyboxSprites.GetOrCreateTile("Tile_Road",
                new Color(0.74f, 0.6f, 0.4f), factory.Square);
            var tileBuildable = GreyboxSprites.GetOrCreateTile("Tile_Buildable",
                new Color(0.35f, 0.55f, 0.65f), factory.Square);
            var tileVillage = GreyboxSprites.GetOrCreateTile("Tile_VillageSlot",
                new Color(0.25f, 0.6f, 0.3f), factory.Square);
            var tileBlocked = GreyboxSprites.GetOrCreateTile("Tile_Blocked",
                new Color(0.1f, 0.1f, 0.12f), factory.Square);

            for (int y = 0; y < GreyboxMapData.Height; y++)
            {
                for (int x = 0; x < GreyboxMapData.Width; x++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    switch (GreyboxMapData.GetCategory(x, y))
                    {
                        case GreyboxMapData.Category.Ground:    ground.SetTile(pos, tileGround); break;
                        case GreyboxMapData.Category.Road:      road.SetTile(pos, tileRoad); break;
                        case GreyboxMapData.Category.Buildable: buildable.SetTile(pos, tileBuildable); break;
                        case GreyboxMapData.Category.Village:   villageSlot.SetTile(pos, tileVillage); break;
                        case GreyboxMapData.Category.Blocked:   blocked.SetTile(pos, tileBlocked); break;
                    }
                }
            }

            EnsureServicesRoot();
            CreateGridSystem(ground.GetComponent<Tilemap>(),
                             road.GetComponent<Tilemap>(),
                             buildable.GetComponent<Tilemap>(),
                             villageSlot.GetComponent<Tilemap>(),
                             blocked.GetComponent<Tilemap>());

            DrawExitMarker(mapRoot.transform);
        }

        private static Tilemap MakeTilemap(Transform parent, string name,
                                           string sortingLayer, int order)
        {
            var go = new GameObject(name);
            // worldPositionStays 기본값(true) 이면 Grid 의 오프셋(_origin)만큼 반대로 밀려서
            // 로컬 위치가 어긋난다 — 타일맵은 항상 Grid 기준 로컬 원점(0,0,0) 이어야 한다.
            go.transform.SetParent(parent, false);
            go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        // ---------- 서비스 ----------

        private static void EnsureServicesRoot()
        {
            if (_servicesRoot == null)
                _servicesRoot = new GameObject("--- Services ---").transform;
        }

        internal static void BuildServices(GreyboxFactory.Result factory)
        {
            EnsureServicesRoot();

            // GameClock
            var clockGo = new GameObject("GameClock");
            clockGo.transform.SetParent(_servicesRoot);
            clockGo.AddComponent<GameClock>();

            // GameSession (+ Wallet 자식)
            var sessionGo = new GameObject("GameSession");
            sessionGo.transform.SetParent(_servicesRoot);
            var walletGo = new GameObject("Wallet");
            walletGo.transform.SetParent(sessionGo.transform);
            var session = sessionGo.AddComponent<GameSession>();

            // factory.Stage 가 에셋 임포트 타이밍에 따라 null 이 되는 경우가 있었다 (씬에 미배선으로 저장됨).
            // 경로에서 다시 읽어 보강하고, 그래도 없으면 조용히 넘어가지 말고 에러로 드러낸다.
            var stage = factory.Stage != null
                ? factory.Stage
                : AssetDatabase.LoadAssetAtPath<StageDefinition>(GreyboxFactory.StageAssetPath);
            if (stage == null)
                Debug.LogError($"[SceneParts] StageDefinition 을 찾지 못했다: {GreyboxFactory.StageAssetPath}");

            var so = new SerializedObject(session);
            so.FindProperty("_definition").objectReferenceValue = stage;
            so.FindProperty("_wallet").objectReferenceValue = walletGo.AddComponent<Wallet>();
            so.ApplyModifiedPropertiesWithoutUndo();

            // PathGraph (노드는 BuildPathNodes 에서 자식으로 붙는다)
            var graphGo = new GameObject("PathGraph");
            graphGo.transform.SetParent(_servicesRoot);
            graphGo.AddComponent<PathGraph>();

            // 조작/치트 컨트롤러들
            AddSimple<DeploymentController>("DeploymentController");
            AddSimple<ShortcutController>("ShortcutController");
            AddSimple<Diagnostics.DebugHotkeys>("DebugHotkeys");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AddSimple<Diagnostics.DebugOverlay>("DebugOverlay");
#endif
        }

        private static void AddSimple<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(_servicesRoot);
            go.AddComponent<T>();
        }

        internal static void CreateGridSystem(Tilemap ground, Tilemap road, Tilemap buildable,
                                              Tilemap villageSlot, Tilemap blocked)
        {
            var gridSystemGo = new GameObject("GridSystem");
            gridSystemGo.transform.SetParent(_servicesRoot);
            var gridSystem = gridSystemGo.AddComponent<GridSystem>();

            var so = new SerializedObject(gridSystem);
            so.FindProperty("_width").intValue = GreyboxMapData.Width;
            so.FindProperty("_height").intValue = GreyboxMapData.Height;
            so.FindProperty("_origin").vector3Value = GreyboxMapData.Origin;
            so.FindProperty("_cellSize").floatValue = 1f;
            so.FindProperty("_ground").objectReferenceValue = ground;
            so.FindProperty("_road").objectReferenceValue = road;
            so.FindProperty("_buildable").objectReferenceValue = buildable;
            so.FindProperty("_villageSlot").objectReferenceValue = villageSlot;
            so.FindProperty("_blocked").objectReferenceValue = blocked;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>탈출 지점: 금색 사각 테두리.</summary>
        internal static void DrawExitMarker(Transform parent)
        {
            var world = CellCenterWorld(GreyboxMapData.N13_Exit);
            float half = 0.55f;

            var go = new GameObject("ExitPoint");
            go.transform.SetParent(parent);
            go.transform.position = world;

            var line = go.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = new Color(1f, 0.84f, 0f);
            line.startWidth = line.endWidth = 0.08f;
            line.loop = true;
            line.positionCount = 4;
            line.SetPosition(0, world + new Vector3(-half, -half));
            line.SetPosition(1, world + new Vector3(half, -half));
            line.SetPosition(2, world + new Vector3(half, half));
            line.SetPosition(3, world + new Vector3(-half, half));
            line.sortingLayerName = "Deploy";
        }

        internal static Vector3 CellCenterWorld(Vector2Int cell)
        {
            return new Vector3(
                GreyboxMapData.Origin.x + cell.x + 0.5f,
                GreyboxMapData.Origin.y + cell.y + 0.5f,
                0f);
        }

        // ---------- 경로 노드 ----------

        internal static void BuildPathNodes()
        {
            var graph = Object.FindAnyObjectByType<PathGraph>();
            if (graph == null)
            {
                Debug.LogError("[SceneParts] PathGraph 없음 — BuildServices 먼저 실행할 것");
                return;
            }

            var authors = new PathNodeAuthoring[GreyboxMapData.Nodes.Length];
            for (int i = 0; i < GreyboxMapData.Nodes.Length; i++)
            {
                var def = GreyboxMapData.Nodes[i];
                var go = new GameObject(def.Name);
                go.transform.SetParent(graph.transform);
                go.transform.position = CellCenterWorld(def.Cell);

                var authoring = go.AddComponent<PathNodeAuthoring>();
                SetNodeFlags(authoring, def.IsStart, def.IsExit);
                authors[i] = authoring;
            }

            // 엣지 저작 (Bidirectional=true, Allowed=All) + 지름길 1개 (Escortee 전용).
            foreach (var (from, to) in GreyboxMapData.Edges)
                Connect(authors[from], authors[to], false);

            var sc = GreyboxMapData.ShortcutEdge;
            Connect(authors[sc.from], authors[sc.to], true);
        }

        private static void SetNodeFlags(PathNodeAuthoring authoring, bool isStart, bool isExit)
        {
            var so = new SerializedObject(authoring);
            so.FindProperty("_isEscorteeStart").boolValue = isStart;
            so.FindProperty("_isExit").boolValue = isExit;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Connect(PathNodeAuthoring from, PathNodeAuthoring to, bool shortcut)
        {
            var so = new SerializedObject(from);
            var array = so.FindProperty("_connections");
            int index = array.arraySize;
            array.arraySize++;
            var element = array.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Target").objectReferenceValue = to;
            element.FindPropertyRelative("Allowed").intValue =
                shortcut ? (int)PathAgent.Escortee : (int)PathAgent.All;
            element.FindPropertyRelative("IsShortcut").boolValue = shortcut;
            element.FindPropertyRelative("Bidirectional").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- 액터 ----------

        internal static void BuildActors(GreyboxFactory.Result factory)
        {
            _actorsRoot = new GameObject("--- Actors ---").transform;
            _actorsRoot.FindOrAddChild("Enemies");
            _actorsRoot.FindOrAddChild("Allies");

            // 보호대상 — 시작 노드 셀 중심.
            Object.Instantiate(factory.EscorteePrefab,
                        CellCenterWorld(GreyboxMapData.EscorteeSpawnCell),
                        Quaternion.identity, _actorsRoot).name = "Escortee";

            // 모체 — 우측 빌드가능 지역.
            Object.Instantiate(factory.MotherPrefab,
                        CellCenterWorld(GreyboxMapData.MotherSpawnCell),
                        Quaternion.identity, _actorsRoot).name = "Mother";

            // 마을 3곳.
            var villagesParent = new GameObject("Villages").transform;
            villagesParent.SetParent(GameObject.Find("--- Map ---").transform);
            for (int i = 0; i < GreyboxMapData.Villages.Length; i++)
            {
                var v = GreyboxMapData.Villages[i];
                Object.Instantiate(factory.VillagePrefab, CellCenterWorld(v),
                            Quaternion.identity, villagesParent).name = $"Village_{i + 1}";
            }
        }

        // ---------- UI ----------

        internal static void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 기본값(Constant Pixel Size)이면 해상도가 바뀔 때 레이아웃이 무너진다 (G-14).
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            BuildHudBottomLeft(canvasGo.transform);
            var hirePanel = BuildHirePanel(canvasGo.transform);
            BuildResultPanel(canvasGo.transform);
            canvasGo.AddComponent<UI.HudTopBar>();   // 상단 HUD 바 (G-14) — 자기 UI 를 스스로 만든다
            canvasGo.AddComponent<UI.PauseMenu>();   // 일시정지 메뉴 (G-12) — 자기 UI 를 스스로 만든다

            // DeploymentController 는 Start 에 FindAnyObjectByType 폴백이 있지만,
            // 인스펙터에 보이는 것이 진실이어야 하므로 여기서 명시 배선한다.
            var deployment = Object.FindAnyObjectByType<DeploymentController>();
            if (deployment != null)
            {
                var so = new SerializedObject(deployment);
                so.FindProperty("_hirePanel").objectReferenceValue = hirePanel;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Text MakeText(Transform parent, string name, string content,
                                     Vector2 anchorMin, Vector2 anchorMax,
                                     Vector2 offsetMin, Vector2 offsetMax,
                                     int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return text;
        }

        /// <summary>좌하단 조작부 — 배속 버튼 4개. 수치 표시는 상단 바(G-14)가 맡는다.</summary>
        private static void BuildHudBottomLeft(Transform canvas)
        {
            // 골드·체력·배속 "표시"는 상단 바(G-14, UI.HudTopBar)로 올라갔다.
            // 여기 좌하단에는 **조작부만** 남긴다 — 정보 위계를 만드는 것이 G-14 의 요점이다.
            //
            // 배속은 키보드(Space/1/2/3)로도 되지만 버튼이 없으면 존재를 알기 어렵다 — 클릭 버튼도 같이 둔다.
            // ⚠️ 이름을 "Btn_Pause" 에서 바꾸지 마라. 우상단 일시정지 메뉴 버튼은 "Btn_PauseMenu" 로 따로 있다.
            var pauseBtn = MakeButton(canvas, "Btn_Pause", "II",
                                      new Vector2(12f, 12f), new Vector2(52f, 44f));
            var speed1Btn = MakeButton(canvas, "Btn_Speed1", "1x",
                                       new Vector2(56f, 12f), new Vector2(96f, 44f));
            var speed2Btn = MakeButton(canvas, "Btn_Speed2", "2x",
                                       new Vector2(100f, 12f), new Vector2(140f, 44f));
            var speed4Btn = MakeButton(canvas, "Btn_Speed4", "4x",
                                       new Vector2(144f, 12f), new Vector2(184f, 44f));

            var controlsGo = new GameObject("SpeedControls");
            controlsGo.transform.SetParent(canvas, false);
            var controls = controlsGo.AddComponent<UI.SpeedControls>();
            var so = new SerializedObject(controls);
            so.FindProperty("_pauseButton").objectReferenceValue = pauseBtn;
            so.FindProperty("_speed1Button").objectReferenceValue = speed1Btn;
            so.FindProperty("_speed2Button").objectReferenceValue = speed2Btn;
            so.FindProperty("_speed4Button").objectReferenceValue = speed4Btn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button MakeButton(Transform parent, string name, string label,
                                         Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            go.GetComponent<Image>().color = new Color(0.25f, 0.28f, 0.35f);

            MakeText(go.transform, "Label", label, Vector2.zero, Vector2.one,
                     Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleCenter);

            return go.GetComponent<Button>();
        }

        private static UI.HirePanel BuildHirePanel(Transform canvas)
        {
            var panelGo = new GameObject("HirePanel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(canvas, false);

            // 좌하단 조작부(배속 버튼) 바로 위에 뜨도록 배치.
            var rect = (RectTransform)panelGo.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(16f, 56f);
            rect.sizeDelta = new Vector2(260f, 160f);

            panelGo.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.18f, 0.9f);

            MakeText(panelGo.transform, "Title", "고용",
                     new Vector2(0f, 1f), new Vector2(1f, 1f),
                     new Vector2(12f, -32f), new Vector2(-12f, -4f),
                     20, TextAnchor.MiddleLeft);

            var hirePanel = panelGo.AddComponent<UI.HirePanel>();
            var so = new SerializedObject(hirePanel);
            so.FindProperty("_buttonRoot").objectReferenceValue = (RectTransform)panelGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hirePanel;
        }

        private static Transform FindOrAddChild(this Transform parent, string name)
        {
            var found = parent.Find(name);
            if (found != null) return found;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void BuildResultPanel(Transform canvas)
        {
            var panelGo = new GameObject("ResultPanel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(canvas, false);

            var rect = (RectTransform)panelGo.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 220f);

            panelGo.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            MakeText(panelGo.transform, "ResultText", "",
                     new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                     new Vector2(16f, -40f), new Vector2(-16f, -8f),
                     42, TextAnchor.MiddleCenter);

            var buttonGo = new GameObject("RestartButton", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panelGo.transform, false);
            var bRect = (RectTransform)buttonGo.transform;
            bRect.anchorMin = new Vector2(0.5f, 0f);
            bRect.anchorMax = new Vector2(0.5f, 0f);
            bRect.pivot = new Vector2(0.5f, 0f);
            bRect.anchoredPosition = new Vector2(0f, 24f);
            bRect.sizeDelta = new Vector2(160f, 44f);
            buttonGo.GetComponent<Image>().color = new Color(0.25f, 0.4f, 0.7f);
            MakeText(buttonGo.transform, "Label", "재시작 (R)",
                     Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                     22, TextAnchor.MiddleCenter);

            panelGo.AddComponent<UI.ResultPanel>();
        }
    }
}
