using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

namespace FruitSort
{
    /// <summary>
    /// Chọn nhánh active của conveyor.
    ///
    /// Khi nhánh thay đổi, component tạo một conveyor hiển thị tạm bằng cách ghép:
    /// - toàn bộ knot của conveyor nguồn;
    /// - các knot của conveyor nhánh đang active.
    ///
    /// Renderer hợp nhất chỉ dùng cho hình ảnh. Movement vẫn chạy trên hai ConveyorSpline gốc,
    /// vì vậy không thay đổi logic FallingPixelManager hoặc graph ConveyorConnections.
    /// </summary>
    [RequireComponent(typeof(ConveyorSpline))]
    [RequireComponent(typeof(ConveyorConnections))]
    public sealed class ConveyorSwitch : MonoBehaviour
    {
        const string CombinedRendererObjectName = "SwitchCombinedConveyorRenderer";

        [Header("Input")]
        [Min(0.1f)] public float clickRadius = 1.1f;
        [Min(0f)] public float branchClickPadding = 0.15f;
        [Range(0f, 0.5f)] public float branchClickStartProgress = 0.08f;

        [Header("Active branch marker")]
        public Sprite linkedBranchSprite;
        public Color indicatorColor = new Color(1f, 0.85f, 0.2f);
        [Range(0.05f, 0.95f)] public float linkedSpriteProgress = 0.35f;
        [Min(0.05f)] public float linkedSpriteSize = 0.45f;
        public int linkedSpriteSortingOrder = 60;

        [Header("Combined conveyor renderer")]
        [Tooltip("Tạo một renderer mới từ knot của conveyor nguồn và nhánh đang chọn.")]
        public bool useCombinedRenderer = true;

        [Tooltip("Số knot lấy từ conveyor nhánh. 0 = lấy toàn bộ knot.")]
        [Min(0)] public int linkedKnotCount;

        [Tooltip("Ẩn MeshRenderer của conveyor nguồn khi renderer hợp nhất đang hoạt động.")]
        public bool hideSourceRenderer = true;

        [Tooltip("Lệch Z cộng thêm cho renderer hợp nhất. Giá trị âm thường đưa mesh gần camera hơn trong scene 2D.")]
        public float combinedRendererZOffset = -0.002f;

        [Tooltip("Khoảng cách tối đa để coi knot cuối nguồn và knot đầu nhánh là cùng một knot.")]
        [Min(0.0001f)] public float duplicateKnotTolerance = 0.01f;

        [Tooltip("Sorting order cộng thêm so với renderer nguồn.")]
        public int combinedSortingOrderOffset = 1;

        [Header("Legacy visual settings")]
        [Tooltip("Giữ lại để prefab cũ không mất dữ liệu. Bản Simple không tạo dim overlay.")]
        [Range(0f, 1f)] public float inactiveDimAlpha = 0.45f;
        [Tooltip("Giữ lại để tương thích prefab cũ. Bản Simple không đổi Z của branch.")]
        [Min(0f)] public float inactiveBranchZOffset = 0.35f;
        [Tooltip("Giữ lại để tương thích prefab cũ. Bản Simple không đổi sorting order.")]
        [Min(1)] public int inactiveBranchSortingOffset = 20;
        [FormerlySerializedAs("activeBranchOwnedDotCount")]
        [Min(2)] public int activeBranchOwnedKnotCount = 2;

        ConveyorSpline _spline;
        ConveyorConnections _connections;
        ConveyorBeltRenderer _sourceBeltRenderer;
        MeshRenderer _sourceMeshRenderer;

        SpriteRenderer _indicator;
        Sprite _fallbackSprite;
        int _activeIndex;

        GameObject _combinedObject;
        SplineContainer _combinedContainer;
        ConveyorSpline _combinedSpline;
        ConveyorBeltRenderer _combinedBeltRenderer;
        MeshRenderer _combinedMeshRenderer;

        bool _sourceRendererStateCaptured;
        bool _sourceRendererWasEnabled;

        public int ActiveIndex
        {
            get
            {
                SanitizeActiveIndex();
                return _activeIndex;
            }
            set
            {
                _activeIndex = Mathf.Max(0, value);
                SanitizeActiveIndex();
                RefreshVisuals();
            }
        }

        public Vector3 SwitchPosition
        {
            get
            {
                EnsureReferences();
                return _spline != null
                    ? _spline.GetPositionOnSpline(1f, 0f)
                    : transform.position;
            }
        }

        public int ValidBranchCount
        {
            get
            {
                EnsureReferences();
                if (_connections == null || _connections.next == null)
                    return 0;

                int count = 0;
                for (int i = 0; i < _connections.next.Count; i++)
                {
                    if (_connections.next[i] != null)
                        count++;
                }
                return count;
            }
        }

        void Awake()
        {
            EnsureReferences();
        }

        void OnEnable()
        {
            EnsureReferences();
            SanitizeActiveIndex();
            RefreshVisuals();
        }

        void OnDisable()
        {
            RestoreSourceRenderer();
            SetCombinedRendererActive(false);
        }

        void OnValidate()
        {
            clickRadius = Mathf.Max(0.1f, clickRadius);
            branchClickPadding = Mathf.Max(0f, branchClickPadding);
            linkedSpriteSize = Mathf.Max(0.05f, linkedSpriteSize);
            activeBranchOwnedKnotCount = Mathf.Max(2, activeBranchOwnedKnotCount);
            linkedKnotCount = Mathf.Max(0, linkedKnotCount);
            duplicateKnotTolerance = Mathf.Max(0.0001f, duplicateKnotTolerance);

            if (Application.isPlaying)
                RefreshVisuals();
        }

        void Update()
        {
            if (GameplayPause.IsPaused || UIPointerGuard.IsPointerOverUI())
                return;
            if (ValidBranchCount < 2 || _spline == null || _spline.IsClosed)
                return;
            if (!PointerInput.PressedThisFrame())
                return;
            if (!PointerInput.TryGetPosition(out Vector2 pointerPosition))
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Vector3 screen = pointerPosition;
            screen.z = Mathf.Abs(camera.transform.position.z - SwitchPosition.z);
            Vector3 world = camera.ScreenToWorldPoint(screen);

            if (Vector2.Distance(world, SwitchPosition) <= clickRadius)
                Toggle();
            else
                TrySwitchToBranchAtWorldPosition(world);
        }

        void EnsureReferences()
        {
            _spline = _spline != null ? _spline : GetComponent<ConveyorSpline>();
            _connections = _connections != null
                ? _connections
                : GetComponent<ConveyorConnections>();
            _sourceBeltRenderer = _sourceBeltRenderer != null
                ? _sourceBeltRenderer
                : GetComponent<ConveyorBeltRenderer>();
            _sourceMeshRenderer = _sourceMeshRenderer != null
                ? _sourceMeshRenderer
                : GetComponent<MeshRenderer>();
        }

        public bool TryGetActiveNext(out ConveyorSpline next)
        {
            EnsureReferences();
            SanitizeActiveIndex();

            if (_connections == null || _connections.next == null ||
                _activeIndex < 0 || _activeIndex >= _connections.next.Count)
            {
                next = null;
                return false;
            }

            next = _connections.next[_activeIndex];
            return next != null;
        }

        public void Toggle()
        {
            EnsureReferences();
            if (_connections == null || _connections.next == null ||
                _connections.next.Count == 0)
                return;

            SanitizeActiveIndex();
            for (int step = 1; step <= _connections.next.Count; step++)
            {
                int index = (_activeIndex + step) % _connections.next.Count;
                if (_connections.next[index] == null)
                    continue;

                SetActiveIndex(index);
                return;
            }
        }

        public bool TrySwitchToBranchAtWorldPosition(Vector3 worldPosition)
        {
            return TryFindBranchIndexAtWorldPosition(worldPosition, out int branchIndex) &&
                   TrySwitchToBranchIndex(branchIndex);
        }

        public bool TryFindBranchIndexAtWorldPosition(Vector3 worldPosition, out int branchIndex)
        {
            EnsureReferences();
            branchIndex = -1;

            if (_connections == null || _connections.next == null)
                return false;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < _connections.next.Count; i++)
            {
                ConveyorSpline branch = _connections.next[i];
                if (branch == null)
                    continue;

                float progress = branch.FindClosestProgress(worldPosition, out float distance);
                if (progress < branchClickStartProgress)
                    continue;

                float hitRadius = branch.HalfWidth + branchClickPadding;
                if (distance > hitRadius || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                branchIndex = i;
            }

            return branchIndex >= 0;
        }

        public bool TrySwitchToBranchIndex(int branchIndex)
        {
            EnsureReferences();
            if (_connections == null || _connections.next == null ||
                branchIndex < 0 || branchIndex >= _connections.next.Count ||
                _connections.next[branchIndex] == null)
                return false;

            SanitizeActiveIndex();
            if (_activeIndex == branchIndex)
                return false;

            SetActiveIndex(branchIndex);
            return true;
        }

        void SetActiveIndex(int index)
        {
            _activeIndex = index;
            RefreshVisuals();
            GamePlayManager.NotifyStateChangedForSave();
        }

        public float GetActiveBranchOwnedProgress(ConveyorSpline branch)
        {
            if (branch == null || branch.Container == null || branch.Container.Spline == null ||
                branch.Container.Spline.Count < 2)
                return 0f;

            int knotIndex = Mathf.Min(
                activeBranchOwnedKnotCount - 1,
                branch.Container.Spline.Count - 1);
            return branch.GetProgressThroughKnot(knotIndex);
        }

        void SanitizeActiveIndex()
        {
            EnsureReferences();
            if (_connections == null || _connections.next == null ||
                _connections.next.Count == 0)
            {
                _activeIndex = 0;
                return;
            }

            _activeIndex = Mathf.Clamp(_activeIndex, 0, _connections.next.Count - 1);
            if (_connections.next[_activeIndex] != null)
                return;

            for (int step = 1; step < _connections.next.Count; step++)
            {
                int index = (_activeIndex + step) % _connections.next.Count;
                if (_connections.next[index] == null)
                    continue;

                _activeIndex = index;
                return;
            }
        }

        void RefreshVisuals()
        {
            if (!Application.isPlaying)
                return;

            EnsureReferences();
            EnsureIndicator();

            ConveyorSpline active = null;
            bool visible = ValidBranchCount >= 2 && TryGetActiveNext(out active);
            _indicator.enabled = visible;

            if (visible)
            {
                RefreshIndicator(active);
                RefreshCombinedRenderer(active);
            }
            else
            {
                RestoreSourceRenderer();
                SetCombinedRendererActive(false);
            }

            ConveyorConnections.RebuildNetwork(_spline);
        }

        void RefreshIndicator(ConveyorSpline active)
        {
            if (_indicator == null || active == null)
                return;

            _indicator.transform.SetParent(active.transform, true);
            _indicator.transform.position =
                active.GetPositionOnSpline(linkedSpriteProgress, 0f);
            _indicator.transform.rotation = Quaternion.identity;
            _indicator.sprite = linkedBranchSprite != null
                ? linkedBranchSprite
                : GetFallbackSprite();
            _indicator.color = indicatorColor;
            _indicator.sortingLayerID =
                active.GetComponent<MeshRenderer>()?.sortingLayerID ?? 0;
            _indicator.sortingOrder = linkedSpriteSortingOrder;

            Vector2 spriteSize = _indicator.sprite != null
                ? _indicator.sprite.bounds.size
                : Vector2.one;
            float size = Mathf.Max(0.01f, spriteSize.x, spriteSize.y);
            _indicator.transform.localScale = Vector3.one * (linkedSpriteSize / size);
        }

        void RefreshCombinedRenderer(ConveyorSpline active)
        {
            if (!useCombinedRenderer || active == null || _spline == null ||
                _sourceBeltRenderer == null)
            {
                RestoreSourceRenderer();
                SetCombinedRendererActive(false);
                return;
            }

            if (!EnsureCombinedRenderer())
            {
                RestoreSourceRenderer();
                return;
            }

            if (!BuildCombinedSpline(active))
            {
                RestoreSourceRenderer();
                SetCombinedRendererActive(false);
                return;
            }

            CopySplineSettings(_spline, _combinedSpline);
            CopyRendererSettings(_sourceBeltRenderer, _combinedBeltRenderer);

            _combinedSpline.Bake();
            _combinedBeltRenderer.RebuildMeshAndMaterials();
            CopySortingSettings();
            SetCombinedRendererActive(true);
            HideSourceRendererIfNeeded();
        }

        bool EnsureCombinedRenderer()
        {
            if (_combinedObject != null && _combinedContainer != null &&
                _combinedSpline != null && _combinedBeltRenderer != null)
                return true;

            Transform existing = transform.Find(CombinedRendererObjectName);
            if (existing != null)
                DestroyGeneratedObject(existing.gameObject);

            _combinedObject = new GameObject(CombinedRendererObjectName);
            _combinedObject.SetActive(false);
            _combinedObject.transform.SetParent(transform, false);
            _combinedObject.transform.localPosition = Vector3.zero;
            _combinedObject.transform.localRotation = Quaternion.identity;
            _combinedObject.transform.localScale = Vector3.one;

            _combinedContainer = _combinedObject.AddComponent<SplineContainer>();
            _combinedSpline = _combinedObject.AddComponent<ConveyorSpline>();
            _combinedBeltRenderer = _combinedObject.AddComponent<ConveyorBeltRenderer>();
            _combinedMeshRenderer = _combinedObject.GetComponent<MeshRenderer>();

            _combinedSpline.registerWithFallingPixelManager = false;
            _combinedSpline.autoRebakeAtRuntime = false;
            _combinedBeltRenderer.renderConnections = false;

            _combinedObject.SetActive(true);
            return _combinedContainer != null && _combinedSpline != null &&
                   _combinedBeltRenderer != null && _combinedMeshRenderer != null;
        }

        bool BuildCombinedSpline(ConveyorSpline active)
        {
            if (_combinedContainer == null || _combinedContainer.Spline == null ||
                !HasUsableSpline(_spline) || !HasUsableSpline(active))
                return false;

            Spline output = _combinedContainer.Spline;
            output.Clear();
            output.Closed = false;

            AppendKnots(output, _spline, _spline.Container.Spline.Count);

            int targetCount = linkedKnotCount <= 0
                ? active.Container.Spline.Count
                : Mathf.Min(linkedKnotCount, active.Container.Spline.Count);
            AppendKnots(output, active, targetCount);

            return output.Count >= 2;
        }

        void AppendKnots(Spline output, ConveyorSpline conveyor, int count)
        {
            if (output == null || !HasUsableSpline(conveyor))
                return;

            count = Mathf.Clamp(count, 0, conveyor.Container.Spline.Count);
            float toleranceSquared = duplicateKnotTolerance * duplicateKnotTolerance;

            for (int i = 0; i < count; i++)
            {
                Vector3 sourceLocal = (Vector3)conveyor.Container.Spline[i].Position;
                Vector3 world = conveyor.transform.TransformPoint(sourceLocal);
                Vector3 combinedLocal = _combinedObject.transform.InverseTransformPoint(world);

                if (output.Count > 0)
                {
                    Vector3 previous = (Vector3)output[output.Count - 1].Position;
                    if ((previous - combinedLocal).sqrMagnitude <= toleranceSquared)
                        continue;
                }

                output.Add(new BezierKnot((float3)combinedLocal));
            }
        }

        static bool HasUsableSpline(ConveyorSpline conveyor)
        {
            return conveyor != null && conveyor.Container != null &&
                   conveyor.Container.Spline != null &&
                   conveyor.Container.Spline.Count >= 2;
        }

        static void CopySplineSettings(ConveyorSpline source, ConveyorSpline destination)
        {
            destination.beltWidth = source.beltWidth;
            destination.bakeResolution = source.bakeResolution;
            destination.straightEdges = true;
            destination.cornerRadius = source.cornerRadius;
            destination.cornerSegments = source.cornerSegments;
            destination.autoRebakeAtRuntime = false;
            destination.registerWithFallingPixelManager = false;
        }

        void CopyRendererSettings(ConveyorBeltRenderer source,
            ConveyorBeltRenderer destination)
        {
            destination.beltMaterial = source.beltMaterial;
            destination.texture = source.texture;
            destination.segments = source.segments;
            destination.zOffset = source.zOffset + combinedRendererZOffset;

            destination.renderConnections = false;
            destination.connectionSegments = source.connectionSegments;
            destination.connectionMinLead = source.connectionMinLead;
            destination.connectionWidthFactor = source.connectionWidthFactor;
            destination.connectionMaxProgress = source.connectionMaxProgress;
            destination.straightConnectionDot = source.straightConnectionDot;
            destination.connectionSnapWidthFactor = source.connectionSnapWidthFactor;

            destination.tilesAcrossWidth = source.tilesAcrossWidth;
            destination.scrollSpeed = source.scrollSpeed;

            destination.showWalls = source.showWalls;
            destination.wallWidth = source.wallWidth;
            destination.wallZOffset = source.wallZOffset;
            destination.scrollWalls = source.scrollWalls;
            destination.wallMaterialOuter = source.wallMaterialOuter;
            destination.wallTextureOuter = source.wallTextureOuter;
            destination.wallMaterialInner = source.wallMaterialInner;
            destination.wallTextureInner = source.wallTextureInner;
        }

        void CopySortingSettings()
        {
            if (_sourceMeshRenderer == null || _combinedMeshRenderer == null)
                return;

            _combinedMeshRenderer.sortingLayerID = _sourceMeshRenderer.sortingLayerID;
            _combinedMeshRenderer.sortingOrder =
                _sourceMeshRenderer.sortingOrder + combinedSortingOrderOffset;
        }

        void HideSourceRendererIfNeeded()
        {
            if (!hideSourceRenderer || _sourceMeshRenderer == null)
            {
                RestoreSourceRenderer();
                return;
            }

            if (!_sourceRendererStateCaptured)
            {
                _sourceRendererWasEnabled = _sourceMeshRenderer.enabled;
                _sourceRendererStateCaptured = true;
            }

            _sourceMeshRenderer.enabled = false;
        }

        void RestoreSourceRenderer()
        {
            if (!_sourceRendererStateCaptured || _sourceMeshRenderer == null)
                return;

            _sourceMeshRenderer.enabled = _sourceRendererWasEnabled;
            _sourceRendererStateCaptured = false;
        }

        void SetCombinedRendererActive(bool active)
        {
            if (_combinedObject != null && _combinedObject.activeSelf != active)
                _combinedObject.SetActive(active);
        }

        void EnsureIndicator()
        {
            if (_indicator != null)
                return;

            GameObject indicatorObject = new GameObject("SwitchIndicator");
            indicatorObject.transform.SetParent(transform, false);
            _indicator = indicatorObject.AddComponent<SpriteRenderer>();
            _indicator.sprite = linkedBranchSprite != null
                ? linkedBranchSprite
                : GetFallbackSprite();
        }

        Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null)
                return _fallbackSprite;

            _fallbackSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            _fallbackSprite.name = "Switch Indicator Fallback";
            return _fallbackSprite;
        }

        void OnDestroy()
        {
            RestoreSourceRenderer();
            DestroyGeneratedObject(_combinedObject);
            _combinedObject = null;

            if (_fallbackSprite != null)
            {
                if (Application.isPlaying)
                    Destroy(_fallbackSprite);
                else
                    DestroyImmediate(_fallbackSprite);
            }
        }

        static void DestroyGeneratedObject(GameObject value)
        {
            if (value == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(value);
            else
                Object.DestroyImmediate(value);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = indicatorColor;
            Gizmos.DrawWireSphere(SwitchPosition, clickRadius);
        }
    }
}
