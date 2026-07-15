using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class ConveyorConnectionContinuityTests
    {
        const string ConveyorPrefabPath = "Assets/_Game/Prefabs/Conveyor.prefab";

        [Test]
        public void ConnectionRoute_StartAndEndMatchSourceAndTargetSamples()
        {
            LevelData data = CreateCornerConnectionData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSpline source = root.transform.Find("Belt_Source").GetComponent<ConveyorSpline>();
                ConveyorSpline target = root.transform.Find("Belt_Target").GetComponent<ConveyorSpline>();
                ConveyorBeltRenderer renderer = source.GetComponent<ConveyorBeltRenderer>();
                MethodInfo getRoute = typeof(ConveyorBeltRenderer).GetMethod(
                    "TryGetConnectionRoute", BindingFlags.Instance | BindingFlags.Public);

                Assert.That(getRoute, Is.Not.Null,
                    "The movement system needs the same sampled path used by the rendered connector.");

                object[] routeArgs = { target, 0f, 0f, 0f };
                Assert.That((bool)getRoute.Invoke(renderer, routeArgs), Is.True);
                float sourceStart = (float)routeArgs[1];
                float targetEnd = (float)routeArgs[2];
                float routeLength = (float)routeArgs[3];

                MethodInfo sampleRoute = typeof(ConveyorBeltRenderer).GetMethod(
                    "TrySampleConnectionRoute", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(sampleRoute, Is.Not.Null);

                object[] startArgs = { target, 0f, 0f, Vector3.zero, Vector3.zero };
                object[] endArgs = { target, routeLength, 0f, Vector3.zero, Vector3.zero };
                Assert.That((bool)sampleRoute.Invoke(renderer, startArgs), Is.True);
                Assert.That((bool)sampleRoute.Invoke(renderer, endArgs), Is.True);

                Vector3 routeStart = (Vector3)startArgs[3];
                Vector3 routeEnd = (Vector3)endArgs[3];
                Assert.That(Vector2.Distance(routeStart,
                    source.GetPositionOnSpline(sourceStart, 0f)), Is.LessThan(0.02f));
                Assert.That(Vector2.Distance(routeEnd,
                    target.GetPositionOnSpline(targetEnd, 0f)), Is.LessThan(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConnectedBelts_KeepTheirEndpointsToPreventVisibleSeams()
        {
            LevelData data = CreateCornerConnectionData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer source = root.transform.Find("Belt_Source")
                    .GetComponent<ConveyorBeltRenderer>();
                ConveyorBeltRenderer target = root.transform.Find("Belt_Target")
                    .GetComponent<ConveyorBeltRenderer>();
                MethodInfo incomingTrim = typeof(ConveyorBeltRenderer).GetMethod(
                    "GetIncomingTrimProgress", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo outgoingTrim = typeof(ConveyorBeltRenderer).GetMethod(
                    "GetOutgoingTrimProgress", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(incomingTrim, Is.Not.Null);
                Assert.That(outgoingTrim, Is.Not.Null);
                Assert.That((float)outgoingTrim.Invoke(source, null), Is.EqualTo(1f).Within(0.0001f));
                Assert.That((float)incomingTrim.Invoke(target, null), Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConnectionWalls_DoNotFlipAcrossTightCornerCenter()
        {
            LevelData data = CreateCornerConnectionData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer renderer = root.transform.Find("Belt_Source")
                    .GetComponent<ConveyorBeltRenderer>();
                renderer.showWalls = true;
                renderer.wallWidth = 0.5f;
                renderer.BuildMesh();

                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                int baseVertexCount = (Mathf.Max(2, renderer.segments) + 1) * 2 * 3;
                AssertConnectorTrianglesKeepWinding(mesh, 1, baseVertexCount);
                AssertConnectorTrianglesKeepWinding(mesh, 2, baseVertexCount);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConveyorWalls_DoNotFlipAcrossOwnTightRoundedCorner()
        {
            LevelData data = CreateCornerConnectionData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorBeltRenderer renderer = root.transform.Find("Belt_Target")
                    .GetComponent<ConveyorBeltRenderer>();
                renderer.showWalls = true;
                renderer.wallWidth = 0.5f;
                renderer.renderConnections = false;
                renderer.BuildMesh();

                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                AssertTrianglesKeepWinding(mesh, 1, 0);
                AssertTrianglesKeepWinding(mesh, 2, 0);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void DotMovement_UsesConnectionRouteWithoutPositionJump()
        {
            LevelData data = CreateCornerConnectionData();
            GameObject root = LevelBuilder.Build(data);
            GameObject managerObject = new GameObject("FallingPixelManager_Test");
            GameObject dotObject = new GameObject("Dot_Test");
            try
            {
                ConveyorSpline source = root.transform.Find("Belt_Source").GetComponent<ConveyorSpline>();
                ConveyorSpline target = root.transform.Find("Belt_Target").GetComponent<ConveyorSpline>();
                ConveyorBeltRenderer renderer = source.GetComponent<ConveyorBeltRenderer>();
                Assert.That(renderer.TryGetConnectionRoute(target,
                    out float sourceStart, out float targetEnd, out _), Is.True);

                FallingPixelManager manager = managerObject.AddComponent<FallingPixelManager>();
                manager.beltSpeed = 2f;
                manager.dotSize = 0.2f;
                manager.speedJitter = 0f;

                Dot dot = dotObject.AddComponent<Dot>();
                dot.state = DotState.OnBelt;
                dot.conveyor = source;
                dot.beltProgress = Mathf.Max(0f, sourceStart - 0.02f);
                dot.lateralOffset = 0.4f;
                dot.beltSpeedFactor = 1f;

                MethodInfo step = typeof(FallingPixelManager).GetMethod(
                    "StepOnBelt", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo positionCacheField = typeof(FallingPixelManager).GetField(
                    "_posCache", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(step, Is.Not.Null);
                Assert.That(positionCacheField, Is.Not.Null);

                Vector3[] positionCache = (Vector3[])positionCacheField.GetValue(manager);
                Vector3 previous = source.GetPositionOnSpline(dot.beltProgress, dot.lateralOffset);
                positionCache[0] = previous;
                const float deltaTime = 0.02f;
                float maxStep = 0f;

                for (int frame = 0; frame < 240; frame++)
                {
                    step.Invoke(manager, new object[] { dot, Vector2.zero, deltaTime, 0 });
                    Vector3 current = positionCache[0];
                    maxStep = Mathf.Max(maxStep, Vector2.Distance(previous, current));
                    previous = current;
                    if (dot.conveyor == target && dot.beltProgress > targetEnd + 0.08f) break;
                }

                Assert.That(dot.conveyor, Is.SameAs(target));
                Assert.That(maxStep, Is.LessThanOrEqualTo(manager.beltSpeed * deltaTime * 2f),
                    "A dot must follow the rounded connection path instead of rotating its lateral offset in one frame.");
            }
            finally
            {
                Object.DestroyImmediate(dotObject);
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void ConnectionRoute_SwitchBranchesAlwaysUseOwnedTargetSection()
        {
            LevelData data = CreateSwitchConnectionData();
            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSpline source = root.transform.Find("Belt_Source")
                    .GetComponent<ConveyorSpline>();
                ConveyorSpline up = root.transform.Find("Belt_Up")
                    .GetComponent<ConveyorSpline>();
                ConveyorSpline down = root.transform.Find("Belt_Down")
                    .GetComponent<ConveyorSpline>();
                ConveyorSwitch routeSwitch = source.GetComponent<ConveyorSwitch>();
                ConveyorBeltRenderer renderer = source.GetComponent<ConveyorBeltRenderer>();

                Assert.That(routeSwitch, Is.Not.Null);
                AssertRouteEndsAtOwnedSection(renderer, routeSwitch, up);
                AssertRouteEndsAtOwnedSection(renderer, routeSwitch, down);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void DotMovement_BlockedOnFirstConnectionFrameRollsBackRouteState()
        {
            LevelData data = CreateCornerConnectionData();
            GameObject root = LevelBuilder.Build(data);
            GameObject managerObject = new GameObject("FallingPixelManager_BlockedConnectionTest");
            GameObject dotObject = new GameObject("Dot_BlockedConnectionTest");
            GameObject obstacleObject = new GameObject("Obstacle_BlockedConnectionTest");
            try
            {
                ConveyorSpline source = root.transform.Find("Belt_Source")
                    .GetComponent<ConveyorSpline>();
                ConveyorSpline target = root.transform.Find("Belt_Target")
                    .GetComponent<ConveyorSpline>();
                ConveyorBeltRenderer renderer = source.GetComponent<ConveyorBeltRenderer>();
                Assert.That(renderer.TryGetConnectionRoute(target,
                    out float sourceStart, out _, out _), Is.True);

                FallingPixelManager manager = managerObject.AddComponent<FallingPixelManager>();
                manager.beltSpeed = 2f;
                manager.dotSize = 0.2f;
                manager.speedJitter = 0f;

                Dot dot = dotObject.AddComponent<Dot>();
                dot.state = DotState.OnBelt;
                dot.conveyor = source;
                dot.beltProgress = Mathf.Max(0f, sourceStart - 0.001f);
                dot.lateralOffset = 0.2f;
                dot.beltSpeedFactor = 1f;

                DotObstacle obstacle = obstacleObject.AddComponent<DotObstacle>();
                obstacle.blockBeltDots = true;
                obstacle.fallbackRadius = 100f;
                obstacle.requiredDots = 100;
                typeof(DotObstacle).GetMethod("OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(obstacle, null);
                Assert.That(DotObstacle.All, Does.Contain(obstacle));

                MethodInfo step = typeof(FallingPixelManager).GetMethod(
                    "StepOnBelt", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo positionCacheField = typeof(FallingPixelManager).GetField(
                    "_posCache", BindingFlags.Instance | BindingFlags.NonPublic);
                Vector3[] positionCache = (Vector3[])positionCacheField.GetValue(manager);
                float previousProgress = dot.beltProgress;
                Vector3 previousPosition = source.GetPositionOnSpline(
                    previousProgress, dot.lateralOffset);
                positionCache[0] = previousPosition;

                step.Invoke(manager, new object[] { dot, Vector2.zero, 0.02f, 0 });

                Assert.That(dot.connectionTarget, Is.Null,
                    "A blocked entry must not leave the dot half-transitioned onto a connection route.");
                Assert.That(dot.connectionDistance, Is.Zero.Within(0.0001f));
                Assert.That(dot.beltProgress, Is.EqualTo(previousProgress).Within(0.0001f));
                Assert.That(Vector2.Distance(positionCache[0], previousPosition),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(obstacleObject);
                Object.DestroyImmediate(dotObject);
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

        static void AssertRouteEndsAtOwnedSection(ConveyorBeltRenderer renderer,
            ConveyorSwitch routeSwitch, ConveyorSpline target)
        {
            float ownedProgress = routeSwitch.GetActiveBranchOwnedProgress(target);
            Assert.That(renderer.TryGetConnectionRoute(target,
                out _, out float targetEnd, out _), Is.True);
            Assert.That(targetEnd, Is.EqualTo(ownedProgress).Within(0.001f),
                "A dot already entering a switch branch needs route geometry that stays " +
                "unchanged if that branch becomes inactive.");
        }

        static void AssertConnectorTrianglesKeepWinding(Mesh mesh, int subMesh, int firstConnectorVertex)
        {
            AssertTrianglesKeepWinding(mesh, subMesh, firstConnectorVertex);
        }

        static void AssertTrianglesKeepWinding(Mesh mesh, int subMesh, int firstVertex)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.GetTriangles(subMesh);
            float expectedSign = 0f;
            int checkedCount = 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                if (a < firstVertex && b < firstVertex && c < firstVertex)
                    continue;

                Vector3 ab = vertices[b] - vertices[a];
                Vector3 ac = vertices[c] - vertices[a];
                float signedArea = Vector3.Cross(ab, ac).z;
                if (Mathf.Abs(signedArea) <= 1e-6f) continue;

                float sign = Mathf.Sign(signedArea);
                if (expectedSign == 0f) expectedSign = sign;
                Assert.That(sign, Is.EqualTo(expectedSign),
                    $"A wall crossed the corner center at triangle {i / 3}: " +
                    $"{vertices[a]} -> {vertices[b]} -> {vertices[c]}.");
                checkedCount++;
            }

            Assert.That(checkedCount, Is.GreaterThan(0));
        }

        static LevelData CreateCornerConnectionData()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = asset.GetComponent<ConveyorSpline>();
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Belt_Source",
                beltWidth = 1f,
                straightEdges = true,
                cornerRadius = 3f,
                cornerSegments = 12,
                knots = new List<Vector3>
                {
                    new Vector3(-2.6f, 3f),
                    new Vector3(2.6f, 3f),
                    new Vector3(2.6f, 4.1f),
                },
            });
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Belt_Target",
                beltWidth = 1f,
                straightEdges = true,
                cornerRadius = 3f,
                cornerSegments = 12,
                knots = new List<Vector3>
                {
                    new Vector3(2.6f, 4.1f),
                    new Vector3(-0.3f, 4.1f),
                    new Vector3(-0.7f, 5.1f),
                },
            });
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 1 });
            return data;
        }

        static LevelData CreateSwitchConnectionData()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorPrefabPath);
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyorPrefab = asset.GetComponent<ConveyorSpline>();
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Belt_Source",
                beltWidth = 1f,
                straightEdges = true,
                hasSwitch = true,
                knots = new List<Vector3>
                {
                    new Vector3(-2f, 0f),
                    Vector3.zero,
                },
            });
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Belt_Up",
                beltWidth = 1f,
                straightEdges = true,
                cornerRadius = 0.5f,
                cornerSegments = 8,
                knots = new List<Vector3>
                {
                    Vector3.zero,
                    new Vector3(2f, 0f),
                    new Vector3(2f, 2f),
                },
            });
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Belt_Down",
                beltWidth = 1f,
                straightEdges = true,
                cornerRadius = 0.5f,
                cornerSegments = 8,
                knots = new List<Vector3>
                {
                    Vector3.zero,
                    new Vector3(2f, 0f),
                    new Vector3(2f, -2f),
                },
            });
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 1 });
            data.conveyorLinks.Add(new LevelData.ConveyorLink { from = 0, to = 2 });
            return data;
        }
    }
}
