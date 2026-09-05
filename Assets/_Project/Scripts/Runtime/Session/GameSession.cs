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

        /// <summary>설정에서 고른 난이도 (G-23). <b>판이 시작될 때 한 번 읽힌다</b> —
        /// 진행 중에는 바꿀 수 없고, 바꾸려면 다시 시작해야 한다.
        ///
        /// static 인 이유: <see cref="RestartStage"/> 가 씬을 다시 로드하므로 컴포넌트에 두면 날아간다.
        /// 도메인 리로드가 꺼져 있어 플레이 종료 후에도 값이 남으므로 아래에서 명시 초기화한다 (CLAUDE.md §3).</summary>
        public static Difficulty SelectedDifficulty { get; set; } = DefaultDifficulty;

        /// <summary>기본 난이도. 처음 켰을 때와 재생 시작마다 여기로 돌아온다.</summary>
        public const Difficulty DefaultDifficulty = Difficulty.Normal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            SelectedDifficulty = DefaultDifficulty;
        }

        [SerializeField] private StageDefinition _definition;
        [SerializeField] private Wallet _wallet;

        private Difficulty _difficulty = DefaultDifficulty;
        private DifficultyTier _tier;
        private bool _hasTier;
        [SerializeField] private Audio.SfxPlayer _sfx;   // 효과음 창구 (G-11). 비워 두면 Awake 에서 찾는다.

        private GameResult _result = GameResult.InProgress;
        private float _elapsedTime;
        private Escortee _escortee;

        public GameResult Result => _result;
        public float ElapsedTime => _elapsedTime;

        /// <summary>이 판이 시작될 때 확정된 난이도 (G-23). 진행 중에 바뀌지 않는다.
        /// 설정에서 고르는 값은 <see cref="SelectedDifficulty"/> 이고, 그건 <b>다음 판</b>부터 적용된다.</summary>
        public Difficulty ActiveDifficulty => _difficulty;

        /// <summary>진행 중인가. 설정 패널이 난이도를 잠글지 판단할 때 쓴다 (G-23).</summary>
        public bool IsRunInProgress => _result == GameResult.InProgress;

        /// <summary>적 1기 격파 보상. 난이도 표가 없으면 0 — 난이도 도입 전과 같은 경제가 된다.</summary>
        public float KillReward => _hasTier ? _tier.KillReward : 0f;
        public Wallet Wallet => _wallet;
        public StageDefinition Definition => _definition;
        public Audio.SfxPlayer Sfx => _sfx;

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
            if (_sfx == null) _sfx = FindAnyObjectByType<Audio.SfxPlayer>();
            if (_definition != null && _wallet != null)
            {
                // 난이도는 여기서 딱 한 번 확정된다 (G-23). 판 중에 바꿀 수 없는 이유가 이것이다 —
                // 수입 규칙이 도중에 바뀌면 그 판의 밸런스를 무엇으로 읽어야 할지 알 수 없다.
                _difficulty = SelectedDifficulty;
                _tier = _definition.TierFor(_difficulty);
                _hasTier = _definition.HasTierFor(_difficulty);

                float perSecond = _hasTier ? _tier.ResourcePerSecond : _definition.ResourcePerSecond;
                _wallet.Initialize(_definition.StartingResource, perSecond);
            }
            else
                Debug.LogError($"[{nameof(GameSession)}] StageDefinition 또는 Wallet 이 없습니다.", this);

            if (_sfx != null && _definition != null)
                _sfx.SetMasterVolume(_definition.MasterVolume);
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
            Debug.Log($"[{nameof(GameSession)}] {result} ({_elapsedTime:F1}초)");

            // 승패음은 Pause(timeScale=0) 적용 전에 — Pause 후에는 재생이 차단된다 (G-11).
            if (_sfx != null)
                _sfx.Play(result == GameResult.Victory ? Audio.SfxPlayer.SfxId.Victory
                                                        : Audio.SfxPlayer.SfxId.Defeat);

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
