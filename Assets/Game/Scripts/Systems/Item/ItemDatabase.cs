using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HAVIGAME;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Database/ItemDatabase")]
public class ItemDatabase : Database<ItemDatabase, int, ItemData> {

#if UNITY_EDITOR
    protected override bool Installable => true;
#endif
}
