using System.Collections.Generic;
using System.Reflection;
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
                renderer.showWalls = true;
                renderer.BuildMesh();
                Mesh mesh = sourceTransform.GetComponent<MeshFilter>().sharedMesh;
                const int stripCount = 3;
                int baseVertexCount = (Mathf.Max(2, renderer.segments) + 1) * 2 * stripCount;
                int baseTriangleIndexCount = Mathf.Max(2, renderer.segments) * 6;

                Assert.That(mesh.subMeshCount, Is.EqualTo(3));
                Assert.That(mesh.vertexCount, Is.GreaterThan(baseVertexCount),
                    "Outgoing branches must append connector geometry after the source strip.");
                for (int subMesh = 0; subMesh < 3; subMesh++)
                {
                    Assert.That(mesh.GetTriangles(subMesh).Length,
                        Is.GreaterThan(baseTriangleIndexCount),
                        $"Connector submesh {subMesh} must include the belt/rail for every branch.");
                }
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
                renderer.showWalls = true;
                renderer.BuildMesh();
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                const int stripCount = 3;
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

        [Test]
        public void ConveyorSwitch_ClickAtJunctionDoesNotChangeActiveBranch()
        {
            LevelData data = CreateSwitchLevelData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Belt_Source").GetComponent<ConveyorSwitch>();

                bool switched = routeSwitch.TrySwitchToBranchAtWorldPosition(routeSwitch.SwitchPosition);

                Assert.That(switched, Is.False);
                Assert.That(routeSwitch.ActiveIndex, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_ClickingInactiveBranchSelectsThatBranch()
        {
            LevelData data = CreateSwitchLevelData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Belt_Source").GetComponent<ConveyorSwitch>();
                ConveyorSpline downBranch = root.transform.Find("Belt_Down").GetComponent<ConveyorSpline>();
                Vector3 clickPosition = downBranch.GetPositionOnSpline(0.5f, 0f);

                bool switched = routeSwitch.TrySwitchToBranchAtWorldPosition(clickPosition);

                Assert.That(switched, Is.True);
                Assert.That(routeSwitch.ActiveIndex, Is.EqualTo(1));
                Assert.That(routeSwitch.TryGetActiveNext(out ConveyorSpline active), Is.True);
                Assert.That(active, Is.SameAs(downBranch));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_ActiveBranchOwnershipCoversTwoDots()
        {
            LevelData data = CreateSwitchLevelData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Belt_Source").GetComponent<ConveyorSwitch>();
                ConveyorSpline activeBranch = root.transform.Find("Belt_Up").GetComponent<ConveyorSpline>();

                float ownedProgress = routeSwitch.GetActiveBranchOwnedProgress(activeBranch);

                Assert.That(ownedProgress, Is.EqualTo(1f).Within(0.001f),
                    "A branch with two spline knots should be owned through knot 1, not by gameplay-dot size.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_KnotOwnershipPassesEntireRoundedCornerAtKnotOne()
        {
            LevelData data = CreateSwitchLevelData();
            data.conveyors[1].cornerRadius = 0.5f;
            data.conveyors[1].cornerSegments = 8;
            data.conveyors[1].knots = new List<Vector3>
            {
                Vector3.zero,
                new Vector3(2f, 0f),
                new Vector3(2f, 2f),
            };

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Belt_Source").GetComponent<ConveyorSwitch>();
                ConveyorSpline branch = root.transform.Find("Belt_Up").GetComponent<ConveyorSpline>();

                float ownedProgress = routeSwitch.GetActiveBranchOwnedProgress(branch);
                Vector3 ownedEnd = branch.GetPositionOnSpline(ownedProgress, 0f);

                Assert.That(ownedEnd.x, Is.EqualTo(2f).Within(0.08f));
                Assert.That(ownedEnd.y, Is.GreaterThan(0.2f),
                    "Ownership through knot 1 must finish after its rounded corner on the outgoing segment.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_InactiveBranchZOffsetsMeshVerticesWithoutMovingSplineTransform()
        {
            LevelData data = CreateSwitchLevelData();
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            data.conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer renderer = root.transform.Find("Belt_Down")
                    .GetComponent<ConveyorBeltRenderer>();
                float transformZ = renderer.transform.position.z;
                float baseMeshZ = renderer.GetComponent<MeshFilter>().sharedMesh.bounds.center.z;

                renderer.SetRuntimeVisualZOffset(-0.35f);

                float shiftedMeshZ = renderer.GetComponent<MeshFilter>().sharedMesh.bounds.center.z;
                Assert.That(renderer.transform.position.z, Is.EqualTo(transformZ).Within(0.0001f));
                Assert.That(shiftedMeshZ, Is.EqualTo(baseMeshZ - 0.35f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_OnlyActiveBranchTrimsOwnedJunctionSegment()
        {
            LevelData data = CreateSwitchLevelData();
            data.conveyors[1].knots.Add(new Vector3(0f, 3f));
            data.conveyors[2].knots.Add(new Vector3(0f, -3f));
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            data.conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSpline source = root.transform.Find("Belt_Source").GetComponent<ConveyorSpline>();
                ConveyorSpline activeBranch = root.transform.Find("Belt_Up").GetComponent<ConveyorSpline>();
                ConveyorSpline inactiveBranch = root.transform.Find("Belt_Down").GetComponent<ConveyorSpline>();
                MethodInfo method = typeof(ConveyorBeltRenderer).GetMethod(
                    "GetSwitchBranchTrimProgress",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null);
                float activeTrim = (float)method.Invoke(null, new object[] { source, activeBranch });
                float inactiveTrim = (float)method.Invoke(null, new object[] { source, inactiveBranch });
                Assert.That(activeTrim, Is.GreaterThan(0f),
                    "The source must own knot 0 -> knot 1 of the active branch.");
                Assert.That(inactiveTrim, Is.EqualTo(0f).Within(0.0001f),
                    "The inactive branch must keep rendering knot 0 -> knot 1 behind the active route.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_UsesConnectionAwareRendererWithoutCombinedDuplicate()
        {
            LevelData data = CreateSwitchLevelData();
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            data.conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Belt_Source").GetComponent<ConveyorSwitch>();
                Assert.That(routeSwitch.useCombinedRenderer, Is.False,
                    "The connection-aware belt renderer must be the only switch visual so an inactive branch is not pushed behind a duplicate active mesh.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_IndicatorUsesSpriteInsteadOfArrowLine()
        {
            LevelData data = CreateSwitchLevelData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Belt_Source").GetComponent<ConveyorSwitch>();
                MethodInfo ensure = typeof(ConveyorSwitch).GetMethod(
                    "EnsureIndicator", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(ensure, Is.Not.Null);

                ensure.Invoke(routeSwitch, null);

                Transform indicator = routeSwitch.transform.Find("SwitchIndicator");
                Assert.That(indicator, Is.Not.Null);
                Assert.That(indicator.GetComponent<SpriteRenderer>(), Is.Not.Null);
                Assert.That(indicator.GetComponent<LineRenderer>(), Is.Null,
                    "The old arrow LineRenderer must be removed.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorSwitch_DestroyRemovesReparentedIndicator()
        {
            LevelData data = CreateSwitchLevelData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSwitch routeSwitch = root.transform.Find("Belt_Source").GetComponent<ConveyorSwitch>();
                MethodInfo ensure = typeof(ConveyorSwitch).GetMethod(
                    "EnsureIndicator", BindingFlags.Instance | BindingFlags.NonPublic);
                ensure.Invoke(routeSwitch, null);

                Transform indicator = routeSwitch.transform.Find("SwitchIndicator");
                Transform activeBranch = root.transform.Find("Belt_Up");
                indicator.SetParent(activeBranch, true);

                MethodInfo onDestroy = typeof(ConveyorSwitch).GetMethod(
                    "OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
                onDestroy.Invoke(routeSwitch, null);
                Object.DestroyImmediate(routeSwitch);

                Assert.That(indicator == null || !indicator.gameObject.activeSelf, Is.True,
                    "Destroying the switch must immediately hide and remove a marker reparented under its active branch.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConnectionLead_UsesClampedRoundedCornerInsteadOfConfiguredRadius()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            ConveyorSpline conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorPrefab;

            LevelData.ConveyorData source = CreateConveyor(
                "Belt_Source", new Vector3(-2.6f, 3f), new Vector3(2.6f, 4.1f));
            source.cornerRadius = 3f;
            source.cornerSegments = 12;
            source.knots = new List<Vector3>
            {
                new Vector3(-2.6f, 3f),
                new Vector3(2.6f, 3f),
                new Vector3(2.6f, 4.1f),
            };
            data.conveyors.Add(source);

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer renderer = root.transform.Find("Belt_Source")
                    .GetComponent<ConveyorBeltRenderer>();
                MethodInfo method = typeof(ConveyorBeltRenderer).GetMethod(
                    "GetEndpointStraightLead", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null);
                float lead = (float)method.Invoke(renderer,
                    new object[] { renderer.GetComponent<ConveyorSpline>(), false });

                Assert.That(lead, Is.EqualTo(0.55f).Within(0.06f),
                    "The endpoint lead must reserve the spline's clamped 0.55-unit fillet, not the configured radius 3.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ClosedConveyor_FirstAndLastStripEdgesMatchAtSeam()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            ConveyorSpline conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorPrefab;
            data.conveyors.Add(CreateClosedConveyor());

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer renderer = root.transform.Find("Belt_Closed")
                    .GetComponent<ConveyorBeltRenderer>();
                renderer.showWalls = true;
                renderer.segments = 48;
                renderer.BuildMesh();

                Vector3[] vertices = renderer.GetComponent<MeshFilter>().sharedMesh.vertices;
                int stripVertexCount = (Mathf.Max(2, renderer.segments) + 1) * 2;
                for (int strip = 0; strip < 3; strip++)
                {
                    int first = strip * stripVertexCount;
                    int last = first + stripVertexCount - 2;
                    Assert.That(Vector3.Distance(vertices[first], vertices[last]),
                        Is.LessThan(0.001f), $"Strip {strip} left edge must close without a gap.");
                    Assert.That(Vector3.Distance(vertices[first + 1], vertices[last + 1]),
                        Is.LessThan(0.001f), $"Strip {strip} right edge must close without a gap.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ClosedConveyor_StartAndEndTangentsMatch()
        {
            GameObject conveyorAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            ConveyorSpline conveyorPrefab = conveyorAsset.GetComponent<ConveyorSpline>();
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = conveyorPrefab;
            data.conveyors.Add(CreateClosedConveyor());

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSpline conveyor = root.transform.Find("Belt_Closed")
                    .GetComponent<ConveyorSpline>();

                Assert.That(conveyor.TrySampleCenterline(0f, out Vector3 start, out Vector3 startTangent),
                    Is.True);
                Assert.That(conveyor.TrySampleCenterline(1f, out Vector3 end, out Vector3 endTangent),
                    Is.True);
                Assert.That(Vector3.Distance(start, end), Is.LessThan(0.001f));
                Assert.That(Vector3.Dot(startTangent, endTangent), Is.GreaterThan(0.999f),
                    "Movement tangent must be continuous when progress wraps from 1 back to 0.");
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

        static LevelData CreateSwitchLevelData()
        {
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            LevelData.ConveyorData source = CreateConveyor("Belt_Source", new Vector3(-2f, 0f), Vector3.zero);
            source.hasSwitch = true;

            data.conveyors.Add(source);
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

        static LevelData.ConveyorData CreateClosedConveyor()
        {
            return new LevelData.ConveyorData
            {
                name = "Belt_Closed",
                beltWidth = 1.4f,
                closed = true,
                straightEdges = true,
                cornerRadius = 0.65f,
                cornerSegments = 8,
                rendererSegments = 48,
                knots = new List<Vector3>
                {
                    new Vector3(-3f, 1.5f),
                    new Vector3(3f, 1.5f),
                    new Vector3(3f, -1.5f),
                    new Vector3(-3f, -1.5f),
                },
            };
        }
    }
}
