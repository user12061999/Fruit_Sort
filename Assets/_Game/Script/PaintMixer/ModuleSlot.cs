using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Slot cố định trên băng chuyền. Người chơi click vào để mở popup chọn module.
    /// Khi có module, ủy quyền xử lý cho subclass (BlenderSlot, MixerSlot, ...).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class ModuleSlot : MonoBehaviour
    {
        [Header("Module hiện tại")]
        public ModuleData currentModule;

        [Header("Visual")]
        [Tooltip("Màu slot khi trống.")]
        public Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [Tooltip("Màu slot khi đang xử lý.")]
        public Color activeColor = Color.yellow;

        [Header("Refs")]
        [Tooltip("Camera dùng để chuyển tọa độ chuột. Để trống = Camera.main.")]
        public Camera cam;
        [Tooltip("PaintStream prefab để tạo mới (gán tại Inspector).")]
        public PaintStream paintStreamPrefab;
        [Tooltip("Băng chuyền để gán cho PaintStream mới tạo.")]
        public ConveyorSpline conveyor;

        [Tooltip("Bán kính dò PaintStream đi qua slot (vì dot/stream không có Rigidbody2D nên " +
                 "không dùng được OnTriggerEnter2D — phải poll bằng Physics2D query).")]
        public float streamDetectRadius = 0.55f;

        protected SpriteRenderer _sr;
        Collider2D _col;
        bool _wasPressed;

        // Tập PaintStream đang nằm trong vùng slot (để phát hiện enter/exit khi poll).
        readonly HashSet<PaintStream> _inside = new HashSet<PaintStream>();
        readonly List<PaintStream> _toExit = new List<PaintStream>(8);

        protected virtual void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<Collider2D>();
            _col.isTrigger = true;
            if (cam == null) cam = Camera.main;
        }

        protected virtual void Start()
        {
            RefreshSlotVisual();
        }

        void Update()
        {
            HandleClickInput();
            PollStreams();
            OnUpdate();
        }

        /// <summary>
        /// Phát hiện PaintStream đi vào/ra vùng slot bằng Physics2D query (thay cho OnTriggerEnter2D,
        /// vốn không kích hoạt vì stream/slot không có Rigidbody2D). Tự gọi OnStreamEnter/OnStreamExit.
        /// </summary>
        void PollStreams()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, streamDetectRadius);

            // Enter: stream trong vùng mà chưa được ghi nhận.
            foreach (var h in hits)
            {
                var ps = h.GetComponent<PaintStream>();
                if (ps == null) continue;
                if (_inside.Add(ps))
                    OnStreamEnter(ps);
            }

            // Exit: stream đã ghi nhận nhưng không còn trong vùng (hoặc đã bị hủy).
            _toExit.Clear();
            foreach (var ps in _inside)
            {
                if (ps == null) { _toExit.Add(ps); continue; }
                float d = Vector2.Distance(transform.position, ps.transform.position);
                if (d > streamDetectRadius + ps.baseRadius)
                    _toExit.Add(ps);
            }
            for (int i = 0; i < _toExit.Count; i++)
            {
                var ps = _toExit[i];
                _inside.Remove(ps);
                if (ps != null) OnStreamExit(ps);
            }
        }

        void HandleClickInput()
        {
            if (Mouse.current == null) return;
            bool pressed = Mouse.current.leftButton.isPressed;
            if (pressed && !_wasPressed)
            {
                Vector3 screenPos = Mouse.current.position.ReadValue();
                if (IsHit(screenPos)) OnSlotClicked();
            }
            _wasPressed = pressed;
        }

        bool IsHit(Vector3 screenPos)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            return _col.bounds.Contains(world);
        }

        /// <summary>Gọi khi người chơi click vào slot. Override để mở popup UI riêng.</summary>
        protected virtual void OnSlotClicked()
        {
            // Mặc định: toggle active visual feedback
            _sr.DOKill();
            _sr.DOColor(activeColor, 0.15f).OnComplete(RefreshSlotVisual);
        }

        /// <summary>Cập nhật màu sắc slot theo module hiện tại.</summary>
        protected void RefreshSlotVisual()
        {
            if (_sr == null) return;
            _sr.color = currentModule != null ? currentModule.slotColor : emptyColor;
        }

        /// <summary>Đặt module mới vào slot.</summary>
        public virtual void SetModule(ModuleData module)
        {
            currentModule = module;
            RefreshSlotVisual();

            // Bounce animation khi đặt module
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.5f);
        }

        /// <summary>Tạo PaintStream mới tại vị trí slot với progress hiện tại.</summary>
        protected PaintStream SpawnPaintStream(Vector3 cmy, float vol)
        {
            if (paintStreamPrefab == null || conveyor == null) return null;
            float t = conveyor.FindClosestProgress(transform.position, out _);
            PaintStream ps = Instantiate(paintStreamPrefab, transform.position, Quaternion.identity);
            ps.cmy = cmy;
            ps.volume = vol;
            ps.conveyor = conveyor;
            ps.progress = t + 0.01f; // đặt ngay sau slot
            ps.RefreshVisual();
            return ps;
        }

        /// <summary>Tìm PaintStream gần slot nhất trong bán kính cho trước.</summary>
        protected PaintStream FindNearbyStream(float radius = 1f)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            PaintStream best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                var ps = h.GetComponent<PaintStream>();
                if (ps == null || ps.IsFrozen || ps.IsMerging) continue;
                float d = Vector3.Distance(transform.position, ps.transform.position);
                if (d < bestDist) { bestDist = d; best = ps; }
            }
            return best;
        }

        /// <summary>Hook cho subclass cập nhật mỗi frame.</summary>
        protected virtual void OnUpdate() { }

        protected virtual void OnStreamEnter(PaintStream stream) { }
        protected virtual void OnStreamExit(PaintStream stream) { }
    }
}
