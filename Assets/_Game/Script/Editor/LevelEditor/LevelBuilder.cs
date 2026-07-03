using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FruitSort
{
    /// <summary>
    /// Dựng level từ <see cref="LevelData"/>. Dùng CHUNG cho runtime (LevelLoader) và
    /// editor (LevelEditorWindow) để scene dựng ra lúc thiết kế và lúc chơi giống hệt nhau.
    ///
    /// Trong editor (không play) prefab được instantiate qua PrefabUtility để giữ liên kết
    /// prefab; runtime dùng Object.Instantiate bình thường.
    /// Bucket/ConveyorSpline tự register vào FallingPixelManager trong OnEnable của chúng,
    /// nên builder chỉ cần instantiate + set field, không cần đăng ký tay.
    /// </summary>
    public static class LevelBuilder
    {
        public const string RootName = "_Level";

        /// <summary>Dựng toàn bộ level, trả về GameObject root (null nếu data null).</summary>
        public static GameObject Build(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("[LevelBuilder] LevelData null, không dựng được level.");
                return null;
            }

            var root = new GameObject($"{RootName}_{data.name}");

            // ---- Băng chuyền (dựng trước để link) ----
            var conveyors = new List<ConveyorSpline>(data.conveyors.Count);
            for (int i = 0; i < data.conveyors.Count; i++)
                conveyors.Add(BuildConveyor(data, data.conveyors[i], i, root.transform));

            foreach (var link in data.conveyorLinks)
            {
                if (link.from < 0 || link.from >= conveyors.Count ||
                    link.to < 0 || link.to >= conveyors.Count ||
                    conveyors[link.from] == null || conveyors[link.to] == null)
                    continue;
                var conn = conveyors[link.from].GetComponent<ConveyorConnections>();
                if (conn != null && !conn.next.Contains(conveyors[link.to]))
                    conn.next.Add(conveyors[link.to]);
            }

            // ---- Bucket ----
            if (data.buckets.Count > 0 && data.bucketPrefab == null)
                Debug.LogError("[LevelBuilder] Level có bucket nhưng chưa gán bucketPrefab.", data);
            else
                for (int i = 0; i < data.buckets.Count; i++)
                    BuildBucket(data, data.buckets[i], i, root.transform);

            // ---- Spawner đứng riêng ----
            if (data.spawners.Count > 0 && data.spawnerPrefab == null)
                Debug.LogError("[LevelBuilder] Level có spawner nhưng chưa gán spawnerPrefab.", data);
            else
                for (int i = 0; i < data.spawners.Count; i++)
                {
                    var sd = data.spawners[i];
                    var go = Spawn(data.spawnerPrefab.gameObject, root.transform);
                    go.name = $"Spawner_{i}" + (sd.fixedColorId >= 0 ? $"_c{sd.fixedColorId}" : "_random");
                    go.transform.position = sd.position;
                    ConfigureSpawner(go.GetComponent<ModelDotSpawner>(), sd, data);
                }

            // ---- Cột spawner ----
            for (int i = 0; i < data.columns.Count; i++)
                BuildColumn(data, data.columns[i], i, root.transform);

            return root;
        }

        static ConveyorSpline BuildConveyor(LevelData data, LevelData.ConveyorData cd, int index, Transform parent)
        {
            GameObject go;
            if (data.conveyorPrefab != null)
            {
                go = Spawn(data.conveyorPrefab.gameObject, parent);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go = new GameObject();
                go.transform.SetParent(parent, false);
            }
            go.name = string.IsNullOrEmpty(cd.name) ? $"Conveyor_{index}" : cd.name;

            var container = go.GetComponent<SplineContainer>();
            if (container == null) container = go.AddComponent<SplineContainer>();
            var spline = container.Spline;
            spline.Clear();
            foreach (var w in cd.knots)
                spline.Add(new BezierKnot((float3)go.transform.InverseTransformPoint(w)), TangentMode.AutoSmooth);
            spline.Closed = cd.closed;

            var conv = go.GetComponent<ConveyorSpline>();
            if (conv == null) conv = go.AddComponent<ConveyorSpline>();
            conv.beltWidth = cd.beltWidth;
            conv.straightEdges = cd.straightEdges;
            conv.cornerRadius = cd.cornerRadius;
            conv.cornerSegments = cd.cornerSegments;

            if (go.GetComponent<ConveyorConnections>() == null)
                go.AddComponent<ConveyorConnections>();

            conv.Bake();
            return conv;
        }

        static void BuildBucket(LevelData data, LevelData.BucketData bd, int index, Transform parent)
        {
            var go = Spawn(data.bucketPrefab.gameObject, parent);
            go.name = $"Bucket_{index}_c{bd.colorId}";
            go.transform.position = bd.position;

            var b = go.GetComponent<Bucket>();
            if (b == null)
            {
                Debug.LogError("[LevelBuilder] bucketPrefab không có component Bucket.", data);
                return;
            }
            b.colorId = bd.colorId;
            b.maxFill = bd.maxFill;
            if (b.fruitDatabase == null) b.fruitDatabase = data.fruitDatabase;
            b.RefreshVisuals();
        }

        static void BuildColumn(LevelData data, LevelData.ColumnData cd, int index, Transform parent)
        {
            GameObject go;
            if (data.columnPrefab != null)
            {
                go = Spawn(data.columnPrefab.gameObject, parent);
                // Con mặc định trong prefab sẽ được thay bằng danh sách trong data.
                PruneChildSpawners(go);
            }
            else
            {
                go = new GameObject();
                go.transform.SetParent(parent, false);
                go.AddComponent<ModelDotSpawnerColumn>();
            }
            go.name = $"SpawnerColumn_{index}";
            go.transform.position = cd.position;

            var col = go.GetComponent<ModelDotSpawnerColumn>();
            if (col != null) col.clickAnywhereInColumn = cd.clickAnywhereInColumn;

            if (cd.spawners.Count > 0 && data.spawnerPrefab == null)
            {
                Debug.LogError("[LevelBuilder] Column có spawner nhưng chưa gán spawnerPrefab.", data);
            }
            else
            {
                for (int i = 0; i < cd.spawners.Count; i++)
                {
                    var sd = cd.spawners[i];
                    var sGo = Spawn(data.spawnerPrefab.gameObject, go.transform);
                    sGo.name = $"Spawner_{i}" + (sd.fixedColorId >= 0 ? $"_c{sd.fixedColorId}" : "_random");
                    sGo.transform.localPosition = sd.position; // LOCAL trong column
                    ConfigureSpawner(sGo.GetComponent<ModelDotSpawner>(), sd, data);
                }
            }

            if (col != null) col.Collect();
        }

        static void ConfigureSpawner(ModelDotSpawner s, LevelData.SpawnerData sd, LevelData data)
        {
            if (s == null)
            {
                Debug.LogError("[LevelBuilder] spawnerPrefab không có component ModelDotSpawner.", data);
                return;
            }
            s.fixedColorId = sd.fixedColorId;
            s.totalClicks = sd.totalDots;
            s.spawnCount = sd.spawnCount;
            if (s.fruitDatabase == null) s.fruitDatabase = data.fruitDatabase;
        }

        /// <summary>Xoá mọi ModelDotSpawner con có sẵn trong prefab column (data sẽ thay thế).</summary>
        static void PruneChildSpawners(GameObject columnGo)
        {
            var kids = columnGo.GetComponentsInChildren<ModelDotSpawner>(true);
            if (kids.Length == 0) return;

#if UNITY_EDITOR
            // Editor (không play): không được xoá con của prefab instance -> unpack trước.
            if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabInstance(columnGo))
                PrefabUtility.UnpackPrefabInstance(columnGo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
#endif
            foreach (var k in kids)
            {
                if (k == null) continue;
                if (Application.isPlaying) Object.Destroy(k.gameObject);
                else Object.DestroyImmediate(k.gameObject);
            }
        }

        /// <summary>Instantiate prefab: editor giữ liên kết prefab, runtime Instantiate thường.</summary>
        static GameObject Spawn(GameObject prefab, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var inst = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                if (inst != null) return inst;
            }
#endif
            return Object.Instantiate(prefab, parent);
        }
    }
}