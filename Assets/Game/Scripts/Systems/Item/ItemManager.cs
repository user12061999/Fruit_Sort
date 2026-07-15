using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime access point for item data stored in <see cref="ItemDatabase"/>.
/// Keeps UI and gameplay code from having to access the database directly.
/// </summary>
public static class ItemManager
{
    public static bool TryGetItem(int itemId, out ItemData itemData)
    {
        itemData = null;

        ItemDatabase database = ItemDatabase.Instance;
        if (database == null)
        {
            return false;
        }

        try
        {
            itemData = database.GetDataById(itemId);
            return itemData != null;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    public static ItemData GetItem(int itemId)
    {
        TryGetItem(itemId, out ItemData itemData);
        return itemData;
    }

    public static Sprite GetIcon(int itemId)
    {
        return TryGetItem(itemId, out ItemData itemData) ? itemData.Icon : null;
    }
}
