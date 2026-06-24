using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FruitSort
{
    /// <summary>
    /// Sinh một dải mesh chạy dọc theo <see cref="ConveyorSpline"/> với bề rộng = beltWidth,
    /// dán texture lên dải đó. Texture được map sao cho tỉ lệ ô luôn vuông theo CHIỀU RỘNG băng
    /// (đổi beltWidth -> texture co/giãn theo), và CUỘN dọc theo hướng băng chuyền để mô phỏng
    /// băng đang chạy.
    /// Tuỳ chọn dựng thêm THÀNH TRONG / THÀNH NGOÀI: hai dải viền dọc 2 mép băng, mỗi thành có
    /// material/texture riêng (submesh riêng). Thành đứng yên (không cuộn) như khung băng.
    /// Gắn CHUNG GameObject với ConveyorSpline (SplineContainer).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ConveyorSpline))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ConveyorBeltRenderer : MonoBehaviour
    {
        [Header("Texture / Material")]
        [Tooltip("Material dùng cho băng. Để trống sẽ tự tạo material Unlit từ 'texture' bên dưới.")]
        public Material beltMaterial;
        [Tooltip("Texture băng chuyền (nên đặt Wrap Mode = Repeat). Dùng khi không gán beltMaterial.")]
        public Texture texture;

        [Header("Mesh")]
        [Tooltip("Số đoạn chia dọc spline. Càng cao càng mượt theo đường cong.")]
        [Min(2)] public int segments = 64;
        [Tooltip("Đẩy mesh ra sau dot một chút theo trục Z (world) để không che dot.")]
        public float zOffset = 0.05f;


        [Header("Texture mapping")]
        [Tooltip("Số lần lặp texture theo CHIỀU RỘNG băng. Tiling dọc tự tính để giữ ô vuông.")]
        public float tilesAcrossWidth = 1f;

        [Header("Cuộn (scroll)")]
        [Tooltip("Tốc độ cuộn texture dọc băng (UV/giây). Dương = chạy theo chiều dot đi (t:0->1).")]
        public float scrollSpeed = 0.5f;

        [Header("Thành băng (tường viền 2 mép)")]
        [Tooltip("Bật để dựng thêm thành trong & thành ngoài dọc 2 mép băng.")]
        public bool showWalls = false;
        [Tooltip("Bề rộng thành (world), đo từ mép băng ra NGOÀI. Âm = lấn vào trong băng.")]
        public float wallWidth = 0.15f;
        [Tooltip("Lệch Z của thành so với băng (âm = nổi lên trước băng/dot, dương = ra sau).")]
        public float wallZOffset = -0.01f;
        [Tooltip("Bật: thành cũng cuộn theo băng. Tắt (mặc định): thành đứng yên như khung.")]
        public bool scrollWalls = false;

        [Tooltip("Material THÀNH NGOÀI (mép +halfW). Trống = tạo Unlit từ wallTextureOuter.")]
        public Material wallMaterialOuter;
        [Tooltip("Texture THÀNH NGOÀI (Wrap = Repeat). Dùng khi không gán wallMaterialOuter.")]
        public Texture wallTextureOuter;

        [Tooltip("Material THÀNH TRONG (mép -halfW). Trống = tạo Unlit từ wallTextureInner.")]
        public Material wallMaterialInner;
        [Tooltip("Texture THÀNH TRONG (Wrap = Repeat). Dùng khi không gán wallMaterialInner.")]
        public Texture wallTextureInner;

        ConveyorSpline _conveyor;
        MeshFilter _mf;
        MeshRenderer _mr;
        Mesh _mesh;
        Material _runtimeMat;        // material băng tự tạo (nếu có) để dọn dẹp
        Material _runtimeMatOuter;   // material thành ngoài tự tạo
        Material _runtimeMatInner;   // material thành trong tự tạo
        Material[] _scrollMats;      // material instances dùng để cuộn lúc play
        float _scroll;
        float _vTiles = 1f;

        // Bộ đệm dựng mesh (tái dùng để giảm GC).
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<int> _triBelt = new List<int>();
        readonly List<int> _triOuter = new List<int>();
        readonly List<int> _triInner = new List<int>();

        ConveyorSpline Conveyor
        {
            get { if (_conveyor == null) _conveyor = GetComponent<ConveyorSpline>(); return _conveyor; }
        }

        void OnEnable()
        {
            EnsureComponents();
            BuildMesh();
        }

        void OnValidate()
        {
            // Rebuild mesh khi sửa field trên Inspector (segments, zOffset, tilesAcrossWidth...)
            if (this == null || !Application.isPlaying)
            {
                BuildMesh();
#if UNITY_EDITOR
                SceneView.RepaintAll();
#endif
            }
        }

        void OnDisable()
        {
            // Dọn material/mesh tạo runtime để tránh leak.
            if (Application.isPlaying)
            {
                if (_scrollMats != null) { foreach (var m in _scrollMats) if (m != null) Destroy(m); _scrollMats = null; }
                if (_runtimeMat != null) Destroy(_runtimeMat);
                if (_runtimeMatOuter != null) Destroy(_runtimeMatOuter);
                if (_runtimeMatInner != null) Destroy(_runtimeMatInner);
            }
        }

        void EnsureComponents()
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();

            // Material băng (submesh 0).
            Material belt = ResolveMaterial(beltMaterial, ref _runtimeMat, texture, "ConveyorBeltMat (runtime)");

            if (showWalls)
            {
                Material outer = ResolveMaterial(wallMaterialOuter, ref _runtimeMatOuter, wallTextureOuter, "ConveyorWallOuterMat (runtime)");
                Material inner = ResolveMaterial(wallMaterialInner, ref _runtimeMatInner, wallTextureInner, "ConveyorWallInnerMat (runtime)");
                _mr.sharedMaterials = new[] { belt, outer, inner };
            }
            else
            {
                _mr.sharedMaterials = new[] { belt };
            }
        }

        // Trả về material để dùng: ưu tiên material gán sẵn, nếu trống thì tạo Unlit từ texture.
        Material ResolveMaterial(Material assigned, ref Material runtimeSlot, Texture tex, string runtimeName)
        {
            if (assigned != null)
            {
                if (tex != null)
                {
                    tex.wrapMode = TextureWrapMode.Repeat;
                    assigned.mainTexture = tex;
                }
                return assigned;
            }

            if (runtimeSlot == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Texture");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                runtimeSlot = new Material(sh) { name = runtimeName };
            }
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                runtimeSlot.mainTexture = tex;
            }
            return runtimeSlot;
        }

        /// <summary>Dựng lại mesh dải dọc spline (kèm thành nếu bật). Gọi lại nếu spline/bề rộng thay đổi.</summary>
        [ContextMenu("Rebuild Belt Mesh")]
        public void BuildMesh()
        {
            if (Conveyor == null) return;
            EnsureComponents();

            int n = Mathf.Max(2, segments);
            float halfW = Conveyor.HalfWidth;
            float width = Mathf.Max(1e-4f, Conveyor.beltWidth);
            float length = Conveyor.GetSplineLength();

            // Giữ ô texture vuông: số lần lặp dọc = tilesAcrossWidth * (dài / rộng).
            _vTiles = tilesAcrossWidth * (length / width);
            // Số lần lặp ngang trên thành (theo bề rộng thành) để giữ ô vuông trên thành.
            float wallTiles = tilesAcrossWidth * (Mathf.Abs(wallWidth) / width);

            _verts.Clear(); _uvs.Clear();
            _triBelt.Clear(); _triOuter.Clear(); _triInner.Clear();

            // Mặt băng: mép trái (+halfW) -> mép phải (-halfW).
            AppendStrip(n, +halfW, -halfW, zOffset, tilesAcrossWidth, 0f, _triBelt);

            if (showWalls)
            {
                // Thành NGOÀI: từ (+halfW + wallWidth) về (+halfW). Giữ latA > latB như mặt băng.
                AppendStrip(n, +halfW + wallWidth, +halfW, zOffset + wallZOffset, wallTiles, 0f, _triOuter);
                // Thành TRONG: từ (-halfW) về (-halfW - wallWidth).
                AppendStrip(n, -halfW, -halfW - wallWidth, zOffset + wallZOffset, 0f, wallTiles, _triInner);
            }

            if (_mesh == null) _mesh = new Mesh { name = "ConveyorBeltMesh" };
            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetUVs(0, _uvs);

            _mesh.subMeshCount = showWalls ? 3 : 1;
            _mesh.SetTriangles(_triBelt, 0);
            if (showWalls)
            {
                _mesh.SetTriangles(_triOuter, 1);
                _mesh.SetTriangles(_triInner, 2);
            }

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _mf.sharedMesh = _mesh;
        }

        // Thêm 1 dải dọc spline giữa 2 mức lệch ngang latA (UV u = uA) và latB (UV u = uB).
        // Đỉnh và tam giác nối tiếp vào _verts/_uvs và list tam giác của submesh tương ứng.
        void AppendStrip(int n, float latA, float latB, float zoff, float uA, float uB, List<int> tris)
        {
            int baseIndex = _verts.Count;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;

                // GetPositionOnSpline trả về WORLD -> đưa về LOCAL của object này cho MeshFilter.
                Vector3 wa = Conveyor.GetPositionOnSpline(t, latA);
                Vector3 wb = Conveyor.GetPositionOnSpline(t, latB);
                wa.z += zoff; wb.z += zoff;

                _verts.Add(transform.InverseTransformPoint(wa));
                _verts.Add(transform.InverseTransformPoint(wb));

                float v = t * _vTiles;
                _uvs.Add(new Vector2(uA, v));
                _uvs.Add(new Vector2(uB, v));
            }

            for (int i = 0; i < n; i++)
            {
                int vi = baseIndex + i * 2;
                tris.Add(vi);
                tris.Add(vi + 2);
                tris.Add(vi + 1);
                tris.Add(vi + 1);
                tris.Add(vi + 2);
                tris.Add(vi + 3);
            }
        }

        void Update()
        {
            if (_mr == null) return;

            if (Application.isPlaying)
            {
                // Dùng material instances để không ghi đè asset gốc.
                if (_scrollMats == null) _scrollMats = _mr.materials; // .materials tạo instances cho mọi submesh
                _scroll += scrollSpeed * Time.deltaTime;

                // offset âm theo V -> hoa văn chạy về phía t=1 khi scrollSpeed dương.
                // Băng (index 0) luôn cuộn; thành (1,2) chỉ cuộn khi scrollWalls.
                for (int i = 0; i < _scrollMats.Length; i++)
                {
                    if (_scrollMats[i] == null) continue;
                    if (i > 0 && !scrollWalls) continue;
                    Vector2 off = _scrollMats[i].mainTextureOffset;
                    off.y = -_scroll;
                    _scrollMats[i].mainTextureOffset = off;
                }
            }
        }
    }
}
