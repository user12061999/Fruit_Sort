#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FruitSort
{
    /// <summary>Creates the twenty 100-dot levels from the approved level-design brief.</summary>
    public static class LevelDesign100DotGenerator
    {
        const string OutputFolder = "Assets/_Game/Levels";
        const string RequestMarker = "Assets/_Game/LevelDesign100Dot.generate";

        public sealed class Definition
        {
            public int Id;
            public string Name;
            public int[] ColorIds;
            public int[] Batches;
            public int[] BucketCapacities;
            public int MoveLimit;
            public float TimeLimit;
            public int RequiredReleases;
            public int TotalSupply
            {
                get { return ColorIds.Length * 100; }
            }
            public int TotalRequiredCapacity
            {
                get
                {
                    int total = 0;
                    for (int i = 0; i < BucketCapacities.Length; i++) total += BucketCapacities[i];
                    return total;
                }
            }
            public int MinimumMoves
            {
                get
                {
                    int moves = RequiredReleases;
                    for (int i = 0; i < Batches.Length; i++) moves += Mathf.CeilToInt(100f / Batches[i]);
                    return moves;
                }
            }
        }

        [MenuItem("Tools/FruitSort/Generate 20 100-Dot Levels")]
        public static void GenerateAssets()
        {
            EnsureOutputFolder();
            Bucket bucketPrefab = LoadRequired<Bucket>("Assets/_Game/Prefabs/Cage.prefab");
            ModelDotSpawner spawnerPrefab = LoadRequired<ModelDotSpawner>("Assets/_Game/Prefabs/FruitSpawn.prefab");
            ModelDotSpawnerColumn columnPrefab = LoadRequired<ModelDotSpawnerColumn>("Assets/_Game/Prefabs/FruitSpawnColumn.prefab");
            ConveyorSpline conveyorPrefab = LoadRequired<ConveyorSpline>("Assets/_Game/Prefabs/Conveyor.prefab");
            FruitDatabase fruitDatabase = LoadRequired<FruitDatabase>("Assets/_Game/Data/FruitDatabase.asset");

            foreach (Definition definition in CreateDefinitions())
            {
                Validate(definition);
                string path = string.Format("{0}/Level_{1:00}.asset", OutputFolder, definition.Id);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level == null)
                {
                    level = ScriptableObject.CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(level, path);
                }

                level.bucketPrefab = bucketPrefab;
                level.spawnerPrefab = spawnerPrefab;
                level.columnPrefab = columnPrefab;
                level.conveyorPrefab = conveyorPrefab;
                level.fruitDatabase = fruitDatabase;
                level.moveLimit = definition.MoveLimit;
                level.timeLimit = definition.TimeLimit;
                level.buckets.Clear();
                level.spawners.Clear();
                level.columns.Clear();
                level.conveyors.Clear();
                level.conveyorLinks.Clear();
                level.obstacles.Clear();
                level.grinders.Clear();

                AddSpawners(level, definition);
                AddConveyorsAndBuckets(level, definition);
                EditorUtility.SetDirty(level);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelDesign100DotGenerator] Generated Level_01 through Level_20.");
        }

        [InitializeOnLoadMethod]
        static void GenerateFromRequestMarker()
        {
            EditorApplication.delayCall += () =>
            {
                if (!System.IO.File.Exists(RequestMarker)) return;
                try { GenerateAssets(); }
                finally { AssetDatabase.DeleteAsset(RequestMarker); }
            };
        }

        public static List<Definition> CreateDefinitions()
        {
            return new List<Definition>
            {
                D(1, "First Hundred", new[] { 0 }, new[] { 20 }, new[] { 100 }, 7, 0),
                D(2, "Ten Small Batches", new[] { 0 }, new[] { 10 }, new[] { 100 }, 12, 0),
                D(3, "First Switch", new[] { 0, 1 }, new[] { 20, 20 }, new[] { 100, 100 }, 13, 0),
                D(4, "Large Batch Routing", new[] { 0, 1 }, new[] { 25, 25 }, new[] { 100, 100 }, 10, 90),
                D(5, "Three Colors", new[] { 0, 1, 2 }, new[] { 20, 20, 20 }, new[] { 100, 100, 100 }, 18, 120),
                D(6, "Fast Routing", new[] { 0, 1, 2 }, new[] { 25, 25, 25 }, new[] { 100, 100, 100 }, 14, 100),
                D(7, "Two Buckets Per Color", new[] { 0, 1 }, new[] { 10, 10 }, new[] { 50, 50, 50, 50 }, 23, 150),
                D(8, "Four-Way Tree", new[] { 0, 1, 2, 3 }, new[] { 20, 20, 20, 20 }, new[] { 100, 100, 100, 100 }, 23, 150),
                D(9, "First Release", new[] { 0, 1 }, new[] { 20, 20 }, new[] { 100, 100 }, 14, 150, 1),
                D(10, "Two Entry Lanes", new[] { 0, 1, 2 }, new[] { 25, 25, 25 }, new[] { 100, 100, 100 }, 15, 130),
                D(11, "Rapid Dispatch", new[] { 0, 1, 2 }, new[] { 10, 10, 10 }, new[] { 100, 100, 100 }, 32, 150),
                D(12, "Large Batch Precision", new[] { 0, 1, 2, 3 }, new[] { 25, 25, 25, 25 }, new[] { 100, 100, 100, 100 }, 18, 130),
                D(13, "Split Red Target", new[] { 0, 1, 3 }, new[] { 10, 20, 20 }, new[] { 50, 50, 100, 100 }, 23, 155),
                D(14, "Dual-Stage Routing", new[] { 0, 1, 2, 3 }, new[] { 25, 25, 25, 25 }, new[] { 100, 100, 100, 100 }, 18, 125),
                D(15, "Double Recovery", new[] { 0, 1, 2, 3 }, new[] { 20, 20, 20, 20 }, new[] { 100, 100, 100, 100 }, 25, 210, 2),
                D(16, "Five Colors", new[] { 0, 1, 2, 3, 4 }, new[] { 20, 20, 20, 20, 20 }, new[] { 100, 100, 100, 100, 100 }, 28, 210),
                D(17, "Mixed Batch Sizes", new[] { 0, 1, 2, 3, 4 }, new[] { 25, 20, 10, 25, 20 }, new[] { 100, 100, 100, 100, 100 }, 31, 230),
                D(18, "Distribution Network", new[] { 0, 1, 2, 3 }, new[] { 10, 20, 20, 10 }, new[] { 50, 50, 100, 50, 50, 100 }, 33, 240),
                D(19, "Precision Recovery", new[] { 0, 1, 2, 3, 4 }, new[] { 25, 25, 25, 25, 25 }, new[] { 100, 100, 100, 100, 100 }, 24, 225, 2),
                D(20, "Final Sorting Plant", new[] { 0, 1, 2, 3, 4 }, new[] { 20, 20, 20, 20, 20 }, new[] { 60, 40, 60, 40, 100, 100, 100 }, 29, 270, 2),
            };
        }

        static Definition D(int id, string name, int[] colors, int[] batches, int[] capacities, int moves, float timer, int releases = 0)
        {
            return new Definition { Id = id, Name = name, ColorIds = colors, Batches = batches, BucketCapacities = capacities, MoveLimit = moves, TimeLimit = timer, RequiredReleases = releases };
        }

        static void AddSpawners(LevelData level, Definition definition)
        {
            for (int i = 0; i < definition.ColorIds.Length; i++)
            {
                level.spawners.Add(new LevelData.SpawnerData
                {
                    position = new Vector3(-8f, 3.5f - i * 1.6f, 0f),
                    fixedColorId = definition.ColorIds[i], totalDots = 100, spawnCount = definition.Batches[i],
                    launchDirection = Vector2.right, launchSpeed = 12f
                });
            }
        }

        static void AddConveyorsAndBuckets(LevelData level, Definition definition)
        {
            AddConveyor(level, "Input", false, new Vector3(-8f, 0f), new Vector3(-1f, 0f));
            int capacityIndex = 0;
            int bucketIndex = 0;
            for (int colorIndex = 0; colorIndex < definition.ColorIds.Length; colorIndex++)
            {
                int capacityCount = CountCapacitySlots(definition, capacityIndex);
                for (int slot = 0; slot < capacityCount; slot++)
                {
                    float y = 4.5f - bucketIndex * (9f / Mathf.Max(1, definition.BucketCapacities.Length - 1));
                    int branch = level.conveyors.Count;
                    AddConveyor(level, "Route_" + bucketIndex, false, new Vector3(-1f, 0f), new Vector3(5f, y));
                    level.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = branch });
                    level.buckets.Add(new LevelData.BucketData
                    {
                        position = new Vector3(5f, y, 0f), colorId = definition.ColorIds[colorIndex],
                        maxFill = definition.BucketCapacities[capacityIndex++], launchDirection = Vector2.left
                    });
                    bucketIndex++;
                }
            }
            level.conveyors[0].hasSwitch = level.conveyorLinks.Count > 1;
            level.conveyors[0].hasSwitchSettings = level.conveyors[0].hasSwitch;
        }

        static int CountCapacitySlots(Definition definition, int startIndex)
        {
            int slots = 0;
            int capacity = 0;
            while (capacity < 100 && startIndex + slots < definition.BucketCapacities.Length)
                capacity += definition.BucketCapacities[startIndex + slots++];
            if (capacity != 100)
                throw new InvalidOperationException(definition.Name + " must allocate exactly 100 capacity per color.");
            return slots;
        }

        static void AddConveyor(LevelData level, string name, bool hasSwitch, Vector3 start, Vector3 end)
        {
            level.conveyors.Add(new LevelData.ConveyorData
            {
                name = name, beltWidth = 2.5f, hasSwitch = hasSwitch, hasSwitchSettings = hasSwitch,
                knots = new List<Vector3> { start, end }
            });
        }

        static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Missing required asset: " + path);
            return asset;
        }

        static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/_Game", "Levels");
        }

        static void Validate(Definition definition)
        {
            if (definition.TotalSupply != definition.TotalRequiredCapacity)
                throw new InvalidOperationException(definition.Name + " has unbalanced supply and capacity.");
            if (definition.MoveLimit < definition.MinimumMoves)
                throw new InvalidOperationException(definition.Name + " has a move limit below its minimum solution.");
        }
    }
}
#endif
