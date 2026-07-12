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
        [Tooltip("Prefab vật cản (có component DotObstacle). Chỉ cần khi level có obstacles.")]
        public DotObstacle obstaclePrefab;
        [Tooltip("Database quả/màu. Gán xuống cho bucket/spawner nào chưa có.")]
        public FruitDatabase fruitDatabase;

        [Header("Luật chơi")]
        [Tooltip("Giới hạn thời gian (giây) cho level. 0 = không giới hạn.")]
        [Min(0f)] public float timeLimit = 0f;
        [Tooltip("So luot tuong tac voi bucket/spawner. 0 = khong gioi han.")]
        [Min(0)] public int moveLimit = 0;

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
            [Tooltip("Đặt switch định tuyến ở cuối băng: người chơi click đổi nhánh dot đi tiếp. " +
                     "Chỉ có nghĩa khi băng HỞ và có >= 2 link đi ra.")]
            public bool hasSwitch;
            public bool straightEdges = true;
            [Min(0f)] public float cornerRadius = 0.5f;
            [Min(1)] public int cornerSegments = 8;
            [Tooltip("Vị trí WORLD của các knot.")]
            public List<Vector3> knots = new List<Vector3>();
        }

        [Serializable]
        public class ObstacleData
        {
            public Vector3 position;
            [Tooltip("Số dot phải ĐANG đè lên cùng lúc để vật cản vỡ (dot bị chặn, không bị tiêu hao).")]
            [Min(1)] public int requiredDots = 10;
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
        public List<ObstacleData> obstacles = new List<ObstacleData>();

        /// <summary>
        /// Hash các thông số gameplay của level. Snapshot tiến trình lưu hash này lúc chụp;
        /// khi restore mà hash hiện tại khác (designer đã sửa totalDots/spawnCount/moveLimit...)
        /// thì snapshot bị coi là cũ và bỏ đi, tránh đè giá trị cũ lên data mới.
        /// </summary>
        public static int ComputeConfigHash(LevelData data)
        {
            if (data == null) return 0;

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + data.moveLimit;
                hash = hash * 31 + Mathf.RoundToInt(data.timeLimit * 100f);

                for (int i = 0; i < data.buckets.Count; i++)
                {
                    BucketData b = data.buckets[i];
                    hash = hash * 31 + b.colorId;
                    hash = hash * 31 + b.maxFill;
                }

                for (int i = 0; i < data.spawners.Count; i++)
                {
                    hash = HashSpawner(hash, data.spawners[i]);
                }

                for (int i = 0; i < data.columns.Count; i++)
                {
                    List<SpawnerData> columnSpawners = data.columns[i].spawners;
                    for (int j = 0; j < columnSpawners.Count; j++)
                    {
                        hash = HashSpawner(hash, columnSpawners[j]);
                    }
                }

                for (int i = 0; i < data.obstacles.Count; i++)
                {
                    hash = hash * 31 + data.obstacles[i].requiredDots;
                }

                hash = hash * 31 + data.conveyors.Count;

                // Chỉ hash khi CÓ switch để không đổi hash của các level cũ (giữ save đang chơi dở).
                for (int i = 0; i < data.conveyors.Count; i++)
                    if (data.conveyors[i].hasSwitch) hash = hash * 31 + (i + 1);

                return hash;
            }
        }

        static int HashSpawner(int hash, SpawnerData s)
        {
            unchecked
            {
                hash = hash * 31 + s.fixedColorId;
                hash = hash * 31 + s.totalDots;
                hash = hash * 31 + s.spawnCount;
                return hash;
            }
        }
    }
}
