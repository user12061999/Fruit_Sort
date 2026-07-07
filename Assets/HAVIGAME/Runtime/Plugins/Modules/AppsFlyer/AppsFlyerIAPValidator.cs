using System;
using System.Collections.Generic;
using UnityEngine;
using HAVIGAME.Services.IAP;

#if IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

#if APPSFLYER
using AppsFlyerSDK;
#endif

#endif

namespace HAVIGAME.Plugins.AppsFlyer {

    [CategoryMenu("AppsFlyer Validator")]
    [System.Serializable]
    public class AppsFlyerIAPValidator : IAPValidator {

        [SerializeField] private bool taxDeductionEnabled;

        private Dictionary<string, TaxInfo> taxs;

        public override string Name => "AppsFlyer Validator";
        public bool TaxDeductionEnabled => taxDeductionEnabled;

        private string GetUserCountryCode() {
            return AppsFlyerManager.CountryCode;
        }


        private decimal CalculateTaxAndFree(decimal originPrice) {
            string countryCode = GetUserCountryCode();
            int tax = GetIAPTax(countryCode);
            decimal result = originPrice;
            if (tax > 0) {
                result = originPrice - originPrice * (0.3m + tax / 100m);
            } else {
                result = originPrice - originPrice * 0.3m;
            }
            return result;
        }

        private int GetIAPTax(string countryCode) {
            if (taxs == null) {
                IAPSettings settings = IAPSettings.Instance;

                taxs = new Dictionary<string, TaxInfo>(settings.Taxs.Length);
                for (int i = 0; i < settings.Taxs.Length; i++) {
                    taxs[settings.Taxs[i].countryCode] = settings.Taxs[i];
                }
            }

            if (taxs.TryGetValue(countryCode, out TaxInfo tax)) {
                return tax.taxRate;
            }

            return 0;
        }

#if IAP

#if APPSFLYER
        private string productId;
        private Action<string, PurchaseProcessingResult> onCompleted;
        private Action<string, PurchaseProcessingResult, string> onFailed;

        public override PurchaseProcessingResult Validate(IStoreController storeController, IExtensionProvider extensionProvider, PurchaseEventArgs purchaseEventArgs, Action<string, PurchaseProcessingResult> onCompleted, Action<string, PurchaseProcessingResult, string> onFailed) {
            this.onCompleted = onCompleted;
            this.onFailed = onFailed;

            productId = purchaseEventArgs.purchasedProduct.definition.id;
            string transactionID = purchaseEventArgs.purchasedProduct.transactionID;
            decimal localizedPrice = purchaseEventArgs.purchasedProduct.metadata.localizedPrice;
            string isoCurrencyCode = purchaseEventArgs.purchasedProduct.metadata.isoCurrencyCode;

            if (TaxDeductionEnabled) {
                localizedPrice = CalculateTaxAndFree(localizedPrice);
            }

#if UNITY_ANDROID
            CrossPlatformValidator validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            AFPurchaseType purchaseType = ConvertToAFPurchaseType(purchaseEventArgs.purchasedProduct.definition.type);

            try {
                IPurchaseReceipt[] purchaseReceipts = validator.Validate(purchaseEventArgs.purchasedProduct.receipt);
                IPurchaseReceipt purchaseReceipt = FindPurchaseReceipt(purchaseReceipts, productId);

                if (purchaseReceipt != null) {
                    GooglePlayReceipt googlePlayPurchaseReceipt = purchaseReceipt as GooglePlayReceipt;
                    string purchaseToken = googlePlayPurchaseReceipt.purchaseToken;
                    AFPurchaseDetailsAndroid details = new AFPurchaseDetailsAndroid(purchaseType, purchaseToken, productId, localizedPrice.ToString(), isoCurrencyCode);
                    AppsFlyerManager.ValidateAndSendInAppPurchase(details, null, OnValidateAndLogCompleted, OnValidateAndLogFailed);
                    return PurchaseProcessingResult.Pending;
                } else {
                    onFailed?.Invoke(productId, PurchaseProcessingResult.Complete, "Receipt not found!");
                    return PurchaseProcessingResult.Complete;
                }
            } catch (IAPSecurityException) {
                onFailed?.Invoke(productId, PurchaseProcessingResult.Complete, "Invalid receipt!");
                return PurchaseProcessingResult.Complete;
            }
#elif UNITY_IOS
            AFSDKPurchaseDetailsIOS details = AFSDKPurchaseDetailsIOS.Init(productId, localizedPrice.ToString(), isoCurrencyCode, transactionID);
            AppsFlyerManager.ValidateAndSendInAppPurchase(details, null, OnValidateAndLogCompleted, OnValidateAndLogFailed);
            return PurchaseProcessingResult.Pending;
#else
            onCompleted?.Invoke(productId, PurchaseProcessingResult.Complete);
            return PurchaseProcessingResult.Complete;
#endif
        }

        private void OnValidateAndLogCompleted(string result) {
            if (Log.DebugEnabled) Log.Debug($"Validate result = {result}");
            onCompleted?.Invoke(productId, PurchaseProcessingResult.Pending);
        }

        private void OnValidateAndLogFailed(string error) {
            onFailed?.Invoke(productId, PurchaseProcessingResult.Pending, error);
        }

        private AFPurchaseType ConvertToAFPurchaseType(ProductType productType) {
            switch (productType) {
                case ProductType.Subscription: return AFPurchaseType.Subscription;
                default: return AFPurchaseType.OneTimePurchase;
            }
        }
#else
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
#endif
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