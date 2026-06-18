using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Singleton quản lý mọi dot đang RƠI và đang TRÊN BĂNG CHUYỀN.
    /// - Di chuyển: rơi tự do -> chạy dọc spline -> bị bucket hút.
    /// - Tách dot (separation) bằng SPATIAL GRID (Moore 3x3) để chịu tải tới 500 dot.
    /// - Xoay nhẹ + nhiễu tốc độ để trông tự nhiên.
    /// KHÔNG dùng Rigidbody / collision vật lý giữa các dot — chỉ Transform.
    /// </summary>
    public class FallingPixelManager : MonoBehaviour
    {
        public static FallingPixelManager Instance { get; private set; }

        [Header("Refs")]
        public ConveyorSpline conveyor;

        [Header("Sức chứa & kích thước")]
        [Tooltip("Số dot tối đa xử lý đồng thời. Vượt trần sẽ bỏ bớt dot mới.")]
        public int maxDots = 500;
        [Tooltip("Kích thước 1 dot (world). Dùng cho cell size của grid & khoảng cách tách.")]
        public float dotSize = 0.5f;

        [Header("Rơi tự do")]
        public float gravity = 20f;
        [Tooltip("Cao độ Y miệng băng chuyền (ứng với t=0). Dot rơi tới đây thì lên belt.")]
        public float beltEntryY = -2f;

        [Header("Chạy trên băng chuyền")]
        [Tooltip("Tốc độ chạy dọc spline (world unit/giây).")]
        public float beltSpeed = 2f;
        [Range(0f, 0.9f)] public float speedJitter = 0.2f;
        [Tooltip("Tốc độ xoay tối đa của dot (độ/giây).")]
        public float maxSpin = 90f;

        [Header("Spatial grid / tách dot")]
        [Tooltip("Cell size = dotSize * hệ số này (~1.2). Ô ~ cỡ dot => mỗi ô vài dot.")]
        public float cellSizeMultiplier = 1.2f;
        [Tooltip("Số neighbor tối đa xét cho mỗi dot (cắt sớm để tránh ô quá đông).")]
        public int maxNeighbors = 8;
        [Tooltip("Cường độ lực đẩy tách (world unit/giây).")]
        public float separationStrength = 3f;

        // ---- runtime ----
        readonly List<Dot> _dots = new List<Dot>(512);
        readonly List<Bucket> _buckets = new List<Bucket>(16);

        // Spatial grid: key (ô) -> list index của dot trong _dots.
        readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>(1024);
        // Pool list để tái dùng -> gần như 0 GC alloc mỗi frame.
        readonly Stack<List<int>> _listPool = new Stack<List<int>>(1024);
        // Lực đẩy tách tính trước cho từng dot (song song với _dots).
        readonly List<Vector2> _push = new List<Vector2>(512);

        float _cellSize = 0.6f;
        float _splineLength = 1f;

        public int ActiveCount => _dots.Count;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            _cellSize = Mathf.Max(0.01f, dotSize * cellSizeMultiplier);
            if (conveyor != null) _splineLength = conveyor.GetSplineLength();
        }

        // ---- Đăng ký bucket (Bucket tự gọi khi bật/tắt) ----
        public void RegisterBucket(Bucket b) { if (b != null && !_buckets.Contains(b)) _buckets.Add(b); }
        public void UnregisterBucket(Bucket b) { _buckets.Remove(b); }

        /// <summary>Thêm 1 dot vừa vỡ vào hệ thống (bắt đầu rơi tự do).</summary>
        public void AddDot(Dot d)
        {
            if (d == null) return;
            if (_dots.Count >= maxDots) { Destroy(d.gameObject); return; } // vượt trần -> bỏ

            d.state = DotState.Falling;
            d.fallSpeed = 0f;
            d.targetBucket = null;
            d.markedForRemoval = false;
            d.beltSpeedFactor = 1f + Random.Range(-speedJitter, speedJitter);
            d.spin = Random.Range(-maxSpin, maxSpin);
            d.ApplyColor();
            d.transform.SetParent(transform, true);
            _dots.Add(d);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _dots.Count == 0) return;

            _cellSize = Mathf.Max(0.01f, dotSize * cellSizeMultiplier);
            if (conveyor != null) _splineLength = conveyor.GetSplineLength();

            BuildGrid();          // 1) dựng lưới không gian
            ComputeSeparation();  // 2) tính lực đẩy tách cho từng dot
            MoveDots(dt);         // 3) tích phân chuyển động + áp lực đẩy
            RemoveDead();         // 4) xoá dot ra khỏi màn (for-loop NGƯỢC)
        }

        // ================= SPATIAL GRID =================

        static long CellKey(int x, int y) => ((long)x << 32) | (uint)y;

        void BuildGrid()
        {
            // Trả mọi list về pool rồi clear.
            foreach (var kv in _grid) { kv.Value.Clear(); _listPool.Push(kv.Value); }
            _grid.Clear();

            for (int i = 0; i < _dots.Count; i++)
            {
                Vector3 p = _dots[i].transform.position;
                int cx = Mathf.FloorToInt(p.x / _cellSize);
                int cy = Mathf.FloorToInt(p.y / _cellSize);
                long key = CellKey(cx, cy);
                if (!_grid.TryGetValue(key, out var list))
                {
                    list = _listPool.Count > 0 ? _listPool.Pop() : new List<int>(8);
                    _grid[key] = list;
                }
                list.Add(i);
            }
        }

        void ComputeSeparation()
        {
            // Đảm bảo _push đủ kích thước.
            while (_push.Count < _dots.Count) _push.Add(Vector2.zero);

            float minDist = dotSize; // muốn các dot cách nhau >= dotSize
            for (int i = 0; i < _dots.Count; i++)
            {
                Vector3 p = _dots[i].transform.position;
                int cx = Mathf.FloorToInt(p.x / _cellSize);
                int cy = Mathf.FloorToInt(p.y / _cellSize);

                Vector2 push = Vector2.zero;
                int count = 0;

                // Duyệt 9 ô lân cận (Moore 3x3).
                for (int ox = -1; ox <= 1 && count < maxNeighbors; ox++)
                {
                    for (int oy = -1; oy <= 1 && count < maxNeighbors; oy++)
                    {
                        if (!_grid.TryGetValue(CellKey(cx + ox, cy + oy), out var list)) continue;
                        for (int k = 0; k < list.Count; k++)
                        {
                            int j = list[k];
                            if (j == i) continue;

                            Vector3 q = _dots[j].transform.position;
                            float dx = p.x - q.x, dy = p.y - q.y;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);

                            if (d > 1e-4f && d < minDist)
                            {
                                float w = (minDist - d) / d; // càng gần đẩy càng mạnh
                                push.x += dx * w;
                                push.y += dy * w;
                                count++;
                            }
                            else if (d <= 1e-4f)
                            {
                                // Trùng vị trí -> đẩy theo hướng ngẫu nhiên.
                                push.x += Random.value - 0.5f;
                                push.y += Random.value - 0.5f;
                                count++;
                            }
                            if (count >= maxNeighbors) break;
                        }
                    }
                }
                _push[i] = push;
            }
        }

        // ================= CHUYỂN ĐỘNG =================

        void MoveDots(float dt)
        {
            float half = (conveyor != null) ? conveyor.HalfWidth - dotSize * 0.5f : 0f;

            for (int i = 0; i < _dots.Count; i++)
            {
                Dot d = _dots[i];
                Vector2 sep = _push[i] * (separationStrength * dt);

                switch (d.state)
                {
                    case DotState.Falling:
                        StepFalling(d, sep, dt);
                        break;
                    case DotState.OnBelt:
                        StepOnBelt(d, sep, half, dt);
                        break;
                    case DotState.Attracting:
                        StepAttracting(d, dt);
                        break;
                }
            }
        }

        void StepFalling(Dot d, Vector2 sep, float dt)
        {
            d.fallSpeed += gravity * dt;
            Vector3 pos = d.transform.position;
            pos.y -= d.fallSpeed * dt;
            pos.x += sep.x;
            pos.y += sep.y;
            d.transform.position = pos;
            d.transform.Rotate(0f, 0f, d.spin * dt);

            if (pos.y <= beltEntryY)
            {
                if (conveyor != null)
                {
                    // Lên băng chuyền tại vị trí ngang RANDOM trong bề rộng.
                    d.state = DotState.OnBelt;
                    d.beltProgress = 0f;
                    d.lateralOffset = Random.Range(-conveyor.HalfWidth, conveyor.HalfWidth);
                    d.beltSpeedFactor = 1f + Random.Range(-speedJitter, speedJitter);
                }
                else
                {
                    // Không có băng chuyền -> rơi khỏi màn thì xoá.
                    d.markedForRemoval = true;
                }
            }
        }

        void StepOnBelt(Dot d, Vector2 sep, float half, float dt)
        {
            if (conveyor == null) { d.markedForRemoval = true; return; }

            // Tiến dọc spline theo tốc độ riêng.
            d.beltProgress += (beltSpeed * d.beltSpeedFactor / _splineLength) * dt;

            // Phân tích lực đẩy tách thành: dọc spline (progress) và ngang (lateral).
            Vector3 tan = conveyor.GetTangent(d.beltProgress);
            Vector3 nrm = new Vector3(-tan.y, tan.x, 0f);
            d.beltProgress += Vector2.Dot(sep, (Vector2)tan) / _splineLength;
            d.lateralOffset += Vector2.Dot(sep, (Vector2)nrm);
            d.lateralOffset = Mathf.Clamp(d.lateralOffset, -half, half); // clamp trong bề rộng

            if (d.beltProgress >= 1f) { d.markedForRemoval = true; return; } // hết băng -> xoá

            d.transform.position = conveyor.GetPositionOnSpline(d.beltProgress, d.lateralOffset);
            d.transform.Rotate(0f, 0f, d.spin * dt);

            TryAttract(d);
        }

        void StepAttracting(Dot d, float dt)
        {
            Bucket b = d.targetBucket;
            if (b == null || !b.IsActive) { d.state = DotState.OnBelt; d.targetBucket = null; return; }

            Vector3 mouth = b.MouthPosition;
            d.transform.position = Vector3.MoveTowards(d.transform.position, mouth, b.attractSpeed * dt);
            d.transform.Rotate(0f, 0f, d.spin * 2f * dt);

            if (Vector3.Distance(d.transform.position, mouth) < 0.05f)
            {
                b.AddFill(1);
                if (GameManager.Instance != null) GameManager.Instance.OnDotSorted(d);
                d.markedForRemoval = true;
            }
        }

        // Tìm bucket cùng màu trong tầm hút -> chuyển sang trạng thái Attracting.
        void TryAttract(Dot d)
        {
            for (int i = 0; i < _buckets.Count; i++)
            {
                Bucket b = _buckets[i];
                if (b == null || !b.IsActive || b.colorId != d.colorId) continue;
                if (Vector3.Distance(d.transform.position, b.MouthPosition) <= b.attractRadius)
                {
                    d.state = DotState.Attracting;
                    d.targetBucket = b;
                    return;
                }
            }
        }

        // ================= XOÁ AN TOÀN =================

        void RemoveDead()
        {
            // For-loop NGƯỢC để xoá phần tử khỏi List an toàn (không lệch index).
            for (int i = _dots.Count - 1; i >= 0; i--)
            {
                Dot d = _dots[i];
                if (d == null) { _dots.RemoveAt(i); continue; }
                if (d.markedForRemoval)
                {
                    _dots.RemoveAt(i);
                    Destroy(d.gameObject);
                }
            }
        }
    }
}
