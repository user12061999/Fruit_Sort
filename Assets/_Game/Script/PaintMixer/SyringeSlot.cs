using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Module Ống Tiêm: click vào slot để mở panel bơm/làm nhạt.
    /// - Bơm Trắng (White): thêm cmy=(0,0,0), làm nhạt màu hiện tại.
    /// - Bơm Đen (Black): thêm cmy=(1,1,1), làm đậm màu hiện tại.
    /// - Bơm C/M/Y: thêm kênh màu chỉ định.
    /// </summary>
    public class SyringeSlot : ModuleSlot
    {
        [Header("Syringe Settings")]
        [Tooltip("Lượng volume thêm/bớt mỗi lần bơm.")]
        public float injectVolume = 0.2f;

        [Header("UI Panel")]
        [Tooltip("Panel bơm (tạo bằng Canvas). Để trống nếu chưa làm UI.")]
        public GameObject syringePanel;
        [Tooltip("Button bơm Trắng.")]
        public Button btnWhite;
        [Tooltip("Button bơm Đen.")]
        public Button btnBlack;
        [Tooltip("Button bơm Cyan.")]
        public Button btnCyan;
        [Tooltip("Button bơm Magenta.")]
        public Button btnMagenta;
        [Tooltip("Button bơm Vàng.")]
        public Button btnYellow;

        PaintStream _currentStream;
        bool _panelOpen = false;

        protected override void Awake()
        {
            base.Awake();
            if (syringePanel != null) syringePanel.SetActive(false);
            SetupButtons();
        }

        void SetupButtons()
        {
            if (btnWhite != null) btnWhite.onClick.AddListener(() => InjectColor(Vector3.zero, "Trắng"));
            if (btnBlack != null) btnBlack.onClick.AddListener(() => InjectColor(Vector3.one, "Đen"));
            if (btnCyan != null) btnCyan.onClick.AddListener(() => InjectColor(new Vector3(1, 0, 0), "Cyan"));
            if (btnMagenta != null) btnMagenta.onClick.AddListener(() => InjectColor(new Vector3(0, 1, 0), "Magenta"));
            if (btnYellow != null) btnYellow.onClick.AddListener(() => InjectColor(new Vector3(0, 0, 1), "Vàng"));
        }

        protected override void OnSlotClicked()
        {
            if (currentModule == null || currentModule.moduleType != ModuleType.Syringe) return;

            // Tìm stream đang đi qua slot
            _currentStream = FindNearbyStream(0.8f);
            if (_currentStream == null)
            {
                // Không có stream: chỉ flash visual
                _sr.DOKill();
                _sr.DOColor(Color.yellow, 0.1f).OnComplete(RefreshSlotVisual);
                return;
            }

            TogglePanel();
        }

        void TogglePanel()
        {
            _panelOpen = !_panelOpen;
            if (syringePanel != null)
            {
                syringePanel.SetActive(_panelOpen);
                if (_panelOpen)
                    syringePanel.transform.DOScale(Vector3.one, 0.2f).From(Vector3.zero).SetEase(Ease.OutBack);
            }
            else
            {
                // Không có UI: log hướng dẫn debug
                Debug.Log("[SyringeSlot] Chưa gán syringePanel. Cần tạo Canvas UI.");
            }
        }

        /// <summary>Bơm màu vào PaintStream đang đi qua slot.</summary>
        public void InjectColor(Vector3 injectCMY, string label)
        {
            if (_currentStream == null) _currentStream = FindNearbyStream(0.8f);
            if (_currentStream == null) return;

            _currentStream.AddPaint(injectCMY, injectVolume);

            // Visual feedback trên slot
            _sr.DOKill();
            Color feedbackColor = PaintColorUtility.CMYToRGB(injectCMY);
            _sr.DOColor(feedbackColor, 0.1f).OnComplete(RefreshSlotVisual);

            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 4, 0.4f);

            Debug.Log($"[SyringeSlot] Bơm {label} (+{injectVolume:F2}v)");
        }

        protected override void OnStreamEnter(PaintStream stream)
        {
            _currentStream = stream;
        }

        protected override void OnStreamExit(PaintStream stream)
        {
            if (_currentStream == stream)
            {
                _currentStream = null;
                // Đóng panel khi stream rời đi
                if (_panelOpen)
                {
                    _panelOpen = false;
                    if (syringePanel != null) syringePanel.SetActive(false);
                }
            }
        }
    }
}
