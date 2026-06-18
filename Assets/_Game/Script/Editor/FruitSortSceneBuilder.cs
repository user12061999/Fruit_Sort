#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace FruitSort.EditorTools
{
    /// <summary>
    /// Tự dựng toàn bộ scene mẫu: Camera, Spline (băng chuyền), Managers, Grid, Shooter, Buckets.
    /// Dùng: menu  Tools > FruitSort > Build Sample Scene.
    /// Có thể chạy nhiều lần — sẽ xoá các object cũ do builder tạo (tag tên "_FS_").
    /// </summary>
    public static class FruitSortSceneBuilder
    {
        const string DotPrefabPath = "Assets/_Game/Prefabs/Dot.prefab";
        const string DotSpritePath = "Assets/_Game/Sprites/WhiteDot.png";

        static readonly Color[] Palette =
        {
            new Color(0.93f, 0.26f, 0.21f), // 0 đỏ
            new Color(0.30f, 0.69f, 0.31f), // 1 xanh lá
            new Color(0.13f, 0.59f, 0.95f), // 2 xanh dương
            new Color(1.00f, 0.76f, 0.03f), // 3 vàng
        };

        [MenuItem("Tools/FruitSort/Build Sample Scene")]
        public static void BuildSampleScene()
        {
            var dotPrefab = AssetDatabase.LoadAssetAtPath<Dot>(DotPrefabPath);
            if (dotPrefab == null)
            {
                EditorUtility.DisplayDialog("FruitSort", "Không tìm thấy Dot prefab tại\n" + DotPrefabPath, "OK");
                return;
            }
            var dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DotSpritePath);

            // Dọn object cũ do builder tạo.
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go != null && go.name.StartsWith("_FS_"))
                    Object.DestroyImmediate(go);

            // ---- Camera ----
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("_FS_MainCamera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.transform.position = new Vector3(0f, 1.5f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.16f);

            // ---- Spline / băng chuyền ----
            var beltGo = new GameObject("_FS_Conveyor");
            var container = beltGo.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            spline.Add(new BezierKnot(new float3(-7f, -2.0f, 0f)), TangentMode.AutoSmooth);
            spline.Add(new BezierKnot(new float3(-2f, -3.0f, 0f)), TangentMode.AutoSmooth);
            spline.Add(new BezierKnot(new float3( 3f, -2.4f, 0f)), TangentMode.AutoSmooth);
            spline.Add(new BezierKnot(new float3( 7f, -3.2f, 0f)), TangentMode.AutoSmooth);
            var conveyor = beltGo.AddComponent<ConveyorSpline>();
            conveyor.beltWidth = 3f;

            // ---- FallingPixelManager ----
            var fallGo = new GameObject("_FS_FallingPixelManager");
            var falling = fallGo.AddComponent<FallingPixelManager>();
            falling.conveyor = conveyor;
            falling.maxDots = 500;
            falling.dotSize = 0.5f;
            falling.beltEntryY = -2.0f;   // ~ Y tại t=0 của spline
            falling.beltSpeed = 2.5f;
            falling.gravity = 22f;

            // ---- PixelGridManager + gốc lưới ----
            var gridOrigin = new GameObject("_FS_GridOrigin");
            gridOrigin.transform.position = new Vector3(-4.2f, 1.5f, 0f);

            var gridGo = new GameObject("_FS_PixelGridManager");
            var grid = gridGo.AddComponent<PixelGridManager>();
            grid.dotPrefab = dotPrefab;
            grid.fallingManager = falling;
            grid.gridOrigin = gridOrigin.transform;
            grid.columns = 16;
            grid.rows = 8;
            grid.spacing = 0.55f;
            grid.dotScale = 0.5f;
            grid.dotHP = 3;
            grid.palette = (Color[])Palette.Clone();

            // ---- Shooter (đáy màn hình) ----
            var shooterGo = new GameObject("_FS_Shooter");
            shooterGo.transform.position = new Vector3(0f, -5.5f, 0f);
            var shooter = shooterGo.AddComponent<Shooter>();
            shooter.gridManager = grid;
            shooter.cam = cam;
            shooter.aimAtMouse = true;
            var tracer = shooterGo.AddComponent<LineRenderer>();
            tracer.widthMultiplier = 0.05f;
            tracer.material = new Material(Shader.Find("Sprites/Default"));
            tracer.enabled = false;
            shooter.tracer = tracer;

            // ---- GameManager ----
            var gmGo = new GameObject("_FS_GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.gridManager = grid;
            gm.fallingManager = falling;

            // ---- Buckets: 1 cái / màu, đặt sát đường spline ----
            float[] ts = { 0.12f, 0.40f, 0.68f, 0.92f };
            for (int i = 0; i < Palette.Length; i++)
            {
                float t = ts[i % ts.Length];
                Vector3 onBelt = conveyor.GetPositionOnSpline(t, 0f);
                Vector3 pos = onBelt + new Vector3(0f, 0.9f, 0f); // hơi cao hơn băng chuyền, trong attractRadius

                var b = new GameObject($"_FS_Bucket_{i}");
                b.transform.position = pos;
                b.transform.localScale = Vector3.one * 0.9f;
                var sr = b.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.color = Palette[i];
                sr.sortingOrder = 5;
                var bucket = b.AddComponent<Bucket>();
                bucket.colorId = i;
                bucket.color = Palette[i];
                bucket.maxFill = 5;
                bucket.attractRadius = 1.4f;
                bucket.attractSpeed = 7f;
                bucket.body = sr;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[FruitSort] Đã dựng scene mẫu. Bấm Play, giữ chuột trái để bắn. Nhớ Save Scene (Ctrl+S).");
            EditorUtility.DisplayDialog("FruitSort",
                "Đã dựng scene mẫu xong!\n\n- Bấm Play\n- Giữ chuột trái để bắn dot\n- Bắn vỡ dot -> rơi xuống băng chuyền -> bucket cùng màu hút\n\nNhớ Ctrl+S để lưu scene.", "OK");
        }
    }
}
#endif
