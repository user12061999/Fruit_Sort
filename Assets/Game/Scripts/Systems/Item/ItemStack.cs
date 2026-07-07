using System.Collections.Generic;
using HAVIGAME;
using UnityEngine;

[System.Serializable]
public struct ItemStack {
    [SerializeField, ConstantField(typeof(ItemID))] private int id;
    [SerializeField] private int amount;

    public int Id => id;
    public int Amount => amount;

    public ItemStack(int id, int amount) {
        this.id = id;
        this.amount = amount;
    }

    public void Stack(int amount) {
        this.amount += amount;
    }

    public void Destack(int amount) {
        if (amount >= this.amount) {
            this.amount = 0;
        }
        else {
            this.amount -= amount;
        }
    }
}
public static class ItemStackExtensions {
    private static readonly Dictionary<int, ItemStack> cache = new Dictionary<int, ItemStack>();
    
    
    public static IEnumerable<ItemStack> Stack(this IEnumerable<ItemStack> itemStacks) {
        cache.Clear();

        foreach (var item in itemStacks) {
            if (cache.ContainsKey(item.Id)) {
                var itemStack = cache[item.Id];
                itemStack.Stack(item.Amount);
                cache[item.Id] = itemStack;

                //cache[item.Id].Stack(item.Amount);
            } else {
                cache[item.Id] = item;
            }
        }

        return cache.Values;
    }
}
