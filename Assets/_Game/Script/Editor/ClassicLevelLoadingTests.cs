using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class ClassicLevelLoadingTests
    {
        const string SharedLevelPrefabPath = "Assets/Game/Resources/Levels/Level_1.prefab";
        const string ConveyorPrefabPath = "Assets/_Game/Prefabs/Conveyor.prefab";
        const string SpawnerPrefabPath = "Assets/_Game/Prefabs/FruitSpawn.prefab";

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(99)]
        public void LoadLevelOption_AlwaysUsesSharedClassicPrefab(int level)
        {
            LoadLevelOption option = LoadLevelOption.Create(level);

            Assert.That(option.Path, Is.EqualTo("Levels/Level_1"));
            Assert.That(option.Level, Is.EqualTo(level));
        }

        [Test]
        public void SharedPrefab_OwnsLevelDataWithoutGameFlowManager()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedLevelPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            ClassicLevelController controller = prefab.GetComponent<ClassicLevelController>();
            Assert.That(controller, Is.Not.Null);

            var serializedController = new SerializedObject(controller);
            SerializedProperty levels = serializedController.FindProperty("levels");
            Assert.That(levels, Is.Not.Null);
            Assert.That(levels.arraySize, Is.EqualTo(5));
            for (int i = 0; i < levels.arraySize; i++)
                Assert.That(levels.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);

            Assert.That(prefab.transform.Find("GameFlowManager"), Is.Null);
        }

        [Test]
        public void Controller_ResolvesOneBasedLevelData()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedLevelPrefabPath);
            ClassicLevelController controller = prefab.GetComponent<ClassicLevelController>();

            Assert.That(controller.GetLevelData(1).name, Is.EqualTo("Level_01"));
            Assert.That(controller.GetLevelData(5).name, Is.EqualTo("Level_05"));
            Assert.That(controller.GetLevelData(0), Is.Null);
            Assert.That(controller.GetLevelData(6), Is.Null);
        }

        [Test]
        public void LevelBuilder_RebuildsConveyorMeshAndRestoresPrefabMaterials()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            ConveyorSpline conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            ConveyorBeltRenderer prefabRenderer = conveyorAsset.GetComponent<ConveyorBeltRenderer>();
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorPrefab;
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Runtime Conveyor",
                beltWidth = 1f,
                closed = true,
                straightEdges = true,
                cornerRadius = 0f,
                cornerSegments = 1,
                knots = new System.Collections.Generic.List<Vector3>
                {
                    new Vector3(-4f, -1f),
                    new Vector3(4f, -1f),
                    new Vector3(4f, 1f),
                    new Vector3(-4f, 1f),
                },
            });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSpline conveyor = root.GetComponentInChildren<ConveyorSpline>();
                ConveyorBeltRenderer renderer = conveyor.GetComponent<ConveyorBeltRenderer>();
                MeshFilter filter = conveyor.GetComponent<MeshFilter>();

                Vector3 expected = conveyor.GetPositionOnSpline(0f, conveyor.HalfWidth);
                expected.z += renderer.zOffset;
                Vector3 actual = renderer.transform.TransformPoint(filter.sharedMesh.vertices[0]);

                Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f));
                Assert.That(renderer.GetComponent<MeshRenderer>().sharedMaterials[0],
                    Is.SameAs(prefabRenderer.beltMaterial));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void LevelBuilder_RestoresSavedWallConfiguration()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Walled Conveyor",
                beltWidth = 1f,
                hasRendererSettings = true,
                showWalls = true,
                wallWidth = 0.35f,
                wallZOffset = -0.04f,
                scrollWalls = true,
                rendererSegments = 18,
                renderConnections = false,
                scrollSpeed = 1.25f,
                knots = new System.Collections.Generic.List<Vector3>
                {
                    Vector3.zero,
                    Vector3.right * 3f,
                },
            });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer renderer = root.GetComponentInChildren<ConveyorBeltRenderer>();
                Assert.That(renderer.showWalls, Is.True);
                Assert.That(renderer.wallWidth, Is.EqualTo(0.35f));
                Assert.That(renderer.wallZOffset, Is.EqualTo(-0.04f));
                Assert.That(renderer.scrollWalls, Is.True);
                Assert.That(renderer.segments, Is.EqualTo(18));
                Assert.That(renderer.renderConnections, Is.False);
                Assert.That(renderer.scrollSpeed, Is.EqualTo(1.25f));
                Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void LevelBuilder_RestoresSavedConveyorSwitchConfiguration()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Switch Source",
                hasSwitch = true,
                hasSwitchSettings = true,
                switchClickRadius = 2.5f,
                switchLinkedSpriteSize = 0.7f,
                switchUseCombinedRenderer = false,
                switchCombinedRendererZOffset = -0.06f,
                switchActiveBranchOwnedKnotCount = 3,
                knots = new System.Collections.Generic.List<Vector3>
                {
                    Vector3.left * 2f,
                    Vector3.zero,
                },
            });
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Switch Target",
                knots = new System.Collections.Generic.List<Vector3>
                {
                    Vector3.zero,
                    Vector3.up * 2f,
                },
            });
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 1 });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Switch Source")
                    .GetComponent<ConveyorSwitch>();
                Assert.That(routeSwitch, Is.Not.Null);
                Assert.That(routeSwitch.clickRadius, Is.EqualTo(2.5f));
                Assert.That(routeSwitch.linkedSpriteSize, Is.EqualTo(0.7f));
                Assert.That(routeSwitch.useCombinedRenderer, Is.False);
                Assert.That(routeSwitch.combinedRendererZOffset, Is.EqualTo(-0.06f));
                Assert.That(routeSwitch.activeBranchOwnedKnotCount, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void LevelBuilder_ConfiguresSpawnerAsSquareWithoutColorIdSprite()
        {
            GameObject spawnerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnerPrefabPath);
            ModelDotSpawner spawnerPrefab = spawnerAsset.GetComponent<ModelDotSpawner>();
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.spawnerPrefab = spawnerPrefab;
            data.fruitDatabase = spawnerPrefab.fruitDatabase;
            data.spawners.Add(new LevelData.SpawnerData
            {
                fixedColorId = 0,
                totalDots = 5,
                spawnCount = 1,
                launchDirection = Vector2.down,
                launchSpeed = 10f,
            });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ModelDotSpawner spawner = root.GetComponentInChildren<ModelDotSpawner>();

                Assert.That(spawner.packageSprite.sprite, Is.SameAs(spawner.dotPrefab.Sr.sprite));
                Assert.That(spawner.TryResolveSpawnAppearance(out int colorId, out _, out Sprite sprite), Is.True);
                Assert.That(colorId, Is.EqualTo(0));
                Assert.That(sprite, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }
    }
}
