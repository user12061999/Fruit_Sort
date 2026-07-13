using NUnit.Framework;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class ConveyorGrinderFlowTests
    {
        [Test]
        public void LevelBuilder_LinksGrinderToItsInputConveyor()
        {
            var data = ScriptableObject.CreateInstance<LevelData>();
            data.conveyors.Add(new LevelData.ConveyorData
            {
                name = "Input",
                knots = new System.Collections.Generic.List<Vector3>
                {
                    Vector3.zero,
                    Vector3.right * 2f,
                },
            });
            data.grinders.Add(new LevelData.GrinderData { position = new Vector3(2f, 0f, 0f), inputConveyor = 0 });

            GameObject root = LevelBuilder.Build(data);
            try
            {
                ConveyorSpline conveyor = root.GetComponentInChildren<ConveyorSpline>();
                ConveyorGrinder grinder = root.GetComponentInChildren<ConveyorGrinder>();

                Assert.That(grinder, Is.Not.Null);
                Assert.That(conveyor.GetComponent<ConveyorConnections>().terminalGrinder, Is.SameAs(grinder));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(data);
            }
        }

    }
}
