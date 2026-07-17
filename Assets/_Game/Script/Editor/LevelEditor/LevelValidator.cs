#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FruitSort.EditorTools
{
    /// <summary>1 lỗi/cảnh báo tìm được khi validate level.</summary>
    public class LevelIssue
    {
        public MessageType type;
        public string message;
        /// <summary>Object liên quan (click để ping trong scene). Có thể null.</summary>
        public Object context;

        public LevelIssue(MessageType type, string message, Object context = null)
        {
            this.type = type;
            this.message = message;
            this.context = context;
        }
    }

    /// <summary>
    /// Validate level ĐANG DỰNG TRONG SCENE (không validate asset — asset chỉ là bản chụp
    /// của scene sau khi Save). Trả về danh sách lỗi/cảnh báo cho LevelEditorWindow hiển thị.
    /// </summary>
    public static class LevelValidator
    {
        public static List<LevelIssue> ValidateScene(LevelData level)
        {
            var issues = new List<LevelIssue>();

            // ---- Config asset ----
            if (level == null)
            {
                issues.Add(new LevelIssue(MessageType.Error, "Chưa gán LevelData."));
                return issues;
            }
            if (level.bucketPrefab == null)
                issues.Add(new LevelIssue(MessageType.Error, "LevelData chưa gán Bucket prefab.", level));
            if (level.spawnerPrefab == null)
                issues.Add(new LevelIssue(MessageType.Error, "LevelData chưa gán Spawner prefab.", level));
            if (level.fruitDatabase == null)
                issues.Add(new LevelIssue(MessageType.Warning, "LevelData chưa gán FruitDatabase — không check được colorId.", level));

            var buckets = Object.FindObjectsByType<Bucket>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var conveyors = Object.FindObjectsByType<ConveyorSpline>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var spawners = Object.FindObjectsByType<ModelDotSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var columns = Object.FindObjectsByType<ModelDotSpawnerColumn>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var obstacles = Object.FindObjectsByType<DotObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // ---- Obstacle ----
            if (obstacles.Length > 0 && level.obstaclePrefab == null)
                issues.Add(new LevelIssue(MessageType.Error,
                    "Scene có vật cản nhưng LevelData chưa gán Obstacle prefab — runtime sẽ không dựng được.", level));
            foreach (var o in obstacles)
            {
                // Ngưỡng vỡ là số dot ĐÈ LÊN CÙNG LÚC -> phải đạt được bằng 1 loạt spawn.
                int maxBurst = 0;
                foreach (var s in spawners) maxBurst = Mathf.Max(maxBurst, s.spawnCount);
                if (maxBurst > 0 && o.requiredDots > maxBurst)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Vật cản '{o.name}' cần {o.requiredDots} dot đè lên cùng lúc nhưng loạt spawn lớn nhất chỉ có {maxBurst} dot — có thể không phá nổi.", o));
            }

            // ---- Tồn tại tối thiểu ----
            if (buckets.Length == 0)
                issues.Add(new LevelIssue(MessageType.Error, "Level chưa có Bucket nào."));
            if (conveyors.Length == 0)
                issues.Add(new LevelIssue(MessageType.Warning, "Level chưa có băng chuyền (ConveyorSpline)."));
            if (spawners.Length == 0)
                issues.Add(new LevelIssue(MessageType.Warning, "Level chưa có nguồn dot (ModelDotSpawner / Column)."));

            // ---- Conveyor ----
            foreach (var c in conveyors)
            {
                var spline = c.Container != null ? c.Container.Spline : null;
                if (spline == null || spline.Count < 2)
                    issues.Add(new LevelIssue(MessageType.Error,
                        $"Băng chuyền '{c.name}' có ít hơn 2 knot — không dùng được.", c));

                var conn = c.GetComponent<ConveyorConnections>();
                if (conn != null)
                    for (int i = 0; i < conn.next.Count; i++)
                        if (conn.next[i] == null)
                            issues.Add(new LevelIssue(MessageType.Warning,
                                $"Băng chuyền '{c.name}' có link 'next[{i}]' bị null.", c));

                // ---- Switch định tuyến ----
                var routeSwitch = c.GetComponent<ConveyorSwitch>();
                bool closed = spline != null && spline.Closed;
                int branches = routeSwitch != null ? routeSwitch.ValidBranchCount
                             : (conn != null ? CountValidNext(conn) : 0);
                if (routeSwitch != null && closed)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Băng chuyền '{c.name}' KHÉP KÍN nhưng có ConveyorSwitch — dot không bao giờ rời băng kín, switch vô dụng.", c));
                if (routeSwitch != null && !closed && branches < 2)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Băng chuyền '{c.name}' có ConveyorSwitch nhưng chỉ có {branches} nhánh next — cần >= 2 nhánh để switch có nghĩa.", c));
                if (routeSwitch == null && !closed && branches >= 2)
                    issues.Add(new LevelIssue(MessageType.Info,
                        $"Băng chuyền '{c.name}' có {branches} nhánh next mà không có ConveyorSwitch — dot sẽ chia nhánh NGẪU NHIÊN."));
            }

            // ---- Spawner ----
            foreach (var s in spawners)
            {
                if (s.dotPrefab == null)
                    issues.Add(new LevelIssue(MessageType.Error,
                        $"Spawner '{s.name}' chưa gán dotPrefab.", s));
                if (s.fixedColorId >= 0 && level.fruitDatabase != null &&
                    level.fruitDatabase.GetById(s.fixedColorId) == null)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Spawner '{s.name}' có fixedColorId={s.fixedColorId} không tồn tại trong FruitDatabase.", s));
            }

            // ---- Column rỗng ----
            foreach (var col in columns)
                if (col.GetComponentsInChildren<ModelDotSpawner>(true).Length == 0)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Column '{col.name}' không có spawner con nào.", col));

            // ---- Bucket: màu hợp lệ + có conveyor cấp dot ----
            foreach (var b in buckets)
            {
                if (level.fruitDatabase != null && level.fruitDatabase.GetById(b.colorId) == null)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Bucket '{b.name}' có colorId={b.colorId} không tồn tại trong FruitDatabase.", b));

                if (conveyors.Length > 0 && b.InputConveyor == null)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Bucket '{b.name}' chưa liên kết Input Conveyor nên sẽ không nhận dot.", b));
                else if (b.InputConveyor != null &&
                         System.Array.IndexOf(conveyors, b.InputConveyor) < 0)
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Bucket '{b.name}' đang liên kết conveyor không thuộc level hiện tại.", b));
            }

            // ---- Cân đối cung/cầu dot theo màu ----
            CheckSupplyDemand(buckets, spawners, issues);

            return issues;
        }

        static int CountValidNext(ConveyorConnections conn)
        {
            int n = 0;
            for (int i = 0; i < conn.next.Count; i++)
                if (conn.next[i] != null) n++;
            return n;
        }

        static void CheckSupplyDemand(Bucket[] buckets, ModelDotSpawner[] spawners, List<LevelIssue> issues)
        {
            var demand = new Dictionary<int, int>(); // colorId -> tổng maxFill
            foreach (var b in buckets)
                demand[b.colorId] = (demand.TryGetValue(b.colorId, out int d) ? d : 0) + b.maxFill;

            var supply = new Dictionary<int, int>(); // colorId -> tổng totalClicks (fixed)
            int wildcard = 0;                        // spawner random (fixedColorId = -1)
            foreach (var s in spawners)
            {
                if (s.fixedColorId >= 0)
                    supply[s.fixedColorId] = (supply.TryGetValue(s.fixedColorId, out int v) ? v : 0) + s.totalClicks;
                else
                    wildcard += s.totalClicks;
            }

            foreach (var kv in demand)
            {
                supply.TryGetValue(kv.Key, out int have);
                int missing = kv.Value - have;
                if (missing <= 0) continue;

                if (wildcard > 0)
                    issues.Add(new LevelIssue(MessageType.Info,
                        $"Màu {kv.Key}: bucket cần {kv.Value} dot, spawner cố định màu chỉ có {have} — " +
                        $"phần thiếu phụ thuộc spawner random ({wildcard} dot random tổng)."));
                else
                    issues.Add(new LevelIssue(MessageType.Warning,
                        $"Màu {kv.Key}: bucket cần {kv.Value} dot nhưng spawner chỉ cung cấp {have} — level không thể hoàn thành."));
            }
        }
    }
}
#endif
