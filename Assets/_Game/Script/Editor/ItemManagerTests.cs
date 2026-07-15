using NUnit.Framework;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class ItemManagerTests
    {
        [Test]
        public void GetIcon_ReturnsTheConfiguredTimeFreezeIcon()
        {
            Sprite icon = ItemManager.GetIcon(ItemID.TimeFreezeBooster);

            Assert.That(icon, Is.Not.Null);
        }

        [Test]
        public void TryGetItem_ReturnsFalseForUnknownItemId()
        {
            bool found = ItemManager.TryGetItem(int.MaxValue, out ItemData itemData);

            Assert.That(found, Is.False);
            Assert.That(itemData, Is.Null);
        }
    }
}
