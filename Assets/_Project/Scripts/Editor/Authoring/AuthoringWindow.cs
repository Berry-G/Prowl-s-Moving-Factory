/** .toon 검증/임포트/익스포트 창. IMGUI. 검증은 에셋을 건드리지 않는다. */
using System; using System.Collections.Generic; using System.IO; using System.Linq;
using UnityEditor; using UnityEngine; using PMF.Data;
namespace PMF.EditorTools.Authoring {
    public class AuthoringWindow : EditorWindow {
        const string PKey = "PMF.Authoring.CurrentStage";
        const string SDir = "Assets/_Project/Data/Stages";
        [MenuItem("PMF/Authoring/Stage Authoring\u2026")]
        public static void ShowWindow() { var w = GetWindow<AuthoringWindow>("Stage Authoring"); w.minSize = new Vector2(500,350); w.Show(); }
        string _path = ""; string _name; Vector2 _sp; readonly List<L> _log = new List<L>();
        struct L { public string t, s; }
        void OnEnable() { _name = EditorPrefs.GetString(PKey, "Stage_Greybox"); _path = SDir + "/" + _name + ".toon"; }
        void OnGUI() {
            GUILayout.Space(8); EditorGUILayout.LabelField("Stage Authoring", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _path = EditorGUILayout.TextField(".toon 경로", _path);
            if (GUILayout.Button("찾아보기", GUILayout.Width(80))) { var c = EditorUtility.OpenFilePanel(".toon", SDir, "toon"); if (!string.IsNullOrEmpty(c)) _path = c; }
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            _name = EditorGUILayout.TextField("스테이지 이름", _name);
            if (EditorGUI.EndChangeCheck()) { EditorPrefs.SetString(PKey, _name); if (string.IsNullOrEmpty(Path.GetDirectoryName(_path)) || _path.StartsWith(SDir)) _path = SDir + "/" + _name + ".toon"; }
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("검증", GUILayout.Height(30))) OnV();
            if (GUILayout.Button("임포트", GUILayout.Height(30))) OnI();
            if (GUILayout.Button("익스포트", GUILayout.Height(30))) OnE();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
            if (_log.Count > 0) {
                EditorGUILayout.LabelField("로그 (" + _log.Count + "줄)", EditorStyles.boldLabel);
                _sp = EditorGUILayout.BeginScrollView(_sp, GUILayout.ExpandHeight(true));
                foreach (var e in _log) {
                    var ic = e.s == "error" ? "\u274c " : e.s == "warning" ? "\u26a0\ufe0f " : "\u2139\ufe0f ";
                    var r = EditorGUILayout.GetControlRect();
                    EditorGUI.SelectableLabel(r, ic + e.t);
                    if (r.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown && Event.current.clickCount == 2) {
                        GUIUtility.systemCopyBuffer = e.t; Debug.Log("[AW] clip: " + e.t); Event.current.Use(); }
                }
                EditorGUILayout.EndScrollView();
            }
        }
        void OnV() {
            _log.Clear(); if (!File.Exists(_path)) { Log("error","파일 없음"); return; }
            try {
                var text = File.ReadAllText(_path); var rr = StageDocumentReader.Read(text);
                if (!rr.Ok) { foreach (var i in rr.Issues) Log(i.Severity, i.Id + " " + i.Path + " - " + i.Message); return; }
                var en = new HashSet<string>(); foreach (var g in AssetDatabase.FindAssets("t:EnemyDefinition")) en.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)));
                foreach (var i in AuthoringValidator.Validate(rr.Doc, en)) Log(i.Severity, i.Id + " " + i.Path + " - " + i.Message);
                Log("info", "검증 완료");
            } catch (Exception ex) { Log("error", "예외: " + ex.Message); }
        }
        void OnI() {
            _log.Clear(); if (!File.Exists(_path)) { Log("error","파일 없음"); return; }
            var sa = SDir + "/" + _name + ".asset";
            var so = AssetDatabase.LoadAssetAtPath<StageDefinition>(sa);
            if (so != null) {
                var at = File.GetLastWriteTimeUtc(sa); var tt = File.GetLastWriteTimeUtc(_path);
                if (at > tt) {
                    var c = EditorUtility.DisplayDialogComplex("SO 가 .toon 보다 새롭습니다",
                        "인스펙터에서 튜닝한 값이 있다면 임포트가 덮어씁니다.\n먼저 [익스포트] 로 파일에 되돌리는 것을 권합니다.",
                        "그래도 임포트", "취소", "익스포트 먼저");
                    if (c == 1) { Log("info","임포트 취소"); return; }
                    if (c == 2) { OnE(); return; }
                }
            }
            var r = AuthoringImporter.Import(_path);
            foreach (var i in r.Issues) Log(i.Severity, i.Id + " " + i.Path + " - " + i.Message);
            if (r.Ok) {
                Log("info", "임포트 완료 - " + _name);
                Log("info", "1. PMF/Create Greybox Scene (편집 모드에서)");
                Log("info", "2. 재생성 확인: SelectionController 존재, SO 튜닝값 유지, 마을 병종 2종 고용, git status 깨끗");
                Log("info", "3. 총수입 천장 재측정 (현재 402 / 56기 / 119초)");
                Log("info", "4. EditMode 테스트 전체 통과");
            } else Log("error", r.Message);
        }
        void OnE() {
            _log.Clear();
            try { ToonExporter.Export(_name); Log("info", "익스포트 완료 - " + SDir + "/" + _name + ".toon"); }
            catch (Exception ex) { Log("error", "익스포트 실패: " + ex.Message); }
        }
        void Log(string sev, string msg) { _log.Add(new L{t=msg,s=sev}); Repaint(); }
    }
}