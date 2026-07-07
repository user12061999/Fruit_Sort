using System;
using UnityEngine;

namespace HAVIGAME.Services.IAP {

    [System.Serializable]
    public enum IAPProductType {
        Consumable,
        NonConsumable,
        Subscription
    }

    [System.Serializable]
    public class IAPProduct {
        [SerializeField] private string name;
        [SerializeField] private IAPProductType type = IAPProductType.Consumable;
        [SerializeField] private string id;
        [SerializeField] private float defaultPrice;
        [SerializeField, TextArea(3, 5)] private string description;

        public string Name => name;
        public IAPProductType Type => type;
        public string Id => id;
        public float DefaultPrice => defaultPrice;
        public string Description => description;

        public string GetLocalizedPriceString() {
            return IAPManager.GetLocalizedPriceString(Id, $"${DefaultPrice}");
        }

        public bool IsOwned() {
            return IAPManager.IsOwned(Id);
        }

        public bool IsSubscribed() {
            if (Type == IAPProductType.Subscription) {
                return IAPManager.IsSubscribed(Id);
            }
            else {
                return false;
            }
        }

        public bool Purchase(Action onCompleted = null, Action<string> onFailed = null) {
            return IAPManager.Purchase(Id, onCompleted, onFailed);
        }
    }
}