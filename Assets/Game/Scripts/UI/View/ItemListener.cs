using UnityEngine;
using HAVIGAME;

public class ItemListener : MonoBehaviour {
    [SerializeField, ConstantField(typeof(ItemID))] private int itemID;
    [SerializeField] private ItemView itemView;

    private void Start() {
        OnPlayerInventoryChanged(new GameEvent.PlayerInventoryChanged(GameData.Inventory, new ItemStack(itemID, 0), true));

        EventDispatcher.AddListener<GameEvent.PlayerInventoryChanged>(OnPlayerInventoryChanged);
    }

    private void OnDestroy() {
        EventDispatcher.RemoveListener<GameEvent.PlayerInventoryChanged>(OnPlayerInventoryChanged);
    }

    private void OnPlayerInventoryChanged(GameEvent.PlayerInventoryChanged e) {
        if (e.ItemStackChange.Id == itemID) {
            ItemStack current = e.Inventory.Get(itemID);

            itemView.SetModel(current);
            itemView.Show();
        }
    }
}
