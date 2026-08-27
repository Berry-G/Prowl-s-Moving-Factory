using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using PMF.Actors;
using PMF.Combat;
using PMF.Data;
using PMF.Pathing;

namespace PMF.Session
{
    /// <summary>판 전체 상태. 승패 판정과 전역 이벤트 허브.</summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private StageDefinition _definition;
        [SerializeField] private Wallet _wallet;

        private GameResult _result = GameResult.InProgress;
        private float _elapsedTime;
        private Escortee _escortee;

        public GameResult Result => _result;
        public float ElapsedTime => _elapsedTime;
        public Wallet Wallet => _wallet;
        public StageDefinition Definition => _definition;

        /// <summary>현재 보호대상. RegisterEscortee 로 등록된다.</summary>
        public Escortee Escortee => _escortee;

        /// <summary>보호대상이 경로 노드를 통과했을 때. 잡몹/모체의 경로 재계산 트리거.</summary>
        public event Action<PathNode> OnEscorteeReachedNode;

        /// <summary>승패가 확정되었을 때. UI 가 구독한다.</summary>
        public event Action<GameResult> OnGameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[{nameof(GameSession)}] 씬에 두 개 이상 존재합니다.", this);
                enabled = false;
                return;
            }

            Instance = this;

            // static TargetRegistry 는 씬 리로드로는 초기화되지 않는다 — 여기서 명시 초기화.
            TargetRegistry.Clear();

            if (_wallet == null) _wallet = GetComponentInChildren<Wallet>();
            if (_definition != null && _wallet != null)
                _wallet.Initialize(_definition.StartingResource, _definition.ResourcePerSecond);
            else
                Debug.LogError($"[{nameof(GameSession)}] StageDefinition 또는 Wallet 이 없습니다.", this);
        }

        private void Start()
        {
            // UI 슬로우모션 배속은 감각 수치다. 코드 상수 대신 StageDefinition 에서 주입 (P-18B 부채 2).
            if (_definition != null)
                GameClock.Instance?.SetUiSlowScale(_definition.UiSlowMotionScale);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_result == GameResult.InProgress)
                _elapsedTime += Time.deltaTime;
        }

        internal void RegisterEscortee(Escortee escortee) => _escortee = escortee;

        internal void NotifyEscorteeReachedNode(PathNode node)
            => OnEscorteeReachedNode?.Invoke(node);

        public void DeclareVictory() => EndGame(GameResult.Victory);
        public void DeclareDefeat() => EndGame(GameResult.Defeat);

        private void EndGame(GameResult result)
        {
            if (_result != GameResult.InProgress) return;

            _result = result;
            Debug.Log($"[GameSession] {result} ({_elapsedTime:F1}초)");

            GameClock.Instance?.Pause();
            OnGameEnded?.Invoke(result);
        }

        /// <summary>현재 씬 리로드. 로드 전 timeScale 복구 + 레지스트리 초기화.</summary>
        public void RestartStage()
        {
            // 배속/일시정지 상태로 재시작해도 정상 속도로 시작해야 한다.
            // Time.timeScale 직접 대입 금지 — GameClock 을 통해서만 되돌린다.
            var clock = GameClock.Instance;
            if (clock != null)
            {
                clock.SetSpeed(1f);
                clock.Resume();
            }

            TargetRegistry.Clear();
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
    }
}
