using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace FruitSort
{
    /// <summary>
    /// Switch định tuyến tại CUỐI băng chuyền hở có >= 2 nhánh next (ConveyorConnections):
    /// người chơi click quanh điểm cuối băng để đổi nhánh dot sẽ đi tiếp,
    /// thay vì để FallingPixelManager chia nhánh ngẫu nhiên.
    /// - KHÔNG tốn move (chỉ là quyết định định tuyến), nhưng vẫn notify save tiến trình.
    /// - Input là POLLING (PointerInput) giống Bucket/ModelDotSpawner — không dùng Collider
    ///   (dot/stream không có Rigidbody2D nên project không dùng OnTrigger).
    /// Gắn CHUNG GameObject với ConveyorSpline + ConveyorConnections (LevelBuilder tự thêm
    /// khi ConveyorData.hasSwitch = true).
    /// </summary>
    [RequireComponent(typeof(ConveyorSpline))]
    [RequireComponent(typeof(ConveyorConnections))]
    public class ConveyorSwitch : MonoBehaviour
    {
        [Tooltip("Bán kính vùng click quanh điểm cuối băng (world unit).")]
        [Min(0.1f)] public float clickRadius = 1.1f;
        [Tooltip("Biên nới thêm khi bấm trực tiếp lên conveyor nhánh để đổi route (world unit).")]
        [Min(0f)] public float branchClickPadding = 0.15f;
        [Tooltip("Bỏ qua phần đầu conveyor nhánh sát điểm giao để người chơi không còn switch bằng cách bấm đúng điểm junction.")]
        [Range(0f, 0.5f)] public float branchClickStartProgress = 0.08f;
        [Header("Marker nhánh đang chọn")]
        [Tooltip("Sprite hiển thị trực tiếp trên conveyor đang được link. Để trống sẽ dùng marker tròn mặc định.")]
        public Sprite linkedBranchSprite;
        [Tooltip("Màu marker của nhánh đang chọn.")]
        public Color indicatorColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("Vị trí marker dọc theo conveyor được link (0 = đầu conveyor, 1 = cuối conveyor).")]
        [Range(0.05f, 0.95f)] public float linkedSpriteProgress = 0.35f;
        [Tooltip("Kích thước marker theo world unit.")]
        [Min(0.05f)] public float linkedSpriteSize = 0.45f;
        [Tooltip("Sorting order của marker.")]
        public int linkedSpriteSortingOrder = 60;

        [Header("Nhánh chọn / không chọn")]
        [Tooltip("Độ tối phủ lên nhánh KHÔNG được chọn (0 = không phủ, 1 = đen kịt). " +
                 "Phủ bằng mesh overlay nên không phụ thuộc shader của băng.")]
        [Range(0f, 1f)] public float inactiveDimAlpha = 0.45f;
        [Tooltip("Đẩy nhánh không chọn ra sau theo Z (world) để nằm KHUẤT dưới nhánh đang chọn " +
                 "và băng nguồn tại chỗ giao nhau.")]
        [Min(0f)] public float inactiveBranchZOffset = 0.35f;
        [Tooltip("Hạ sorting order của nhánh inactive để nó luôn nằm sau source và nhánh active.")]
        [Min(1)] public int inactiveBranchSortingOffset = 20;
        [Tooltip("Số knot spline đầu của nhánh active được conveyor gốc render tiếp. 2 = knot 0 và knot 1.")]
        [FormerlySerializedAs("activeBranchOwnedDotCount")]
        [Min(2)] public int activeBranchOwnedKnotCount = 2;

        const string DimOverlayName = "SwitchDimOverlay";

        ConveyorSpline _spline;
        ConveyorConnections _conn;
        SpriteRenderer _indicatorSprite;
        Transform _indicatorRoot;
        Texture2D _runtimeIndicatorTexture;
        Sprite _runtimeIndicatorSprite;
        Material _dimMaterial;
        int _activeIndex;

        /// <summary>Index nhánh đang chọn trong ConveyorConnections.next (tự sanitize).</summary>
        public int ActiveIndex
        {
            get { SanitizeIndex(); return _activeIndex; }
            set { _activeIndex = Mathf.Max(0, value); SanitizeIndex(); RefreshIndicator(); }
        }

        /// <summary>Điểm cuối băng (t=1) — tâm vùng click và gốc mũi tên.</summary>
        public Vector3 SwitchPosition
        {
            get
            {
                if (_spline == null) _spline = GetComponent<ConveyorSpline>();
                if (_spline != null && _spline.TrySampleCenterline(1f, out Vector3 pos, out _))
                    return pos;
                return transform.position;
            }
        }

        /// <summary>Số nhánh next khác null. Switch chỉ có nghĩa khi >= 2.</summary>
        public int ValidBranchCount
        {
            get
            {
                if (_conn == null) _conn = GetComponent<ConveyorConnections>();
                if (_conn == null || _conn.next == null) return 0;
                int n = 0;
                for (int i = 0; i < _conn.next.Count; i++)
                    if (_conn.next[i] != null) n++;
                return n;
            }
        }

        void Awake()
        {
            _spline = GetComponent<ConveyorSpline>();
            _conn = GetComponent<ConveyorConnections>();
        }

        void OnEnable()
        {
            RefreshIndicator();
        }

        void Update()
        {
            // Popup đang đè -> không nhận click gameplay (input là polling, UI không chặn được).
            if (GameplayPause.IsPaused) return;
            if (_spline == null || _conn == null) return;
            // Băng khép kín không bao giờ Advance; < 2 nhánh thì không có gì để đổi.
            if (_spline.IsClosed || ValidBranchCount < 2) return;
            if (!PointerInput.PressedThisFrame()) return;

            // UI đang đè lên -> không cho click xuyên qua xuống switch.
            if (UIPointerGuard.IsPointerOverUI()) return;

            Camera cam = Camera.main;
            if (cam == null) return;
            if (!PointerInput.TryGetPosition(out Vector2 pointerPos)) return;

            Vector3 screen = pointerPos;
            Vector3 pivot = SwitchPosition;
            screen.z = Mathf.Abs(cam.transform.position.z - pivot.z);
            Vector3 world = cam.ScreenToWorldPoint(screen);

            TrySwitchToBranchAtWorldPosition(world);
        }

        /// <summary>Nhánh đang chọn. Trả về false nếu không có nhánh hợp lệ (đích cuối).</summary>
        public bool TryGetActiveNext(out ConveyorSpline next)
        {
            SanitizeIndex();
            var list = _conn != null ? _conn.next : null;
            next = list != null && _activeIndex >= 0 && _activeIndex < list.Count
                ? list[_activeIndex]
                : null;
            return next != null;
        }

        /// <summary>Chuyển sang nhánh hợp lệ kế tiếp (vòng) + feedback + notify save.</summary>
        public void Toggle()
        {
            var list = _conn != null ? _conn.next : null;
            if (list == null || list.Count == 0) return;

            for (int step = 1; step <= list.Count; step++)
            {
                int i = (_activeIndex + step) % list.Count;
                if (list[i] != null) { _activeIndex = i; break; }
            }

            RefreshIndicator();

            if (_indicatorRoot != null)
            {
                _indicatorRoot.DOKill(true); // hoàn tất punch dở để không lệch scale gốc
                _indicatorRoot.localScale = Vector3.one;
                _indicatorRoot.DOPunchScale(Vector3.one * 0.3f, 0.2f, 8, 0.8f);
            }

            // Trạng thái switch nằm trong save tiến trình -> ghi lại ngay sau khi đổi.
            GamePlayManager.NotifyStateChangedForSave();
        }

        /// <summary>
        /// Runtime click behavior: click the target conveyor branch itself, then route to that branch.
        /// Returns false when the click misses all valid next conveyors or the selected branch is already active.
        /// </summary>
        public bool TrySwitchToBranchAtWorldPosition(Vector3 worldPosition)
        {
            if (TryFindBranchIndexAtWorldPosition(worldPosition, out int branchIndex))
                return TrySwitchToBranchIndex(branchIndex);

            return false;
        }

        public bool TryFindBranchIndexAtWorldPosition(Vector3 worldPosition, out int branchIndex)
        {
            branchIndex = -1;
            if (_conn == null) _conn = GetComponent<ConveyorConnections>();
            var list = _conn != null ? _conn.next : null;
            if (list == null || list.Count == 0) return false;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                ConveyorSpline branch = list[i];
                if (branch == null) continue;

                float progress = branch.FindClosestProgress(worldPosition, out float distance);
                if (progress < branchClickStartProgress) continue;

                float hitRadius = Mathf.Max(0.01f, branch.HalfWidth + branchClickPadding);
                if (distance > hitRadius || distance >= bestDistance) continue;

                bestDistance = distance;
                branchIndex = i;
            }

            return branchIndex >= 0;
        }

        public bool TrySwitchToBranchIndex(int branchIndex)
        {
            if (_conn == null) _conn = GetComponent<ConveyorConnections>();
            var list = _conn != null ? _conn.next : null;
            if (list == null || branchIndex < 0 || branchIndex >= list.Count || list[branchIndex] == null)
                return false;

            SanitizeIndex();
            if (_activeIndex == branchIndex) return false;

            _activeIndex = branchIndex;
            RefreshIndicator();
            GamePlayManager.NotifyStateChangedForSave();
            return true;
        }

        /// <summary>
        /// Conveyor gốc sở hữu đoạn từ knot 0 tới knot cuối trong activeBranchOwnedKnotCount.
        /// </summary>
        public float GetActiveBranchOwnedProgress(ConveyorSpline branch)
        {
            if (branch == null) return 0f;

            if (branch.Container == null || branch.Container.Spline == null ||
                branch.Container.Spline.Count < 2)
                return 0f;

            int knotIndex = Mathf.Min(
                Mathf.Max(2, activeBranchOwnedKnotCount) - 1,
                branch.Container.Spline.Count - 1);
            if (knotIndex >= branch.Container.Spline.Count - 1) return 1f;

            return branch.GetProgressThroughKnot(knotIndex);
        }

        void SanitizeIndex()
        {
            if (_conn == null) _conn = GetComponent<ConveyorConnections>();
            var list = _conn != null ? _conn.next : null;
            if (list == null || list.Count == 0) { _activeIndex = 0; return; }

            _activeIndex = Mathf.Clamp(_activeIndex, 0, list.Count - 1);
            if (list[_activeIndex] != null) return;

            // Entry đang trỏ null (băng bị xoá) -> nhích tới entry hợp lệ gần nhất (vòng).
            for (int step = 1; step < list.Count; step++)
            {
                int i = (_activeIndex + step) % list.Count;
                if (list[i] != null) { _activeIndex = i; return; }
            }
        }

        // ---- Sprite marker chỉ nhánh đang chọn ----

        void EnsureIndicator()
        {
            if (_indicatorSprite != null) return;

            var go = new GameObject("SwitchIndicator");
            go.transform.SetParent(transform, false);
            _indicatorRoot = go.transform;

            _indicatorSprite = go.AddComponent<SpriteRenderer>();
            _indicatorSprite.sprite = linkedBranchSprite != null
                ? linkedBranchSprite
                : CreateFallbackIndicatorSprite();
            _indicatorSprite.color = indicatorColor;
            _indicatorSprite.sortingOrder = linkedSpriteSortingOrder;
        }

        void RefreshIndicator()
        {
            if (!Application.isPlaying) return;
            EnsureIndicator();
            RefreshBranchVisuals();
            // Connector liên tục trim cả cuối source và đầu target. Khi đổi route phải rebuild
            // source + toàn bộ target: target cũ trả lại phần đầu, target mới được trim để nhận cung nối.
            RebuildConnectionMeshes();
            if (_indicatorSprite == null) return;

            ConveyorSpline next = null;
            bool show = _spline != null && !_spline.IsClosed &&
                        ValidBranchCount >= 2 && TryGetActiveNext(out next);
            _indicatorSprite.enabled = show;
            if (!show) return;

            _indicatorRoot.SetParent(next.transform, true);
            _indicatorRoot.position = next.GetPositionOnSpline(linkedSpriteProgress, 0f);
            _indicatorRoot.rotation = Quaternion.identity;
            _indicatorSprite.sprite = linkedBranchSprite != null
                ? linkedBranchSprite
                : CreateFallbackIndicatorSprite();
            _indicatorSprite.color = indicatorColor;
            _indicatorSprite.sortingLayerID = next.GetComponent<MeshRenderer>()?.sortingLayerID ?? 0;
            _indicatorSprite.sortingOrder = linkedSpriteSortingOrder;

            Vector2 spriteSize = _indicatorSprite.sprite != null
                ? _indicatorSprite.sprite.bounds.size
                : Vector2.one;
            float largestSide = Mathf.Max(0.01f, spriteSize.x, spriteSize.y);
            _indicatorRoot.localScale = Vector3.one * (linkedSpriteSize / largestSide);
        }


        void RebuildConnectionMeshes()
        {
            ConveyorBeltRenderer sourceRenderer = GetComponent<ConveyorBeltRenderer>();
            if (sourceRenderer != null) sourceRenderer.RebuildMeshAndMaterials();

            if (_conn == null || _conn.next == null) return;
            var rebuilt = new HashSet<ConveyorBeltRenderer>();
            for (int i = 0; i < _conn.next.Count; i++)
            {
                ConveyorSpline target = _conn.next[i];
                if (target == null) continue;
                ConveyorBeltRenderer targetRenderer = target.GetComponent<ConveyorBeltRenderer>();
                if (targetRenderer == null || !rebuilt.Add(targetRenderer)) continue;
                targetRenderer.RebuildMeshAndMaterials();
            }
        }

        /// <summary>
        /// Nhánh KHÔNG được chọn: phủ lớp tối + đẩy lùi ra sau (Z) để nằm khuất dưới nhánh
        /// đang chọn tại chỗ giao nhau; nhánh được chọn: sáng, cùng mặt phẳng với băng nguồn.
        /// Phủ tối bằng MESH OVERLAY (share mesh của băng, material Sprites/Default màu đen
        /// bán trong suốt) vì shader băng (Mobile/Particles) không có property màu để tint.
        /// Overlay share mesh THEO THAM CHIẾU nên tự khớp khi ConveyorBeltRenderer rebuild.
        /// </summary>
        void RefreshBranchVisuals()
        {
            if (_conn == null) _conn = GetComponent<ConveyorConnections>();
            if (_conn == null || _conn.next == null) return;

            TryGetActiveNext(out ConveyorSpline active);
            MeshRenderer sourceMeshRenderer = GetComponent<MeshRenderer>();
            int sourceSortingLayer = sourceMeshRenderer != null
                ? sourceMeshRenderer.sortingLayerID
                : 0;
            int sourceSortingOrder = sourceMeshRenderer != null
                ? sourceMeshRenderer.sortingOrder
                : 0;
            int sortingOffset = Mathf.Max(1, inactiveBranchSortingOffset);

            for (int i = 0; i < _conn.next.Count; i++)
            {
                ConveyorSpline belt = _conn.next[i];
                if (belt == null) continue;
                bool isActive = belt == active;

                ConveyorBeltRenderer beltRenderer = belt.GetComponent<ConveyorBeltRenderer>();
                if (beltRenderer != null)
                {
                    float visualZ = isActive ? 0f : -Mathf.Max(0f, inactiveBranchZOffset);
                    beltRenderer.SetRuntimeVisualZOffset(visualZ, false);
                }

                MeshRenderer branchMeshRenderer = belt.GetComponent<MeshRenderer>();
                if (branchMeshRenderer != null)
                {
                    branchMeshRenderer.sortingLayerID = sourceSortingLayer;
                    branchMeshRenderer.sortingOrder = isActive
                        ? sourceSortingOrder
                        : sourceSortingOrder - sortingOffset;
                }

                Transform overlay = belt.transform.Find(DimOverlayName);
                if (!isActive && overlay == null) overlay = CreateDimOverlay(belt);
                if (overlay != null)
                {
                    overlay.gameObject.SetActive(!isActive);
                    MeshRenderer overlayRenderer = overlay.GetComponent<MeshRenderer>();
                    if (overlayRenderer != null)
                    {
                        overlayRenderer.sortingLayerID = sourceSortingLayer;
                        overlayRenderer.sortingOrder = sourceSortingOrder - sortingOffset + 1;
                    }
                }
            }
        }

        Sprite CreateFallbackIndicatorSprite()
        {
            if (_runtimeIndicatorSprite != null) return _runtimeIndicatorSprite;

            const int size = 32;
            _runtimeIndicatorTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SwitchIndicatorFallbackTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.44f;
            float radiusSquared = radius * radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    pixels[y * size + x] = dx * dx + dy * dy <= radiusSquared
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            _runtimeIndicatorTexture.SetPixels32(pixels);
            _runtimeIndicatorTexture.Apply(false, true);
            _runtimeIndicatorSprite = Sprite.Create(
                _runtimeIndicatorTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            _runtimeIndicatorSprite.name = "SwitchIndicatorFallbackSprite";
            _runtimeIndicatorSprite.hideFlags = HideFlags.HideAndDontSave;
            return _runtimeIndicatorSprite;
        }

        Transform CreateDimOverlay(ConveyorSpline belt)
        {
            MeshFilter sourceFilter = belt.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null) return null;

            var go = new GameObject(DimOverlayName);
            go.transform.SetParent(belt.transform, false);
            // Nhích lên trước mặt băng CỦA CHÍNH NÓ một chút (vẫn sau nhánh active vì cả
            // object nhánh inactive đã bị đẩy lùi inactiveBranchZOffset).
            go.transform.localPosition = new Vector3(0f, 0f, -0.02f);

            var overlayFilter = go.AddComponent<MeshFilter>();
            overlayFilter.sharedMesh = sourceFilter.sharedMesh;

            if (_dimMaterial == null)
            {
                _dimMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    color = new Color(0f, 0f, 0f, Mathf.Clamp01(inactiveDimAlpha))
                };
            }

            var overlayRenderer = go.AddComponent<MeshRenderer>();
            int subMeshCount = Mathf.Max(1, sourceFilter.sharedMesh.subMeshCount);
            var mats = new Material[subMeshCount];
            for (int i = 0; i < subMeshCount; i++) mats[i] = _dimMaterial;
            overlayRenderer.sharedMaterials = mats;
            overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            return go.transform;
        }

        void OnDestroy()
        {
            if (_indicatorRoot != null)
            {
                _indicatorRoot.gameObject.SetActive(false);
                DestroyRuntimeIndicatorAsset(_indicatorRoot.gameObject);
            }
            if (_dimMaterial != null) Destroy(_dimMaterial);
            DestroyRuntimeIndicatorAsset(_runtimeIndicatorSprite);
            DestroyRuntimeIndicatorAsset(_runtimeIndicatorTexture);
        }

        static void DestroyRuntimeIndicatorAsset(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = indicatorColor;
            Gizmos.DrawWireSphere(SwitchPosition, clickRadius);
        }
    }
}
