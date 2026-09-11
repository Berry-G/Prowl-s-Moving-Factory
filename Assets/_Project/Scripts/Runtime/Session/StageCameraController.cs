using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PMF.Grid;

namespace PMF.Session
{
    /// <summary>목적: 가변 맵 카메라. 구조: GridSystem 월드 경계 사용. 불변: UI 위 입력 무시/시간배속 독립. 근거: ADR-0023.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class StageCameraController : MonoBehaviour
    {
        [SerializeField] private float _panSpeed = 12f;
        [SerializeField] private float _edgePixels = 20f;
        [SerializeField] private float _zoomStep = 0.15f;
        [SerializeField] private float _minimumSize = 2f;
        private Camera _camera;
        private GridSystem _grid;
        private Bounds _bounds;
        private float _fitSize;
        private float _aspect;
        private bool _edgeEnabled;
        private bool _dragging;
        private Vector2 _previousPointer;
        private void Awake() => _camera = GetComponent<Camera>();
        private void OnEnable() { UserSettings.Changed += ReadSettings; ReadSettings(); }
        private void OnDisable() { UserSettings.Changed -= ReadSettings; _dragging = false; }
        private void ReadSettings() => _edgeEnabled = UserSettings.Current.EdgeScrolling;
        private void Start()
        {
            _grid = GridSystem.Instance;
            if (_grid == null) { enabled = false; return; }
            // 왜: 셀→월드 수식은 GridSystem에만 둔다. 바깥 셀 중심 사이 중점을 경계로 사용.
            var min = (_grid.CellToWorld(new GridCoord(-1,-1)) + _grid.CellToWorld(new GridCoord(0,0))) * 0.5f;
            var max = (_grid.CellToWorld(new GridCoord(_grid.Width-1,_grid.Height-1)) + _grid.CellToWorld(new GridCoord(_grid.Width,_grid.Height))) * 0.5f;
            _bounds = new Bounds((min+max)*0.5f, max-min);
            RefreshAspect(true);
        }
        public static Vector3 ClampPosition(Vector3 p, Bounds bounds, float size, float aspect)
        {
            float halfWidth = size * aspect;
            p.x = bounds.size.x <= halfWidth*2 ? bounds.center.x : Mathf.Clamp(p.x,bounds.min.x+halfWidth,bounds.max.x-halfWidth);
            p.y = bounds.size.y <= size*2 ? bounds.center.y : Mathf.Clamp(p.y,bounds.min.y+size,bounds.max.y-size);
            return p;
        }
        public static Vector2 EdgeDirection(Vector2 p, Rect viewport, float margin)
        {
            if (!viewport.Contains(p)) return Vector2.zero;
            var v = new Vector2(p.x < viewport.xMin+margin ? -1 : p.x > viewport.xMax-margin ? 1 : 0,
                                p.y < viewport.yMin+margin ? -1 : p.y > viewport.yMax-margin ? 1 : 0);
            return v.normalized;
        }
        private void RefreshAspect(bool initial)
        {
            _aspect = Mathf.Max(_camera.aspect,0.01f);
            _fitSize = Mathf.Max(_bounds.extents.y,_bounds.extents.x/_aspect);
            _camera.orthographicSize = initial ? _fitSize : Mathf.Min(_camera.orthographicSize,_fitSize);
            if (initial) transform.position = new Vector3(_bounds.center.x,_bounds.center.y,transform.position.z);
            Clamp();
        }
        private void Clamp() => transform.position = ClampPosition(transform.position,_bounds,_camera.orthographicSize,_aspect);
        private void LateUpdate()
        {
            if (_grid == null) return;
            if (Mathf.Abs(_camera.aspect-_aspect)>0.001f) RefreshAspect(false);
            var mouse = Mouse.current;
            if (mouse == null || !Application.isFocused || UI.PauseMenu.IsOpen || UI.ShortcutPanel.IsOpen || (GameClock.Instance != null && GameClock.Instance.IsUiSlowMotion))
            { _dragging=false; return; }
            Vector2 p = mouse.position.ReadValue();
            if (!_camera.pixelRect.Contains(p) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            { _dragging=false; return; }
            ApplyPointer(p, mouse.scroll.ReadValue().y, mouse.middleButton.wasPressedThisFrame, mouse.middleButton.isPressed, Time.unscaledDeltaTime);
        }

        // 왜: 장치 입력 수집과 이동 처리를 분리해 포커스 없는 테스트에서도 동일한 이동 경로를 검증한다.
        internal void ApplyPointer(Vector2 p, float wheel, bool middlePressed, bool middleHeld, float deltaTime)
        {
            if (wheel != 0f)
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize * Mathf.Exp(-Mathf.Sign(wheel)*_zoomStep),Mathf.Min(_minimumSize,_fitSize),_fitSize);
            if (middlePressed) { _dragging=true; _previousPointer=p; }
            if (!middleHeld) _dragging=false;
            if (_dragging)
            {
                Vector3 previous = _camera.ScreenToWorldPoint(new Vector3(_previousPointer.x,_previousPointer.y,0));
                Vector3 current = _camera.ScreenToWorldPoint(new Vector3(p.x,p.y,0));
                transform.position += previous-current; _previousPointer=p;
            }
            else if (_edgeEnabled)
            {
                Vector2 direction=EdgeDirection(p,_camera.pixelRect,_edgePixels);
                transform.position += (Vector3)(direction * (_panSpeed*deltaTime));
            }
            Clamp();
        }
    }
}
