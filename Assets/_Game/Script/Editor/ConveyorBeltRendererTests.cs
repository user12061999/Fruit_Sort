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

                Assert.That(mesh.vertexCount, Is.GreaterThan(baseVertexCount),
                    "A multi-link source needs extra junction geometry beyond its normal belt strips.");
                Assert.That(mesh.vertexCount, Is.EqualTo(baseVertexCount + 8),
                    "A three-way rule tile is one square belt face plus one closed wall side, not a radial disk.");

                Vector3 endpoint = sourceTransform.GetComponent<ConveyorSpline>()
                    .GetPositionOnSpline(1f, 0f);
                Vector3 tileCenter = mesh.vertices
                    .Skip(baseVertexCount)
                    .Take(4)
                    .Select(sourceTransform.TransformPoint)
                    .Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) / 4f;

                Assert.That(Vector2.Distance(tileCenter, endpoint), Is.LessThan(0.001f),
                    "The square rule tile must be centered on the shared linked endpoint.");
                Assert.That(mesh.normals.All(normal => normal.z < -0.99f), Is.True,
                    "Junction disk and border triangles must face the same side as the belt strips.");

                ConveyorSpline source = sourceTransform.GetComponent<ConveyorSpline>();
                ConveyorConnections connections = sourceTransform.GetComponent<ConveyorConnections>();
                var armDirections = new List<Vector2> { -source.GetTangent(1f) };
                var armHalfWidths = new List<float> { source.HalfWidth };
                foreach (ConveyorSpline target in connections.next)
                {
                    armDirections.Add(target.GetTangent(0f));
                    armHalfWidths.Add(target.HalfWidth);
                }

                int[] outerTriangles = mesh.GetTriangles(1);
                int junctionWallTriangleCount = 0;
                Vector2 expectedClosedSide = source.GetTangent(1f).normalized;
                for (int i = 0; i < outerTriangles.Length; i += 3)
                {
                    if (outerTriangles[i] < baseVertexCount && outerTriangles[i + 1] < baseVertexCount &&
                        outerTriangles[i + 2] < baseVertexCount)
                        continue;

                    junctionWallTriangleCount++;

                    Vector3 centroid = sourceTransform.TransformPoint(
                        (mesh.vertices[outerTriangles[i]] + mesh.vertices[outerTriangles[i + 1]] +
                         mesh.vertices[outerTriangles[i + 2]]) / 3f);
                    Vector2 relative = centroid - endpoint;
                    float closedProjection = Vector2.Dot(relative, expectedClosedSide);
                    float closedLateralDistance = Mathf.Abs(
                        expectedClosedSide.x * relative.y - expectedClosedSide.y * relative.x);
                    Assert.That(closedProjection, Is.GreaterThan(closedLateralDistance),
                        "The rule tile must close the side opposite the incoming conveyor arm.");
                    for (int arm = 0; arm < armDirections.Count; arm++)
                    {
                        Vector2 direction = armDirections[arm].normalized;
                        if (Vector2.Dot(relative, direction) <= 0f) continue;

                        float perpendicularDistance = Mathf.Abs(
                            direction.x * relative.y - direction.y * relative.x);
                        Assert.That(perpendicularDistance,
                            Is.GreaterThanOrEqualTo(armHalfWidths[arm] - 0.03f),
                            "Junction border geometry must leave every linked belt lane open.");
                    }
                }
                Assert.That(junctionWallTriangleCount, Is.EqualTo(2),
                    "A three-way rule tile has one closed wall quad.");
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
