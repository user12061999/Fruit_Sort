using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HAVIGAME;

namespace FruitSort
{
    /// <summary>
    /// Tracks score, move limits, win/lose state, and optional HUD text.
    /// </summary>
    public class GamePlayManager : MonoBehaviour
    {
        public enum PlayState
        {
            Playing,
            Won,
            Lost
        }

        public enum LoseReason
        {
            None,
            OutOfTime,
            OutOfMoves
        }

        public static GamePlayManager Instance { get; private set; }

        [Header("Refs")]
        public FallingPixelManager fallingManager;

        [Header("Moves")]
        [Tooltip("Maximum bucket/spawner interactions. 0 = unlimited.")]
        [Min(0)] public int moveLimit = 0;
        [Tooltip("Runtime debug value for remaining moves.")]
        public int movesLeftDebug;

        [Header("Time")]
        [Tooltip("Maximum level time in seconds. 0 = unlimited.")]
        [Min(0f)] public float timeLimit = 0f;
        [Tooltip("Runtime debug value for remaining time in seconds.")]
        public float timeLeftDebug;

        [Header("Score")]
        public int scorePerSorted = 10;
        public int scorePerBucket = 100;
        public int score = 0;

        [Header("UI")]
        public Text scoreText;
        public Text dotsLeftText;
        public Text onBeltText;
        public Text movesLeftText;
        public Text timeLeftText;

        [Header("Game State Events")]
        public UnityEvent onWin;
        public UnityEvent onLose;

        /// <summary>Bắn SAU khi một action (move) được ghi nhận — dùng để cache tiến trình.</summary>
        public static event System.Action onInteractionRecorded;

        int _movesLeft;
        int _lastScore = int.MinValue;
        int _lastOnBelt = int.MinValue;
        int _lastMovesLeft = int.MinValue;
        int _lastTimeLeftSeconds = int.MinValue;
        float _timeLeft;
        bool _timeCounting;
        bool _hasStartedInteraction;
        bool _progressRestored;
        PlayState _state = PlayState.Playing;
        LoseReason _loseReason = LoseReason.None;

        public int MovesLeft => HasMoveLimit ? _movesLeft : int.MaxValue;
        public bool HasMoveLimit => moveLimit > 0;
        public float TimeLeft => HasTimeLimit ? _timeLeft : float.PositiveInfinity;
        public int TimeLeftSeconds => HasTimeLimit ? Mathf.CeilToInt(Mathf.Max(0f, _timeLeft)) : int.MaxValue;
        public bool HasTimeLimit => timeLimit > 0f;
        public bool IsTimeCounting => _timeCounting;
        public bool HasStartedInteraction => _hasStartedInteraction;
        public bool CanUseMove => _state == PlayState.Playing && (!HasMoveLimit || _movesLeft > 0);
        public bool HasWon => _state == PlayState.Won;
        public bool HasLost => _state == PlayState.Lost;
        public PlayState State => _state;
        public LoseReason LastLoseReason => _loseReason;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            ResetMoves();
            ResetTime();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (fallingManager == null) fallingManager = FallingPixelManager.Instance;
            // Đã khôi phục tiến trình từ save trước khi Start chạy -> không reset đè lên.
            if (!_progressRestored)
            {
                ResetMoves();
                ResetTime();
            }
            RefreshUI();
        }

        void Update()
        {
            TickTime();
            EvaluateOutOfMoves();
            RefreshUI();
        }

        public void ConfigureFromLevel(LevelData level)
        {
            if (level == null) return;
            moveLimit = Mathf.Max(0, level.moveLimit);
            timeLimit = Mathf.Max(0f, level.timeLimit);
            _hasStartedInteraction = false;
            _progressRestored = false;
            ResetMoves();
            ResetTime();
        }

        /// <summary>
        /// Khôi phục tiến trình từ save. Gọi SAU <see cref="ConfigureFromLevel"/> khi resume level.
        /// Không khôi phục hasStartedInteraction: interaction đầu sau resume sẽ khởi động lại
        /// đồng hồ (như lần vào level đầu) để người chơi không mất giờ trong lúc chưa thao tác.
        /// </summary>
        public void RestoreProgress(int savedMovesLeft, float savedTimeLeft, int savedScore)
        {
            _state = PlayState.Playing;
            _loseReason = LoseReason.None;
            _hasStartedInteraction = false;
            _timeCounting = false;

            if (HasMoveLimit) _movesLeft = Mathf.Clamp(savedMovesLeft, 0, moveLimit);
            movesLeftDebug = _movesLeft;

            // Chừa tối thiểu 1s để người chơi không thua ngay tại frame đầu sau resume.
            if (HasTimeLimit) _timeLeft = Mathf.Clamp(savedTimeLeft, 1f, timeLimit);
            timeLeftDebug = _timeLeft;

            score = savedScore;
            _progressRestored = true;
            _lastMovesLeft = int.MinValue;
            _lastTimeLeftSeconds = int.MinValue;
            DispatchMovesChanged();
            RefreshUI();
        }

        public void ResetMoves()
        {
            _movesLeft = Mathf.Max(0, moveLimit);
            movesLeftDebug = _movesLeft;
            _state = PlayState.Playing;
            _loseReason = LoseReason.None;
            _lastMovesLeft = int.MinValue;
            DispatchMovesChanged();
        }

        public void ResetTime()
        {
            _timeLeft = Mathf.Max(0f, timeLimit);
            timeLeftDebug = _timeLeft;
            _timeCounting = false;
            _lastTimeLeftSeconds = int.MinValue;
        }

        public void SetTimeCounting(bool isCounting)
        {
            _timeCounting = isCounting && HasTimeLimit && _state == PlayState.Playing;
        }

        public bool RecordInteraction()
        {
            if (!CanUseMove) return false;

            if (!_hasStartedInteraction)
            {
                _hasStartedInteraction = true;
                SetTimeCounting(true);

                if (ClassicLevelController.instance != null)
                {
                    ClassicLevelController.instance.StartCountdownOnFirstInteraction();
                }
            }

            if (HasMoveLimit)
            {
                _movesLeft = Mathf.Max(0, _movesLeft - 1);
                movesLeftDebug = _movesLeft;
                DispatchMovesChanged();
            }

            EvaluateOutOfMoves();
            RefreshUI();
            onInteractionRecorded?.Invoke();
            return true;
        }

        void DispatchMovesChanged()
        {
            EventDispatcher.Dispatch(new GameEvent.LevelMovesChanged(
                HasMoveLimit,
                _movesLeft,
                moveLimit));
        }

        void TickTime()
        {
            if (_state != PlayState.Playing || !HasTimeLimit || !_timeCounting) return;

            _timeLeft = Mathf.Max(0f, _timeLeft - Time.deltaTime);
            timeLeftDebug = _timeLeft;
            if (_timeLeft <= 0f) Lose(LoseReason.OutOfTime);
        }

        public void OnDotSorted(Dot d)
        {
            if (_state != PlayState.Playing) return;
            score += scorePerSorted;
        }

        public void OnBucketFilled(Bucket b)
        {
            if (_state != PlayState.Playing) return;
            score += scorePerBucket;

            if (AreAllBucketsFull())
                Win();
            else
                EvaluateOutOfMoves();
        }

        void EvaluateOutOfMoves()
        {
            if (_state != PlayState.Playing || !HasMoveLimit || _movesLeft > 0) return;

            if (AreAllBucketsFull())
            {
                Win();
                return;
            }

            if (CanStillCompleteAnyBucket()) return;
            Lose(LoseReason.OutOfMoves);
        }

        bool AreAllBucketsFull()
        {
            // Dùng registry Bucket.All thay cho FindObjectsByType — hàm này được gọi
            // mỗi frame (EvaluateOutOfMoves) nên không được quét scene.
            System.Collections.Generic.IReadOnlyList<Bucket> buckets = Bucket.All;
            if (buckets == null || buckets.Count == 0) return false;

            for (int i = 0; i < buckets.Count; i++)
            {
                Bucket bucket = buckets[i];
                if (bucket != null && !bucket.IsFull) return false;
            }
            return true;
        }

        bool CanStillCompleteAnyBucket()
        {
            System.Collections.Generic.IReadOnlyList<Bucket> buckets = Bucket.All;
            if (buckets == null || buckets.Count == 0) return false;

            FallingPixelManager fm = fallingManager != null ? fallingManager : FallingPixelManager.Instance;
            for (int i = 0; i < buckets.Count; i++)
            {
                Bucket bucket = buckets[i];
                if (bucket == null || !bucket.CanStillFillForWin) continue;

                int needed = bucket.RemainingFillForWin;
                if (needed <= 0) return true;

                int available = 0;
                if (fm != null) available += fm.CountActiveDotsByColor(bucket.colorId);
                available += ModelDotSpawner.CountPendingDotsForColor(bucket.colorId);
                available += Bucket.CountPendingReleaseDotsForColor(bucket.colorId);
                if (available >= needed) return true;
            }
            return false;
        }

        void Win()
        {
            if (_state != PlayState.Playing) return;
            _state = PlayState.Won;
            _loseReason = LoseReason.None;
            onWin?.Invoke();
        }

        void Lose(LoseReason reason)
        {
            if (_state != PlayState.Playing) return;
            _state = PlayState.Lost;
            _loseReason = reason;
            onLose?.Invoke();
        }

        /// <summary>
        /// Revive sau khi thua do hết move: cộng thêm lượt và cho chơi tiếp.
        /// Nới moveLimit theo để movesLeft không bị clamp mất khi save/restore.
        /// </summary>
        public void ReviveWithMoves(int amount)
        {
            if (amount <= 0 || !HasMoveLimit || _state == PlayState.Won) return;

            _movesLeft += amount;
            if (_movesLeft > moveLimit) moveLimit = _movesLeft;
            movesLeftDebug = _movesLeft;

            if (_state == PlayState.Lost)
            {
                _state = PlayState.Playing;
                _loseReason = LoseReason.None;
            }

            if (_hasStartedInteraction) SetTimeCounting(true);
            DispatchMovesChanged();
            RefreshUI();
        }

        /// <summary>
        /// Revive sau khi thua do hết giờ: cộng thêm giây và cho chơi tiếp.
        /// LevelTimer (đồng hồ UI) cộng riêng qua ClassicLevelController.AddMoreSeconds.
        /// </summary>
        public void ReviveWithTime(float seconds)
        {
            if (seconds <= 0f || !HasTimeLimit || _state == PlayState.Won) return;

            _timeLeft += seconds;
            if (_timeLeft > timeLimit) timeLimit = _timeLeft;
            timeLeftDebug = _timeLeft;
            _lastTimeLeftSeconds = int.MinValue;

            if (_state == PlayState.Lost)
            {
                _state = PlayState.Playing;
                _loseReason = LoseReason.None;
            }

            if (_hasStartedInteraction) SetTimeCounting(true);
            RefreshUI();
        }

        void RefreshUI()
        {
            if (scoreText != null && score != _lastScore)
            {
                scoreText.text = $"Score: {score}";
                _lastScore = score;
            }

            if (onBeltText != null && fallingManager != null)
            {
                int v = fallingManager.ActiveCount;
                if (v != _lastOnBelt)
                {
                    onBeltText.text = $"On belt: {v}";
                    _lastOnBelt = v;
                }
            }

            if (movesLeftText != null && _movesLeft != _lastMovesLeft)
            {
                movesLeftText.text = HasMoveLimit ? $"Moves: {_movesLeft}" : "Moves: --";
                _lastMovesLeft = _movesLeft;
            }

            if (timeLeftText != null)
            {
                int seconds = TimeLeftSeconds;
                if (seconds != _lastTimeLeftSeconds)
                {
                    timeLeftText.text = HasTimeLimit ? $"Time: {FormatSeconds(seconds)}" : "Time: --";
                    _lastTimeLeftSeconds = seconds;
                }
            }
        }

        static string FormatSeconds(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
