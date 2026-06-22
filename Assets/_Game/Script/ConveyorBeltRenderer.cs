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

        ConveyorSpline _conveyor;
        MeshFilter _mf;
        MeshRenderer _mr;
        Mesh _mesh;
        Material _runtimeMat;     // material tự tạo (nếu có) để dọn dẹp
        Material _scrollMat;      // material instance dùng để cuộn lúc play
        float _scroll;
        float _vTiles = 1f;

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
                if (_scrollMat != null) Destroy(_scrollMat);
                if (_runtimeMat != null) Destroy(_runtimeMat);
            }
        }

        void EnsureComponents()
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();

            if (beltMaterial != null)
            {
                _mr.sharedMaterial = beltMaterial;
            }
            else if (_mr.sharedMaterial == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Texture");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                _runtimeMat = new Material(sh) { name = "ConveyorBeltMat (runtime)" };
                _mr.sharedMaterial = _runtimeMat;
            }

            // Gán texture + bật Repeat để cuộn không bị kẹp mép.
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
                if (_mr.sharedMaterial != null) _mr.sharedMaterial.mainTexture = texture;
            }
        }

        /// <summary>Dựng lại mesh dải dọc spline. Gọi lại nếu spline/bề rộng thay đổi.</summary>
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

            int vCount = (n + 1) * 2;
            var verts = new Vector3[vCount];
            var uvs = new Vector2[vCount];
            var tris = new int[n * 6];

            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;

                // GetPositionOnSpline trả về WORLD -> đưa về LOCAL của object này cho MeshFilter.
                Vector3 wl = Conveyor.GetPositionOnSpline(t, +halfW);
                Vector3 wr = Conveyor.GetPositionOnSpline(t, -halfW);
                wl.z += zOffset; wr.z += zOffset;

                Vector3 ll = transform.InverseTransformPoint(wl);
                Vector3 lr = transform.InverseTransformPoint(wr);

                int vi = i * 2;
                verts[vi] = ll;       // mép trái (+halfW)
                verts[vi + 1] = lr;   // mép phải (-halfW)

                float v = t * _vTiles;
                uvs[vi] = new Vector2(0f, v);
                uvs[vi + 1] = new Vector2(tilesAcrossWidth, v);
            }

            for (int i = 0; i < n; i++)
            {
                int vi = i * 2;
                int ti = i * 6;
                tris[ti] = vi;
                tris[ti + 1] = vi + 2;
                tris[ti + 2] = vi + 1;
                tris[ti + 3] = vi + 1;
                tris[ti + 4] = vi + 2;
                tris[ti + 5] = vi + 3;
            }

            if (_mesh == null) _mesh = new Mesh { name = "ConveyorBeltMesh" };
            _mesh.Clear();
            _mesh.vertices = verts;
            _mesh.uv = uvs;
            _mesh.triangles = tris;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            _mf.sharedMesh = _mesh;
        }

        void Update()
        {
            if (_mr == null) return;

            if (Application.isPlaying)
            {
                // Dùng material instance để không ghi đè asset gốc.
                if (_scrollMat == null) _scrollMat = _mr.material; // .material tạo instance
                _scroll += scrollSpeed * Time.deltaTime;

                Vector2 off = _scrollMat.mainTextureOffset;
                // offset âm theo V -> hoa văn chạy về phía t=1 khi scrollSpeed dương.
                off.y = -_scroll;
                _scrollMat.mainTextureOffset = off;
            }
        }
    }
}
