using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace FruitSort
{
    /// <summary>
    /// Vật cản CHẶN dot (không tiêu hao): dot bay/rơi bị đẩy ra ngoài và nằm tì lên bề mặt,
    /// dot trên băng bị giữ đứng yên. Khi có đủ <see cref="requiredDots"/> dot ĐANG đè lên
    /// cùng lúc thì vật cản vỡ và biến mất, dot tiếp tục đi bình thường.
    /// KHÔNG dùng physics callback — FallingPixelManager poll AABB mỗi frame và báo tiếp xúc
    /// qua <see cref="RegisterPress"/> giữa cặp <see cref="BeginFrameAll"/>/<see cref="EndFrameAll"/>.
    /// </summary>
    public class DotObstacle : MonoBehaviour
    {
        [Header("Luật")]
        [Tooltip("Số dot phải ĐANG đè lên cùng lúc để vật cản vỡ.")]
        [Min(1)] public int requiredDots = 10;
        [Tooltip("Chặn dot đang bay/rơi (dot nằm tì lên bề mặt vật cản).")]
        public bool blockFlyingDots = true;
        [Tooltip("Chặn dot đang chạy trên băng chuyền (dot đứng yên trước vật cản).")]
        public bool blockBeltDots = true;

        [Header("Vùng chặn")]
        [Tooltip("Collider2D làm vùng chặn (AABB bounds). Để trống = dùng bounds của Body, " +
                 "không có nữa thì fallback hộp vuông theo bán kính.")]
        public Collider2D zone;
        [Tooltip("Sprite thân vật cản. Để trống = tự lấy SpriteRenderer trên object.")]
        public SpriteRenderer body;
        [Tooltip("Nửa cạnh hộp fallback khi không có zone lẫn body.")]
        [Min(0.05f)] public float fallbackRadius = 0.5f;

        [Header("Hiển thị số dot")]
        [Tooltip("Text hiển thị số dot còn thiếu để vỡ. Để trống = tự tạo TextMeshPro con lúc play.")]
        public TMP_Text countText;
        [Tooltip("Vị trí local của text so với tâm vật cản (chỉ dùng khi text tự tạo).")]
        public Vector2 countTextOffset = Vector2.zero;
        [Tooltip("Cỡ chữ world-space của text tự tạo.")]
        [Min(0.5f)] public float countTextSize = 3f;
        [Tooltip("Cường độ punch scale của text khi số thay đổi (0 = tắt).")]
        [Range(0f, 1f)] public float countTextPop = 0.35f;
        [Tooltip("Thời lượng punch của text.")]
        [Min(0.05f)] public float countTextPopDuration = 0.2f;

        [Header("Hiệu ứng")]
        [Tooltip("Cường độ punch scale khi vỡ.")]
        [Range(0f, 1f)] public float breakPunch = 0.35f;
        [Tooltip("Thời gian fade out trước khi biến mất.")]
        [Min(0.05f)] public float fadeOutDuration = 0.3f;

        // ---- runtime ----
        int _pressing;          // số dot đang tì lên trong frame hiện tại
        bool _broken;
        int _lastShownRemaining = int.MinValue;
        SpriteGridFill _gridFill;

        /// <summary>Số dot đang đè lên (frame gần nhất).</summary>
        public int Pressing => _pressing;
        /// <summary>Số dot còn thiếu để vỡ.</summary>
        public int Remaining => Mathf.Max(0, requiredDots - _pressing);
        public bool IsBroken => _broken;

        // Registry tĩnh (giống Bucket.All): FallingPixelManager poll trực tiếp,
        // không phụ thuộc thứ tự khởi tạo manager/vật cản.
        static readonly List<DotObstacle> s_all = new List<DotObstacle>();
        public static IReadOnlyList<DotObstacle> All => s_all;

        /// <summary>Gọi đầu frame (trước khi di chuyển dot): reset bộ đếm tiếp xúc.</summary>
        public static void BeginFrameAll()
        {
            for (int i = 0; i < s_all.Count; i++)
                if (s_all[i] != null) s_all[i]._pressing = 0;
        }

        /// <summary>Gọi cuối frame (sau khi di chuyển dot): chốt bộ đếm, đủ dot thì vỡ.</summary>
        public static void EndFrameAll()
        {
            for (int i = 0; i < s_all.Count; i++)
            {
                DotObstacle o = s_all[i];
                if (o == null || o._broken) continue;
                o.UpdateVisual();
                if (o._pressing >= o.requiredDots) o.Break();
            }
        }

        void OnEnable()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (body != null && _gridFill == null) _gridFill = body.GetComponent<SpriteGridFill>();
            if (!s_all.Contains(this)) s_all.Add(this);
            UpdateVisual();
        }

        void OnDisable()
        {
            s_all.Remove(this);
        }

        /// <summary>
        /// Reset trạng thái theo requiredDots hiện tại. LevelBuilder PHẢI gọi sau khi gán
        /// config lúc runtime (OnEnable đã chạy trước đó với giá trị prefab).
        /// </summary>
        public void ReinitializeFromConfig()
        {
            _pressing = 0;
            _broken = false;
            _lastShownRemaining = int.MinValue;
            UpdateVisual();
        }

        /// <summary>AABB vùng chặn (world). False nếu không xác định được.</summary>
        public bool TryGetWorldBounds(out Bounds bounds)
        {
            if (zone != null)
            {
                bounds = zone.bounds;
                return true;
            }
            if (body != null && body.sprite != null)
            {
                bounds = body.bounds;
                return true;
            }
            bounds = new Bounds(transform.position, Vector3.one * (fallbackRadius * 2f));
            return true;
        }

        /// <summary>FallingPixelManager gọi mỗi lần có 1 dot tì vào trong frame này.</summary>
        public void RegisterPress()
        {
            if (!_broken) _pressing++;
        }

        void Break()
        {
            if (_broken) return;
            _broken = true;

            if (countText != null) countText.gameObject.SetActive(false);

            transform.DOKill(true);
            if (breakPunch > 0f)
                transform.DOPunchScale(Vector3.one * breakPunch, 0.25f, 8, 0.8f);
            if (body != null)
                body.DOFade(0f, fadeOutDuration).SetDelay(0.15f);

            Destroy(gameObject, 0.15f + fadeOutDuration);
        }

        void UpdateVisual()
        {
            // Edit mode: hiển thị đầy đủ để preview.
            int remaining = Application.isPlaying ? Remaining : Mathf.Max(1, requiredDots);

            if (_gridFill != null)
            {
                _gridFill.SetBalancedGrid(requiredDots);
                _gridFill.FillAmount = Mathf.Clamp01(remaining / (float)Mathf.Max(1, requiredDots));
            }

            UpdateCountText(remaining);
        }

        void UpdateCountText(int remaining)
        {
            EnsureCountText();
            if (countText == null) return;
            if (remaining == _lastShownRemaining) return;

            countText.text = remaining.ToString();

            // Pop khi GIÁ TRỊ đổi (bỏ qua lần set đầu để không pop lúc vừa dựng level).
            if (Application.isPlaying && countTextPop > 0f && _lastShownRemaining != int.MinValue)
            {
                countText.transform.DOKill(true);
                countText.transform.DOPunchScale(Vector3.one * countTextPop, countTextPopDuration, 6, 0.7f);
            }
            _lastShownRemaining = remaining;
        }

        // Tự tạo TextMeshPro world-space khi chưa gán trong prefab (chỉ lúc play,
        // tránh đẻ object rác vào scene/prefab trong edit mode).
        void EnsureCountText()
        {
            if (countText != null || !Application.isPlaying) return;

            var go = new GameObject("CountText");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = (Vector3)countTextOffset;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = countTextSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);

            MeshRenderer textRenderer = go.GetComponent<MeshRenderer>();
            if (textRenderer != null && body != null)
            {
                textRenderer.sortingLayerID = body.sortingLayerID;
                textRenderer.sortingOrder = body.sortingOrder + 20;
            }

            countText = tmp;
        }

        void OnValidate()
        {
            requiredDots = Mathf.Max(1, requiredDots);
            if (body == null) body = GetComponent<SpriteRenderer>();
            if (zone == null) zone = GetComponent<Collider2D>();
            if (body != null && _gridFill == null) _gridFill = body.GetComponent<SpriteGridFill>();
            UpdateVisual();
        }
    }
}
