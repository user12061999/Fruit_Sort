using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FruitSort.EditorTests
{
    public sealed class GamePanelBoosterItemViewTests
    {
        const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void BoosterButtons_UseItemViewAndListenerInsteadOfGamePanelManualPresentation()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Resources/UI/GamePanel.prefab");
            Assert.That(prefab, Is.Not.Null);

            Assert.That(FindListener(prefab, ItemID.TimeFreezeBooster), Is.Not.Null);
            Assert.That(FindListener(prefab, ItemID.MagnetBooster), Is.Not.Null);
            Assert.That(typeof(GamePanel).GetMethod("ApplyItemIcon", InstanceFlags | BindingFlags.Static), Is.Null);
            Assert.That(typeof(GamePanel).GetField("txtFreezeCount", InstanceFlags), Is.Null);
            Assert.That(typeof(GamePanel).GetField("txtMagnetCount", InstanceFlags), Is.Null);
        }

        static ItemListener FindListener(GameObject prefab, int itemId)
        {
            FieldInfo itemIdField = typeof(ItemListener).GetField("itemID", InstanceFlags);
            FieldInfo itemViewField = typeof(ItemListener).GetField("itemView", InstanceFlags);
            foreach (ItemListener listener in prefab.GetComponentsInChildren<ItemListener>(true))
            {
                if ((int)itemIdField.GetValue(listener) == itemId &&
                    itemViewField.GetValue(listener) is ItemView)
                    return listener;
            }

            return null;
        }
    }
}
