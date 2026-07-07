using System;
using UnityEngine;

#if IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
#endif

namespace HAVIGAME.Services.IAP {
    [System.Serializable]
    public abstract class IAPValidator {
        public abstract string Name { get; }

#if IAP
        public abstract PurchaseProcessingResult Validate(IStoreController storeController, IExtensionProvider extensionProvider, PurchaseEventArgs purchaseEventArgs, Action<string, PurchaseProcessingResult> onCompleted, Action<string, PurchaseProcessingResult, string> onFailed);
#endif
    }

    [CategoryMenu("Unity Validator")]
    [System.Serializable]
    public class UnityIAPValidator : IAPValidator {
        public override string Name => "Unity Validator";

#if IAP
        public override PurchaseProcessingResult Validate(IStoreController storeController, IExtensionProvider extensionProvider, PurchaseEventArgs purchaseEventArgs, Action<string, PurchaseProcessingResult> onCompleted, Action<string, PurchaseProcessingResult, string> onFailed) {
            CrossPlatformValidator validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            string productId = purchaseEventArgs.purchasedProduct.definition.id;
            string transactionID = purchaseEventArgs.purchasedProduct.transactionID;

            try {
                IPurchaseReceipt[] purchaseReceipts = validator.Validate(purchaseEventArgs.purchasedProduct.receipt);
                IPurchaseReceipt purchaseReceipt = FindPurchaseReceipt(purchaseReceipts, productId);

                if (purchaseReceipt != null) {
                    onCompleted?.Invoke(productId, PurchaseProcessingResult.Complete);
                    return PurchaseProcessingResult.Complete;
                } else {
                    onFailed?.Invoke(productId, PurchaseProcessingResult.Complete, "Receipt not found!");
                    return PurchaseProcessingResult.Complete;
                }
            } catch (IAPSecurityException) {
                onFailed?.Invoke(productId, PurchaseProcessingResult.Complete, "Invalid receipt!");
                return PurchaseProcessingResult.Complete;
            }
        }

        private IPurchaseReceipt FindPurchaseReceipt(IPurchaseReceipt[] purchaseReceipts, string productId) {
            foreach (IPurchaseReceipt productReceipt in purchaseReceipts) {
                Log.Debug($"productID = {productReceipt.productID}, date = {productReceipt.purchaseDate}, transactionID = {productReceipt.transactionID}");

                if (productReceipt.productID.Equals(productId)) {
                    return productReceipt;
                }
            }
            return null;
        }
#endif
    }
}