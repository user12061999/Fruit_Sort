#if UNITY_EDITOR
using NUnit.Framework;

namespace FruitSort.EditorTests
{
    public sealed class LevelDesign100DotGeneratorTests
    {
        [Test]
        public void Definitions_HaveBalancedSupplyAndValidMoveLimits()
        {
            var definitions = LevelDesign100DotGenerator.CreateDefinitions();

            Assert.That(definitions, Has.Count.EqualTo(20));
            foreach (LevelDesign100DotGenerator.Definition definition in definitions)
            {
                Assert.That(definition.TotalSupply, Is.EqualTo(definition.TotalRequiredCapacity), definition.Name);
                Assert.That(definition.MoveLimit, Is.GreaterThanOrEqualTo(definition.MinimumMoves), definition.Name);
            }
        }
    }
}
#endif
