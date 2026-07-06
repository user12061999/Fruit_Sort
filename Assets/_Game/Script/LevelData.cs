using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Toàn bộ dữ liệu 1 level: prefab config + danh sách Bucket / Spawner / SpawnerColumn /
    /// ConveyorSpline (knots) + liên kết định tuyến giữa các băng.
    ///
    /// Dùng ScriptableObject (không JSON) vì cần giữ THAM CHIẾU prefab/FruitDatabase —
    /// serialize trực tiếp trong asset, không cần bảng tra GUID.
    /// Editor lưu scene -> asset qua LevelEditorWindow; runtime dựng lại qua LevelLoader.
    /// </summary>
    [CreateAssetMenu(menuName = "LoopSort/Level Data", fileName = "Level_001")]
    public class LevelData : ScriptableObject
    {
        [Header("Prefabs (dùng khi build level)")]
        [Tooltip("Prefab bucket/cage (có component Bucket).")]
        public Bucket bucketPrefab;
        [Tooltip("Prefab gói spawn dot (có component ModelDotSpawner).")]
        public ModelDotSpawner spawnerPrefab;
        [Tooltip("Prefab cột spawner (có component ModelDotSpawnerColumn). Để trống = tool tự tạo GameObject trống.")]
        public ModelDotSpawnerColumn columnPrefab;
        [Tooltip("Prefab băng chuyền (SplineContainer + ConveyorSpline). Để trống = tool tự tạo GameObject trống.")]
        public ConveyorSpline conveyorPrefab;
        [Tooltip("Database quả/màu. Gán xuống cho bucket/spawner nào chưa có.")]
        public FruitDatabase fruitDatabase;

        [Header("Luật chơi")]
        [Tooltip("Giới hạn thời gian (giây) cho level. 0 = không giới hạn.")]
        [Min(0f)] public float timeLimit = 0f;

        [Serializable]
        public class BucketData
        {
            public Vector3 position;
            [Tooltip("ID màu, khớp colorId trong FruitDatabase.")]
            public int colorId;
            [Tooltip("Số dot để đầy bucket.")]
            [Min(1)] public int maxFill = 5;
            [Tooltip("Hướng phóng dot trả về băng khi click nhả giỏ (sẽ normalize).")]
            public Vector2 launchDirection = Vector2.down;
        }

        [Serializable]
        public class SpawnerData
        {
            [Tooltip("WORLD position nếu đứng riêng; LOCAL position nếu nằm trong column.")]
            public Vector3 position;
            [Tooltip("-1 = random theo FruitDatabase/palette.")]
            public int fixedColorId = -1;
            [Tooltip("Tổng số dot trong gói (totalClicks của ModelDotSpawner).")]
            [Min(1)] public int totalDots = 50;
            [Tooltip("Số dot sinh ra mỗi lần click.")]
            [Min(1)] public int spawnCount = 10;
            [Tooltip("Hướng phóng dot vào băng (sẽ normalize). VD: (0,-1) xuống, (0,1) lên.")]
            public Vector2 launchDirection = Vector2.down;
            [Tooltip("Tốc độ phóng (world unit/giây).")]
            [Min(0.1f)] public float launchSpeed = 10f;
        }

        [Serializable]
        public class ColumnData
        {
            public Vector3 position;
            public bool clickAnywhereInColumn = true;
            [Tooltip("Các gói trong cột, thứ tự = thứ tự active (con đầu trước). Position là LOCAL.")]
            public List<SpawnerData> spawners = new List<SpawnerData>();
        }

        [Serializable]
        public class ConveyorData
        {
            public string name = "Conveyor";
            public float beltWidth = 3f;
            [Tooltip("Spline khép kín (loop) hay hở.")]
            public bool closed;
            public bool straightEdges = true;
            [Min(0f)] public float cornerRadius = 0.5f;
            [Min(1)] public int cornerSegments = 8;
            [Tooltip("Vị trí WORLD của các knot.")]
            public List<Vector3> knots = new List<Vector3>();
        }

        /// <summary>Cuối băng <see cref="from"/> -> đầu băng <see cref="to"/> (index trong <see cref="conveyors"/>).</summary>
        [Serializable]
        public class ConveyorLink
        {
            public int from;
            public int to;
        }

        public List<ConveyorData> conveyors = new List<ConveyorData>();
        public List<ConveyorLink> conveyorLinks = new List<ConveyorLink>();
        public List<BucketData> buckets = new List<BucketData>();
        public List<SpawnerData> spawners = new List<SpawnerData>();
        public List<ColumnData> columns = new List<ColumnData>();
    }
}
