using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FruitSort
{
    /// <summary>
    /// Luồng chơi theo chuỗi level: build LevelData -> theo dõi WIN / LOSE -> panel kết quả
    /// -> click để qua level kế / chơi lại. UI overlay tự dựng bằng code, không cần setup.
    ///
    /// WIN : mọi bucket trong level được lấp đầy (nghe event Bucket.OnBucketFull).
    /// LOSE: hết giờ (nếu LevelData.timeLimit > 0), HOẶC mọi gói đã cạn + không còn dot
    ///       hoạt động + không còn giỏ nào chứa dot có thể nhả ra (bế tắc thật sự).
    ///
    /// Cách dùng: thêm component này vào 1 GameObject trong scene gameplay (scene cần Camera;
    /// thiếu FallingPixelManager sẽ tự tạo). Để trống 'levels' = tự load toàn bộ LevelData
    /// trong Resources/Levels, sort theo tên.
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        [Tooltip("Chuỗi level theo thứ tự. Để TRỐNG = tự load Resources/Levels (sort theo tên).")]
        public LevelData[] levels;
        [Tooltip("Tự bắt đầu level đã lưu khi vào Play.")]
        public bool autoStart = true;
        [Tooltip("Lưu tiến độ bằng PlayerPrefs (key LoopSort.LevelIndex).")]
        public bool saveProgress = true;
        [Tooltip("Đợi bao lâu sau khi thắng/thua mới hiện panel (cho hiệu ứng chạy nốt).")]
        [Min(0f)] public float endPanelDelay = 1f;

        const string ProgressKey = "LoopSort.LevelIndex";
        const float StuckConfirmSeconds = 1.5f; // trạng thái bế tắc phải ổn định bấy nhiêu giây

        public static GameFlowManager Instance { get; private set; }

        public int CurrentLevelIndex => _index;
        public bool IsLevelRunning => _running && !_ended;

        GameObject _root;
        int _index;
        int _totalBuckets;
        int _filledBuckets;
        bool _running;
        bool _ended;
        bool _won;
        bool _panelReady; // panel đã hiện xong 1 frame, bắt đầu nhận click
        float _levelStartTime;
        float _stuckTimer;
        string _loseReason = "";
        readonly List<ModelDotSpawner> _spawners = new List<ModelDotSpawner>();
        readonly List<Bucket> _buckets = new List<Bucket>();

        // ---- UI tự dựng ----
        GameObject _panel;
        Text _title, _subtitle, _hud;
        int _lastHudFilled = -1, _lastHudSecs = -2;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
        }

        void OnEnable() { Bucket.OnBucketFull += HandleBucketFull; }
        void OnDisable() { Bucket.OnBucketFull -= HandleBucketFull; }

        void Start()
        {
            if (levels == null || levels.Length == 0)
            {
                levels = Resources.LoadAll<LevelData>("Levels");
                System.Array.Sort(levels, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("[GameFlow] Không có level nào: gán 'levels' hoặc đặt LevelData trong Resources/Levels.", this);
                enabled = false;
                return;
            }

            EnsureFallingManager();

            if (autoStart)
                StartLevel(saveProgress ? PlayerPrefs.GetInt(ProgressKey, 0) : 0);
        }

        void EnsureFallingManager()
        {
            if (FallingPixelManager.Instance != null) return;
            if (FindFirstObjectByType<FallingPixelManager>() != null) return;
            var go = new GameObject("_FallingPixelManager");
            go.AddComponent<FallingPixelManager>();
        }

        /// <summary>Dựng và bắt đầu level thứ <paramref name="index"/> (clamp vào chuỗi).</summary>
        public void StartLevel(int index)
        {
            _index = Mathf.Clamp(index, 0, levels.Length - 1);

            if (_root != null) Destroy(_root);
            _root = LevelBuilder.Build(levels[_index]);
            if (_root == null) { enabled = false; return; }

            _spawners.Clear();
            _root.GetComponentsInChildren(true, _spawners);
            _buckets.Clear();
            _root.GetComponentsInChildren(true, _buckets);
            _totalBuckets = _buckets.Count;
            _filledBuckets = 0;

            _running = true;
            _ended = false;
            _panelReady = false;
            _stuckTimer = 0f;
            _levelStartTime = Time.time;
            _panel.SetActive(false);
            _lastHudFilled = -1; // ép HUD vẽ lại
            UpdateHud();
        }

        public void RestartLevel() { StartLevel(_index); }

        void HandleBucketFull(Bucket b)
        {
            if (!_running || _ended || b == null) return;
            if (_root == null || !b.transform.IsChildOf(_root.transform)) return;

            _filledBuckets++;
            UpdateHud();
            if (_filledBuckets >= _totalBuckets) EndLevel(true, "");
        }

        void Update()
        {
            if (!_running) return;

            if (_ended)
            {
                if (_panelReady && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (_won) StartLevel(NextIndex());
                    else RestartLevel();
                }
                return;
            }

            UpdateHud();

            // ---- LOSE: hết giờ ----
            float limit = levels[_index].timeLimit;
            if (limit > 0f && Time.time - _levelStartTime >= limit)
            {
                EndLevel(false, "Hết giờ!");
                return;
            }

            // ---- LOSE: bế tắc (hết nguyên liệu) ----
            // Mọi gói cạn + không còn dot hoạt động + không giỏ nào còn dot để nhả.
            // Đợi ổn định vài giây để không bắt nhầm lúc dot đang bay vào giỏ.
            if (AllSpawnersDepleted() && NoActiveDots() && !AnyReleasableBucket())
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer >= StuckConfirmSeconds) EndLevel(false, "Hết nguyên liệu!");
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        bool AllSpawnersDepleted()
        {
            for (int i = 0; i < _spawners.Count; i++)
            {
                var s = _spawners[i];
                if (s == null) continue;
                if (s.gameObject.activeInHierarchy && !s.IsDepleted) return false;
            }
            return true;
        }

        static bool NoActiveDots()
        {
            var fm = FallingPixelManager.Instance;
            return fm == null || fm.ActiveCount == 0;
        }

        bool AnyReleasableBucket()
        {
            for (int i = 0; i < _buckets.Count; i++)
            {
                var b = _buckets[i];
                if (b != null && b.IsActive && b.currentFill > 0) return true;
            }
            return false;
        }

        int NextIndex()
        {
            int next = _index + 1;
            return next >= levels.Length ? 0 : next; // hết chuỗi -> vòng về đầu
        }

        void EndLevel(bool won, string reason)
        {
            if (_ended) return;
            _ended = true;
            _won = won;
            _loseReason = reason;

            if (saveProgress)
                PlayerPrefs.SetInt(ProgressKey, won ? NextIndex() : _index);

            StartCoroutine(ShowEndPanel());
        }

        IEnumerator ShowEndPanel()
        {
            yield return new WaitForSeconds(endPanelDelay);
            if (_root != null) _root.SetActive(false); // đóng băng level phía sau panel

            bool lastLevel = _index >= levels.Length - 1;
            if (_won)
            {
                _title.text = lastLevel ? "THẮNG TOÀN BỘ!" : $"LEVEL {_index + 1} HOÀN THÀNH!";
                _title.color = new Color(0.45f, 0.95f, 0.45f);
                _subtitle.text = lastLevel ? "Click để chơi lại từ đầu" : $"Click để vào Level {_index + 2}";
            }
            else
            {
                _title.text = "THUA RỒI!";
                _title.color = new Color(1f, 0.45f, 0.4f);
                _subtitle.text = _loseReason + "\nClick để thử lại";
            }
            _panel.SetActive(true);

            yield return null; // bỏ 1 frame để click cuối cùng của gameplay không bấm xuyên panel
            _panelReady = true;
        }

        // =====================================================
        // UI dựng bằng code (uGUI; nhận click qua Input System, không cần EventSystem)
        // =====================================================

        void BuildUI()
        {
            var canvasGo = new GameObject("_FlowCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // HUD nhỏ trên đỉnh: Level x/y + số giỏ + đồng hồ (nếu có timeLimit).
            _hud = MakeText(canvasGo.transform, "Hud", 44, FontStyle.Bold, TextAnchor.UpperCenter);
            RectTransform hudRt = _hud.rectTransform;
            hudRt.anchorMin = new Vector2(0f, 1f);
            hudRt.anchorMax = new Vector2(1f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.anchoredPosition = new Vector2(0f, -28f);
            hudRt.sizeDelta = new Vector2(0f, 80f);

            // Panel kết quả (nền tối + 2 dòng chữ).
            _panel = new GameObject("EndPanel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var img = _panel.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.72f);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _title = MakeText(_panel.transform, "Title", 96, FontStyle.Bold, TextAnchor.MiddleCenter);
            RectTransform trt = _title.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(0f, 120f);
            trt.sizeDelta = new Vector2(1000f, 220f);

            _subtitle = MakeText(_panel.transform, "Subtitle", 52, FontStyle.Normal, TextAnchor.MiddleCenter);
            RectTransform srt = _subtitle.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, -80f);
            srt.sizeDelta = new Vector2(1000f, 200f);

            _panel.SetActive(false);
        }

        static Text MakeText(Transform parent, string name, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        void UpdateHud()
        {
            if (_hud == null || levels == null || _index >= levels.Length) return;

            float limit = levels[_index].timeLimit;
            int secs = limit > 0f && !_ended
                ? Mathf.Max(0, Mathf.CeilToInt(limit - (Time.time - _levelStartTime)))
                : -1;
            if (_filledBuckets == _lastHudFilled && secs == _lastHudSecs) return;
            _lastHudFilled = _filledBuckets;
            _lastHudSecs = secs;

            string txt = $"Level {_index + 1}/{levels.Length}    Giỏ {_filledBuckets}/{_totalBuckets}";
            if (secs >= 0) txt += $"    Còn {secs}s";
            _hud.text = txt;
        }
    }
}
