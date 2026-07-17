using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class PointerInputLegacyInputTests
    {
        [Test]
        public void OldInputProject_UsesLegacyTouchAndMousePointerPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string projectSettings = File.ReadAllText(Path.Combine(projectRoot,
                "ProjectSettings", "ProjectSettings.asset"));
            string pointerInput = File.ReadAllText(Path.Combine(projectRoot,
                "Assets", "_Game", "Script", "PointerInput.cs"));
            string pointerGuard = File.ReadAllText(Path.Combine(projectRoot,
                "Assets", "_Game", "Script", "UIPointerGuard.cs"));

            Assert.That(projectSettings, Does.Contain("activeInputHandler: 0"));
            Assert.That(pointerInput, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(pointerInput, Does.Contain("Input.touchCount"));
            Assert.That(pointerInput, Does.Contain("Input.GetMouseButtonDown(0)"));
            Assert.That(pointerGuard, Does.Contain("IsPointerOverGameObject(fingerId)"));
        }
    }
}
