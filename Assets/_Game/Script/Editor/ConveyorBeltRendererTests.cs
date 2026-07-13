using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class ConveyorBeltRendererTests
    {
        const string ConveyorPrefabPath = "Assets/_Game/Prefabs/Conveyor.prefab";

        [Test]
        public void LevelBuilder_MultiLinkSourceBuildsJunctionCoverAtSharedEndpoint()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            ConveyorSpline conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            LevelData data = CreateSplitJunctionData(conveyorPrefab);

            GameObject root = LevelBuilder.Build(data);
            try
            {
                Transform sourceTransform = root.transform.Find("Belt_Source");
                Assert.That(sourceTransform, Is.Not.Null);

                ConveyorBeltRenderer renderer = sourceTransform.GetComponent<ConveyorBeltRenderer>();
                Mesh mesh = sourceTransform.GetComponent<MeshFilter>().sharedMesh;
                int stripCount = renderer.showWalls ? 3 : 1;
                int baseVertexCount = (Mathf.Max(2, renderer.segments) + 1) * 2 * stripCount;
                const int smoothSegments = 12;
                const int stripsPerBranch = 3; // belt + two rails

                ConveyorSpline source = sourceTransform.GetComponent<ConveyorSpline>();
                ConveyorConnections connections = sourceTransform.GetComponent<ConveyorConnections>();
                int expectedExtraVertexCount = connections.next.Count * stripsPerBranch *
                    (smoothSegments + 1) * 2;

                Assert.That(mesh.vertexCount, Is.EqualTo(baseVertexCount + expectedExtraVertexCount),
                    "Each switch branch must be rendered as a smooth belt strip with two curved rails.");
                Assert.That(mesh.normals.All(normal => normal.z < -0.99f), Is.True,
                    "Smooth switch strips and rails must face the same side as the base conveyor.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void LevelBuilder_MultiIncomingTargetBuildsRuleTileAtSharedStart()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            ConveyorSpline conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorPrefab;
            data.conveyors.Add(CreateConveyor("Belt_Left", new Vector3(-2f, 0f), Vector3.zero));
            data.conveyors.Add(CreateConveyor("Belt_Down", new Vector3(0f, -2f), Vector3.zero));
            data.conveyors.Add(CreateConveyor("Belt_Target", Vector3.zero, new Vector3(0f, 2f)));
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 2 });
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 1, to = 2 });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer renderer = root.transform.Find("Belt_Target")
                    .GetComponent<ConveyorBeltRenderer>();
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                int stripCount = renderer.showWalls ? 3 : 1;
                int baseVertexCount = (Mathf.Max(2, renderer.segments) + 1) * 2 * stripCount;

                Assert.That(mesh.vertexCount, Is.GreaterThan(baseVertexCount),
                    "A multi-incoming conveyor must render a rule tile instead of overlapping start rails.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        static LevelData CreateSplitJunctionData(ConveyorSpline conveyorPrefab)
        {
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorPrefab;
            data.conveyors.Add(CreateConveyor("Belt_Source", new Vector3(-2f, 0f), Vector3.zero));
            data.conveyors.Add(CreateConveyor("Belt_Up", Vector3.zero, new Vector3(0f, 2f)));
            data.conveyors.Add(CreateConveyor("Belt_Down", Vector3.zero, new Vector3(0f, -2f)));
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 1 });
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 2 });
            return data;
        }

        static LevelData.ConveyorData CreateConveyor(string name, Vector3 start, Vector3 end)
        {
            return new LevelData.ConveyorData
            {
                name = name,
                beltWidth = 1f,
                straightEdges = true,
                cornerRadius = 0f,
                cornerSegments = 1,
                knots = new List<Vector3> { start, end },
            };
        }
    }
}
