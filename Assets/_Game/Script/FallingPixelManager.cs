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
        [Tooltip("Băng chuyền mặc định (dùng cho Falling và Approaching). Để trống = tự tìm băng đầu tiên.")]
        public ConveyorSpline conveyor;

        [Header("Sức chứa & kích thước")]
        [Tooltip("Số dot tối đa xử lý đồng thời. Vượt trần sẽ bỏ bớt dot mới. <= 0 = KHÔNG giới hạn.")]
        public int maxDots = 0;
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

        [Header("Bay thẳng vào băng chuyền (spawn từ model 3D)")]
        [Tooltip("Tốc độ bay thẳng từ điểm spawn tới miệng băng chuyền (world unit/giây).")]
        public float approachSpeed = 8f;
        [Tooltip("Vị trí trên spline để các dot bay vào (0 = đầu băng, 1 = cuối băng).")]
        [Range(0f, 1f)] public float spawnEntryProgress = 0f;

        [Header("Spatial grid / tách dot")]
        [Tooltip("Cell size = dotSize * hệ số này (~1.2). Ô ~ cỡ dot => mỗi ô vài dot.")]
        public float cellSizeMultiplier = 1.2f;
        [Tooltip("Số neighbor tối đa xét cho mỗi dot (cắt sớm để tránh ô quá đông).")]
        public int maxNeighbors = 8;
        [Tooltip("Cường độ lực đẩy tách (world unit/giây).")]
        public float separationStrength = 1f;

        // ---- runtime ----
        readonly List<Dot> _dots = new List<Dot>(512);
        readonly List<Bucket> _buckets = new List<Bucket>(16);
        readonly List<ConveyorSpline> _allConveyors = new List<ConveyorSpline>(8);

        // Spatial grid: key (ô) -> list index của dot trong _dots.
        readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>(1024);
        // Pool list để tái dùng -> gần như 0 GC alloc mỗi frame.
        readonly Stack<List<int>> _listPool = new Stack<List<int>>(1024);
        // Lực đẩy tách tính trước cho từng dot (song song với _dots).
        readonly List<Vector2> _push = new List<Vector2>(512);

        // CACHE vị trí để tránh đọc/ghi Transform.position nhiều lần mỗi frame.
        // Transform access trong Unity rất chậm khi có hàng trăm dot.
        Vector3[] _posCache = new Vector3[512];

        float _cellSize = 0.6f;

        public int ActiveCount => _dots.Count;

        /// <summary>Danh sách dot đang hoạt động (chỉ đọc). Dùng cho module poll dot trên băng (vd BlenderSlot).</summary>
        public IReadOnlyList<Dot> ActiveDots => _dots;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void Start()
        {
            _cellSize = Mathf.Max(0.01f, dotSize * cellSizeMultiplier);
            // Đăng ký tất cả băng chuyền đã có trong scene
            foreach (var c in FindObjectsByType<ConveyorSpline>(FindObjectsSortMode.None))
                RegisterConveyor(c);
            if (conveyor == null && _allConveyors.Count > 0)
                conveyor = _allConveyors[0];
        }

        // ---- Đăng ký bucket (Bucket tự gọi khi bật/tắt) ----
        public void RegisterBucket(Bucket b) { if (b != null && !_buckets.Contains(b)) _buckets.Add(b); }
        public void UnregisterBucket(Bucket b) { _buckets.Remove(b); }

        // ---- Đăng ký băng chuyền (ConveyorSpline tự gọi khi bật/tắt lúc play) ----
        public void RegisterConveyor(ConveyorSpline c)
        {
            if (c != null && !_allConveyors.Contains(c)) _allConveyors.Add(c);
        }
        public void UnregisterConveyor(ConveyorSpline c) { _allConveyors.Remove(c); }

        /// <summary>Thêm 1 dot vừa vỡ vào hệ thống (bắt đầu rơi tự do).</summary>
        public void AddDot(Dot d)
        {
            if (d == null) return;
            if (maxDots > 0 && _dots.Count >= maxDots) { Destroy(d.gameObject); return; } // vượt trần -> bỏ

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

        /// <summary>
        /// Thêm 1 dot phóng theo hướng cố định. Dot tự phát hiện và lên băng chuyền đầu tiên nó chạm.
        /// Dùng cho ModelDotSpawner với useDirectionalLaunch = true.
        /// </summary>
        public void AddDotLaunched(Dot d, Vector2 velocity)
        {
            if (d == null) return;
            if (maxDots > 0 && _dots.Count >= maxDots) { Destroy(d.gameObject); return; }

            d.state = DotState.Launched;
            d.launchVelocity = velocity;
            d.fallSpeed = 0f;
            d.targetBucket = null;
            d.markedForRemoval = false;
            d.beltSpeedFactor = 1f + Random.Range(-speedJitter, speedJitter);
            d.spin = Random.Range(-maxSpin, maxSpin);
            d.ApplyColor();
            d.transform.SetParent(transform, true);
            _dots.Add(d);
        }

        /// <summary>
        /// Thêm 1 dot bay THẲNG từ vị trí hiện tại tới miệng băng chuyền rồi chạy như bình thường.
        /// Dùng cho spawn từ model 3D (ModelDotSpawner).
        /// </summary>
        /// <param name="entryProgress">Progress (t, 0..1) trên spline để vào belt. NaN = dùng spawnEntryProgress.</param>
        /// <param name="lateral">Lệch ngang khi vào belt. NaN = random trong bề rộng.</param>
        public void AddDotApproaching(Dot d, float entryProgress = float.NaN, float lateral = float.NaN)
        {
            if (d == null) return;
            if (maxDots > 0 && _dots.Count >= maxDots) { Destroy(d.gameObject); return; } // vượt trần -> bỏ

            float t = float.IsNaN(entryProgress) ? spawnEntryProgress : Mathf.Clamp01(entryProgress);
            float lat = float.IsNaN(lateral)
                ? ((conveyor != null) ? Random.Range(-conveyor.HalfWidth, conveyor.HalfWidth) : 0f)
                : lateral;

            d.state = DotState.Approaching;
            d.fallSpeed = 0f;
            d.targetBucket = null;
            d.markedForRemoval = false;
            d.beltEntryProgress = t;
            d.entryLateral = lat;
            d.approachTarget = (conveyor != null)
                ? conveyor.GetPositionOnSpline(t, lat)
                : d.transform.position;
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

            // Đảm bảo cache đủ kích thước.
            int n = _dots.Count;
            if (_posCache.Length < n) _posCache = new Vector3[Mathf.Max(n, _posCache.Length * 2)];

            // Đọc TẤT CẢ vị trí 1 lần duy nhất trong frame.
            for (int i = 0; i < n; i++)
                _posCache[i] = _dots[i].transform.position;

            bool needSep = separationStrength > 0f && n > 1;

            if (needSep)
            {
                BuildGrid();          // 1) dựng lưới không gian
                ComputeSeparation();  // 2) tính lực đẩy tách cho từng dot
            }
            else
            {
                // Zero push khi không cần separation.
                while (_push.Count < n) _push.Add(Vector2.zero);
                for (int i = 0; i < n; i++) _push[i] = Vector2.zero;
            }

            MoveDots(dt, n);  // 3) tích phân chuyển động + áp lực đẩy

            // Ghi vị trí đã cập nhật trở lại Transform (1 lần/dot).
            for (int i = 0; i < n; i++)
            {
                // Dot đã được bucket nhận nuôi do DOTween điều khiển -> không ghi đè vị trí.
                if (_dots[i] != null && !_dots[i].capturedByBucket)
                    _dots[i].transform.position = _posCache[i];
            }

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
                Vector3 p = _posCache[i]; // dùng cache, KHÔNG đọc Transform
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
                Vector3 p = _posCache[i]; // dùng cache
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

                            Vector3 q = _posCache[j]; // dùng cache
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

        void MoveDots(float dt, int n)
        {
            for (int i = 0; i < n; i++)
            {
                Dot d = _dots[i];
                if (d == null) continue;
                Vector2 sep = _push[i] * (separationStrength * dt);

                switch (d.state)
                {
                    case DotState.Launched:
                        StepLaunched(d, sep, dt, i);
                        break;
                    case DotState.Approaching:
                        StepApproaching(d, sep, dt, i);
                        break;
                    case DotState.Falling:
                        StepFalling(d, sep, dt, i);
                        break;
                    case DotState.OnBelt:
                        StepOnBelt(d, sep, dt, i);
                        break;
                    case DotState.Attracting:
                        StepAttracting(d, dt, i);
                        break;
                }
            }
        }

        // Phóng theo hướng cố định; khi chạm bất kỳ băng chuyền nào thì lên belt đó.
        void StepLaunched(Dot d, Vector2 sep, float dt, int idx)
        {
            Vector3 pos = _posCache[idx];
            pos.x += d.launchVelocity.x * dt + sep.x;
            pos.y += d.launchVelocity.y * dt + sep.y;
            d.transform.Rotate(0f, 0f, d.spin * dt);

            // Kiểm tra va chạm với từng băng chuyền đã đăng ký.
            float threshold = dotSize * 0.5f;
            for (int i = 0; i < _allConveyors.Count; i++)
            {
                var belt = _allConveyors[i];
                if (belt == null || !belt.isActiveAndEnabled) continue;

                float t = belt.FindClosestProgress(pos, out float dist);
                if (dist > belt.HalfWidth + threshold) continue;

                // Tính lateral offset thực tế (dương/âm theo pháp tuyến).
                belt.TrySampleCenterline(t, out Vector3 center, out Vector3 tan);
                Vector3 nrm = new Vector3(-tan.y, tan.x, 0f);
                float lat = Mathf.Clamp(Vector3.Dot(pos - center, nrm),
                                        -belt.HalfWidth, belt.HalfWidth);

                d.state = DotState.OnBelt;
                d.conveyor = belt;
                d.beltProgress = t;
                d.lateralOffset = lat;
                d.beltSpeedFactor = 1f + Random.Range(-speedJitter, speedJitter);
                _posCache[idx] = pos;
                return;
            }

            // Xoá nếu bay quá xa màn hình.
            if (pos.y < beltEntryY - 10f || Mathf.Abs(pos.x) > 30f || pos.y > 30f)
                d.markedForRemoval = true;

            _posCache[idx] = pos;
        }

        // Bay thẳng tới đích trên spline; tới nơi thì chuyển sang OnBelt.
        void StepApproaching(Dot d, Vector2 sep, float dt, int idx)
        {
            // Đích có thể đổi nếu spline di chuyển -> cập nhật lại cho an toàn.
            if (conveyor != null)
                d.approachTarget = conveyor.GetPositionOnSpline(d.beltEntryProgress, d.entryLateral);

            Vector3 pos = _posCache[idx]; // dùng cache
            Vector3 moved = Vector3.MoveTowards(pos, d.approachTarget, approachSpeed * dt);
            float dMoved = Vector3.Distance(moved, d.approachTarget);

            // Tách nhẹ để các dot không chồng khít khi bay theo bầy, NHƯNG không cho tách đẩy
            // dot ra XA target hơn 'moved'. Nhờ vậy khoảng cách tới đích giảm đều mỗi frame
            // (luôn tiến tới và CHẮC CHẮN tới nơi) -> không còn dot kẹt lởn vởn ở cửa vào.
            Vector3 withSep = moved + new Vector3(sep.x, sep.y, 0f);
            if (dMoved > 1e-4f && Vector3.Distance(withSep, d.approachTarget) > dMoved)
                withSep = d.approachTarget + (withSep - d.approachTarget).normalized * dMoved;

            _posCache[idx] = withSep; // ghi cache, KHÔNG ghi Transform ngay
            d.transform.Rotate(0f, 0f, d.spin * dt);

            if (dMoved <= dotSize * 0.5f)
            {
                if (conveyor != null)
                {
                    d.state = DotState.OnBelt;
                    d.conveyor = conveyor;
                    d.beltProgress = d.beltEntryProgress;
                    d.lateralOffset = d.entryLateral;
                    d.beltSpeedFactor = 1f + Random.Range(-speedJitter, speedJitter);
                }
                else
                {
                    d.markedForRemoval = true;
                }
            }
        }

        void StepFalling(Dot d, Vector2 sep, float dt, int idx)
        {
            d.fallSpeed += gravity * dt;
            Vector3 pos = _posCache[idx]; // dùng cache
            pos.y -= d.fallSpeed * dt;
            pos.x += sep.x;
            pos.y += sep.y;
            _posCache[idx] = pos; // ghi cache
            d.transform.Rotate(0f, 0f, d.spin * dt);

            if (pos.y <= beltEntryY)
            {
                if (conveyor != null)
                {
                    // Lên băng chuyền tại vị trí ngang RANDOM trong bề rộng.
                    d.state = DotState.OnBelt;
                    d.conveyor = conveyor;
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

        void StepOnBelt(Dot d, Vector2 sep, float dt, int idx)
        {
            if (d.conveyor == null) d.conveyor = conveyor;
            if (d.conveyor == null) { d.markedForRemoval = true; return; }

            float length = Mathf.Max(0.01f, d.conveyor.GetSplineLength());
            float half = d.conveyor.HalfWidth - dotSize * 0.5f;

            // Tiến dọc spline theo tốc độ riêng.
            d.beltProgress += (beltSpeed * d.beltSpeedFactor / length) * dt;
            if (d.beltProgress >= 1f)
            {
                if (d.conveyor.IsClosed)
                {
                    // Spline khép vòng → quay lại đầu thay vì xoá.
                    d.beltProgress -= 1f;
                }
                else if (!AdvanceToNext(d))
                {
                    d.markedForRemoval = true;
                }
                return;
            }

            // MỘT lần lấy mẫu spline (LUT): vị trí tâm + tiếp tuyến tại progress hiện tại.
            // Thay cho 2 lần Container.Evaluate trước đây (GetTangent + GetPositionOnSpline).
            if (!d.conveyor.TrySampleCenterline(d.beltProgress, out Vector3 center, out Vector3 tan))
            { d.markedForRemoval = true; return; }
            Vector3 nrm = new Vector3(-tan.y, tan.x, 0f);

            // Phân tích lực đẩy tách thành: dọc spline (theo tiếp tuyến) và ngang (theo pháp tuyến).
            // CHẶN tách dọc đẩy NGƯỢC chiều đi (along < 0): nếu cho âm, dot bị các dot phía trước
            // đẩy lùi -> beltProgress gần như đứng yên -> "di chuyển rất chậm" khi đông. Chỉ cho
            // tách dọc đẩy TIẾN (along > 0, giúp giãn đám về phía trước); tách NGANG giữ nguyên.
            // GIỚI HẠN along để separation không vượt quá belt movement -> tránh dot nhảy loạn khi đông.
            float maxSep = beltSpeed * dt * 0.5f; // tối đa 50% quãng đường belt/frame
            float along = Mathf.Clamp(Vector2.Dot(sep, (Vector2)tan), 0f, maxSep);
            d.beltProgress += along / length;                        // ghi nhận vào progress
            float lateralSep = Mathf.Clamp(Vector2.Dot(sep, (Vector2)nrm), -maxSep, maxSep);
            d.lateralOffset += lateralSep;
            d.lateralOffset = Mathf.Clamp(d.lateralOffset, -half, half); // clamp trong bề rộng

            // Vị trí = tâm + lệch ngang + nhích dọc (xấp xỉ bậc 1 — sep mỗi frame rất nhỏ nên đủ mượt).
            _posCache[idx] = center + nrm * d.lateralOffset + tan * along; // ghi cache
            d.transform.Rotate(0f, 0f, d.spin * dt);

            TryAttract(d, idx);
        }

        /// <summary>
        /// Khi dot chạy hết băng hiện tại: chuyển sang băng được nối qua <see cref="ConveyorConnections"/>.
        /// Nhiều nhánh (splitter) -> chọn ngẫu nhiên. Trả về false nếu không có băng kế (đích cuối).
        /// </summary>
        bool AdvanceToNext(Dot d)
        {
            if (d.conveyor == null) return false;
            var conn = d.conveyor.GetComponent<ConveyorConnections>();
            if (conn == null || conn.next == null || conn.next.Count == 0) return false;

            // Lọc nhánh hợp lệ rồi chọn ngẫu nhiên.
            ConveyorSpline pick = null;
            int valid = 0;
            for (int i = 0; i < conn.next.Count; i++)
            {
                var nx = conn.next[i];
                if (nx == null) continue;
                valid++;
                if (Random.Range(0, valid) == 0) pick = nx; // reservoir sampling -> phân bố đều
            }
            if (pick == null) return false;

            d.conveyor = pick;
            d.beltProgress = 0f;
            d.lateralOffset = Mathf.Clamp(d.lateralOffset, -pick.HalfWidth, pick.HalfWidth);
            d.beltSpeedFactor = 1f + Random.Range(-speedJitter, speedJitter);
            return true;
        }

        void StepAttracting(Dot d, float dt, int idx)
        {
            Bucket b = d.targetBucket;
            if (b == null || !b.IsActive) { d.state = DotState.OnBelt; d.targetBucket = null; return; }

            Vector3 mouth = b.MouthPosition;
            _posCache[idx] = Vector3.MoveTowards(_posCache[idx], mouth, b.attractSpeed * dt);
            d.transform.Rotate(0f, 0f, d.spin * 2f * dt);

            if (Vector3.Distance(_posCache[idx], mouth) < 0.05f)
            {
                // Ghi vị trí cache cuối cùng vào Transform trước khi bucket nhận nuôi (DOTween sẽ
                // điều khiển từ đây) — nếu không, vòng write-back sau MoveDots vẫn dùng vị trí cũ.
                d.transform.position = _posCache[idx];
                b.ReceiveDot(d);             // dot bay vào & nằm trong giỏ (KHÔNG destroy)
                if (GameManager.Instance != null) GameManager.Instance.OnDotSorted(d);
                d.capturedByBucket = true;
                d.markedForRemoval = true;
            }
        }

        // Tìm bucket cùng màu trong tầm hút -> chuyển sang trạng thái Attracting.
        void TryAttract(Dot d, int idx)
        {
            for (int i = 0; i < _buckets.Count; i++)
            {
                Bucket b = _buckets[i];
                if (b == null || !b.IsActive || b.colorId != d.colorId) continue;
                // Phát hiện theo VÙNG VA CHẠM (Collider2D) của bucket, fallback bán kính nếu chưa gán zone.
                if (b.Contains(_posCache[idx])) // dùng cache
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
                    // Đã vào giỏ -> bucket sở hữu, KHÔNG destroy (sẽ đi theo giỏ khi worker mang đi).
                    if (!d.capturedByBucket)
                        Destroy(d.gameObject);
                }
            }
        }
    }
}
