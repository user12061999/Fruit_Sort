using System;
using UnityEngine;
using System.Collections.Generic;

#if IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine.Purchasing.Extension;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
#endif


namespace HAVIGAME.Services.IAP {
    public static class IAPManager {

        public const string DEFINE_SYMBOL = "IAP";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        public static event IAPPurchaseDelegate onPurchased;
        public static event IAPPurchaseErrorDelegate onPurchaseFailed;
        public static event IAPRestorePurchaseDelegate onRestorePurchased;
        public static event IAPErrorDelegate onRestorePurchaseFailed;

#if IAP
        private static StoreListener storeListener;
        private static ProductTransaction productTransaction;
#endif

        public static bool IsInitialized => initializeEvent.IsInitialized;
        public static bool IsTransacting {
            get {
#if IAP
                return IsInitialized && productTransaction.IsTransacting;
#else
                return false;
#endif
            }
        }

        public static void Initialize() {
            IAPSettings settings = IAPSettings.Instance;

#if IAP
            if (initializeEvent.IsRunning) {
                Log.Warning("[IAPManager] Cancel initialize! In-App Purchasing initialized with result {0}", initializeEvent.IsInitialized);
                return;
            }

            if (settings.GameServiceEnabled) {
                InitializeGamingServices(OnInitializeGamingServicesCompleted, OnInitializeGamingServicesFailed);
            }
            else {
                InitializeInAppPurchase();
            }
#endif
        }

        public static bool IsOwned(string productId) {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] In-App Purchasing is not initialized.");
                return false;
            }
            return storeListener.IsOwned(productId);
#else
            return false;
#endif
        }

        public static bool IsSubscribed(string productId) {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] In-App Purchasing is not initialized.");
                return false;
            }
            return storeListener.IsOwned(productId);
#else
            return false;
#endif
        }

        public static bool Purchase(string productId, Action onCompleted = null, Action<string> onFailed = null) {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] Purchase failed! In-App Purchasing is not initialized.");
                return false;
            }

            Product product = storeListener.GetProduct(productId);

            if (product == null) {
                return false;
            }

            return productTransaction.StartTransaction(productId, onCompleted, onFailed);
#else
            Log.Info("[IAPManager] Purchase failed! In-App Purchasing is not enabled.");
            onFailed?.Invoke("In-App Purchasing is not enabled");
            return false;
#endif
        }

        public static bool RestorePurchases(Action<string[]> onCompleted = null, Action<string> onFailed = null) {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] Restore purchase failed! In-App Purchasing is not initialized.");
                onFailed?.Invoke("In-App Purchasing is not initialized.");
                return false;
            }

            return productTransaction.StartRestorePurchase(onCompleted, onFailed);
#else
            Log.Info("[IAPManager] Restore purchase failed! In-App Purchasing is not enabled.");
            onFailed?.Invoke("In-App Purchasing is not enabled");
            return false;
#endif
        }

        public static string GetLocalizedPriceString(string productId, string defaultPrice = "$0.99") {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] In-App Purchasing is not initialized.");
                return defaultPrice;
            }

            Product product = storeListener.GetProduct(productId);
            if (product != null) {
                return product.metadata.localizedPriceString;
            }
            else {
                return defaultPrice;
            }
#else
            return defaultPrice;
#endif
        }

        public static decimal GetLocalizedPrice(string productId, decimal defaultPrice = 0.99m) {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] In-App Purchasing is not initialized.");
                return defaultPrice;
            }

            Product product = storeListener.GetProduct(productId);
            if (product != null) {
                return product.metadata.localizedPrice;
            }
            else {
                return defaultPrice;
            }
#else
            return defaultPrice;
#endif
        }

        public static string GetIsoCurrencyCode(string productId, string defaultIsoCurrencyCode = "$") {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] In-App Purchasing is not initialized.");
                return defaultIsoCurrencyCode;
            }

            Product product = storeListener.GetProduct(productId);
            if (product != null) {
                return product.metadata.isoCurrencyCode;
            }
            else {
                return defaultIsoCurrencyCode;
            }
#else
            return defaultIsoCurrencyCode;
#endif
        }

        public static DateTime GetSubscriptionPurchaseDate(string productId) {
#if IAP
            if (!IsInitialized) {
                Log.Warning("[IAPManager] In-App Purchasing is not initialized.");
                return default;
            }
            return storeListener.GetSubscriptionPurchaseDate(productId);
#else
            return default;
#endif
        }

#if IAP
        private static void InitializeGamingServices(Action onCompleted, Action<string> onFailed) {
            Log.Debug("[IAPManager] Initialize the gaming services...");
            try {
                var options = new InitializationOptions().SetEnvironmentName("production");

                UnityServices.InitializeAsync(options).ContinueWith(task => Executor.Instance.RunOnMainTheard(onCompleted));
            }
            catch (Exception exception) {
                Executor.Instance.RunOnMainTheard(() => onFailed?.Invoke(exception.Message));
            }
        }

        private static void OnInitializeGamingServicesCompleted() {
            Log.Debug("[IAPManager] Initialize the gaming services completed!");

            InitializeInAppPurchase();
        }

        private static void OnInitializeGamingServicesFailed(string error) {
            Log.Error("[IAPManager] Initialize the gaming services failed!");
            initializeEvent.Invoke(false);
        }

        private static void InitializeInAppPurchase() {
            Log.Debug("[IAPManager] Initialize the in-app purchasing...");

            IAPSettings settings = IAPSettings.Instance;

            ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            foreach (IAPProduct product in settings.Products) {
                builder.AddProduct(product.Id, Convert(product.Type));
            }

            storeListener = new StoreListener(settings.Validator);

            storeListener.onInitialized += OnInAppPurchaseInitializeCompleted;
            storeListener.onInitializeFailed += OnInAppPurchaseInitializeFailed;
            storeListener.onPurchased += OnPurchased;
            storeListener.onPurchaseFailed += OnPurchaseFailed;
            storeListener.onRestorePurchased += OnRestorePurchased;
            storeListener.onRestorePurchaseFailed += OnRestorePurchaseFailed;

            UnityPurchasing.Initialize(storeListener, builder);

            Database.Unload(settings);
        }

        private static void OnInAppPurchaseInitializeCompleted() {
            Log.Info("[IAPManager] Initialize the in-app purchasing completed!");
            productTransaction = new ProductTransaction(storeListener);

            initializeEvent.Invoke(true);
        }

        private static void OnInAppPurchaseInitializeFailed(string error) {
            Log.Debug("[IAPManager] Initialize the in-app purchasing failed, error = {0}", error);

            initializeEvent.Invoke(false);
        }

        private static void OnPurchased(string productId) {
            onPurchased?.Invoke(productId);
        }

        private static void OnPurchaseFailed(string productId, string error) {
            onPurchaseFailed?.Invoke(productId, error);
        }

        private static void OnRestorePurchased(string[] products) {
            onRestorePurchased?.Invoke(products);
        }

        private static void OnRestorePurchaseFailed(string error) {
            onRestorePurchaseFailed?.Invoke(error);
        }

        private static ProductType Convert(IAPProductType productType) {
            return (ProductType)productType;
        }

        private class StoreListener : IDetailedStoreListener {
            private IAPValidator validator;
            private IStoreController controller;
            private IExtensionProvider extensions;
            private bool isPurchasing;
            private bool isRestorePurchasing;

            public bool IsPurchasing => isPurchasing;
            public bool IsRestorePurchasing => isRestorePurchasing;

            public event IAPDelegate onInitialized;
            public event IAPErrorDelegate onInitializeFailed;
            public event IAPPurchaseDelegate onPurchased;
            public event IAPPurchaseErrorDelegate onPurchaseFailed;
            public event IAPRestorePurchaseDelegate onRestorePurchased;
            public event IAPErrorDelegate onRestorePurchaseFailed;

            public StoreListener(IAPValidator validator) {
                this.validator = validator;
                this.controller = null;
                this.extensions = null;
                this.isPurchasing = false;
                this.isRestorePurchasing = false;
            }

            public void OnInitialized(IStoreController controller, IExtensionProvider extensions) {
                this.controller = controller;
                this.extensions = extensions;

                onInitialized?.Invoke();
            }

            public void OnInitializeFailed(InitializationFailureReason error) {

            }

            public void OnInitializeFailed(InitializationFailureReason error, string message) {
                onInitializeFailed?.Invoke(message);
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) {
                onPurchaseFailed?.Invoke(product.definition.id, failureReason.ToString());
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) {
                onPurchaseFailed?.Invoke(product.definition.id, Utility.Text.Format("{0} - {1}", failureDescription.reason, failureDescription.message));
            }

            public void InitiatePurchase(string productId) {
                isPurchasing = true;
                controller.InitiatePurchase(productId);
            }

            public void RestorePurchases() {
                if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer) {
                    IAppleExtensions appleExtension = GetExtension<IAppleExtensions>();
                    isRestorePurchasing = true;
                    appleExtension.RestoreTransactions((result, error) => {
                        if (result) {
                            List<string> restoredProducts = new List<string>();
                            foreach (Product product in GetProducts(ProductType.NonConsumable)) {
                                if (IsOwned(product.definition.id)) {
                                    restoredProducts.Add(product.definition.id);
                                }
                            }

                            onRestorePurchased?.Invoke(restoredProducts.ToArray());
                        } else {
                            onRestorePurchaseFailed?.Invoke(error);
                        }
                        isRestorePurchasing = false;
                    });
                } else if (Application.platform == RuntimePlatform.Android) {
                    isRestorePurchasing = true;
                    List<string> restoredProducts = new List<string>();
                    foreach (Product product in GetProducts(ProductType.NonConsumable)) {
                        if (IsOwned(product.definition.id)) {
                            restoredProducts.Add(product.definition.id);
                        }
                    }
                    onRestorePurchased?.Invoke(restoredProducts.ToArray());
                    isRestorePurchasing = false;
                } else {
                    onRestorePurchaseFailed?.Invoke("In-App Purchasing is no supported this platform.");
                }
            }

            public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent) {
                if (isRestorePurchasing) {
                    Log.Debug($"Restore purchase product completed, productID = {purchaseEvent.purchasedProduct.definition.id}.");
                    return PurchaseProcessingResult.Complete;
                }

                if (!isPurchasing) {
                    Log.Debug($"Auto restore purchase product completed, productID = {purchaseEvent.purchasedProduct.definition.id}.");
                    return PurchaseProcessingResult.Complete;
                }

                if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer) {
                    if (validator != null) {
                        Log.Debug($"Start validating purchase receipt, productID = {purchaseEvent.purchasedProduct.definition.id}, validator = {validator.Name}.");
                        PurchaseProcessingResult result = validator.Validate(controller, extensions, purchaseEvent, OnValidatePurchaseCompleted, OnValidatePurchaseFailed);
                        return result;
                    } else {
                        Log.Debug($"The receipt is valid because the validator has been disabled, productID = {purchaseEvent.purchasedProduct.definition.id}.");
                        onPurchased?.Invoke(purchaseEvent.purchasedProduct.definition.id);
                        isPurchasing = false;
                        return PurchaseProcessingResult.Complete;
                    }
                } else {
                    return PurchaseProcessingResult.Complete;
                }
            }

            private void OnValidatePurchaseCompleted(string productId, PurchaseProcessingResult result) {
                Log.Debug("Verify the purchase completed.");

                onPurchased?.Invoke(productId);

                if (result == PurchaseProcessingResult.Pending) {
                    ConfirmPendingPurchase(productId);
                }
            }

            private void OnValidatePurchaseFailed(string productId, PurchaseProcessingResult result, string error) {
                Log.Error($"Verify the purchase failed, error = {error}");

                onPurchaseFailed?.Invoke(productId, error);

                if (result == PurchaseProcessingResult.Pending) {
                    ConfirmPendingPurchase(productId);
                }
            }

            private void ConfirmPendingPurchase(string productId) {
                Product product = GetProduct(productId);

                if (product != null) {
                    controller.ConfirmPendingPurchase(product);
                }

                isPurchasing = false;
            }

            public Product GetProduct(string productId) {
                return controller.products.WithID(productId);
            }

            public IEnumerable<Product> GetProducts(ProductType productType) {
                foreach (Product product in controller.products.all) {
                    if (product.definition.type == productType) {
                        yield return product;
                    }
                }
            }

            public bool IsOwned(string productId) {
                Product product = GetProduct(productId);

                bool hasReceipt = product.hasReceipt;

                return hasReceipt;
            }

            public bool IsSubscribed(string productId) {
                SubscriptionInfo subscriptionInfo = GetSubscriptionInfo(productId);

                if (subscriptionInfo != null) {
                    return subscriptionInfo.isSubscribed() == UnityEngine.Purchasing.Result.True;
                }

                return false;
            }

            public DateTime GetSubscriptionPurchaseDate(string productId) {
                SubscriptionInfo subscriptionInfo = GetSubscriptionInfo(productId);

                if (subscriptionInfo != null) {
                    return subscriptionInfo.getPurchaseDate();
                }

                return default;
            }

            public T GetExtension<T>() where T : IStoreExtension {
                return extensions.GetExtension<T>();
            }

            public UnityEngine.Purchasing.SubscriptionInfo GetSubscriptionInfo(string productId) {
                if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer) {
                    Log.Warning("Getting subscription info is only available on Android and iOS.");
                    return null;
                }

                Product product = GetProduct(productId);

                if (product.definition.type != ProductType.Subscription) {
                    Log.Debug("Couldn't get subscription info: this product is not a subscription product.");
                    return null;
                }

                if (!product.hasReceipt) {
                    Log.Debug("Couldn't get subscription info: this product doesn't have a valid receipt.");
                    return null;
                }

                if (Application.platform == RuntimePlatform.Android) {
                    SubscriptionManager subscriptionManager = new SubscriptionManager(product, null);
                    return subscriptionManager.getSubscriptionInfo();
                }
                else {
                    IAppleExtensions appleExtensions = GetExtension<IAppleExtensions>();
                    Dictionary<string, string> introPriceDictionary = appleExtensions.GetIntroductoryPriceDictionary();
                    string introJson = null;

                    if (introPriceDictionary != null && introPriceDictionary.ContainsKey(product.definition.storeSpecificId)) {
                        introJson = introPriceDictionary[product.definition.storeSpecificId];
                    }

                    SubscriptionManager subscriptionManager = new SubscriptionManager(product, introJson);
                    return subscriptionManager.getSubscriptionInfo();
                }
            }
        }

        private class ProductTransaction : IDisposable {
            private StoreListener storeListener;
            private bool isTransacting;
            private string productId;
            private Action onPurchased;
            private Action<string> onPurchaseFailed;
            private Action<string[]> onRestorePurchased;
            private Action<string> onRestorePurchaseFailed;

            public bool IsTransacting => isTransacting;

            public ProductTransaction(StoreListener storeListener) {
                this.storeListener = storeListener;
                this.isTransacting = false;

                storeListener.onPurchased += OnPurchased;
                storeListener.onPurchaseFailed += OnPurchasFailed;
                storeListener.onRestorePurchased += OnRestorePurchased;
                storeListener.onRestorePurchaseFailed += OnRestorePurchaseFailed;
            }

            public void Dispose() {
                storeListener.onPurchased -= OnPurchased;
                storeListener.onPurchaseFailed -= OnPurchasFailed;
                storeListener.onRestorePurchased -= OnRestorePurchased;
                storeListener.onRestorePurchaseFailed -= OnRestorePurchaseFailed;
                isTransacting = false;
                storeListener = null;
            }

            public bool StartTransaction(string productId, Action onCompleted, Action<string> onFailed) {
                if (isTransacting) {
                    onFailed?.Invoke("The transaction is under process. Please try again later.");
                    return false;
                } else {
                    if (string.IsNullOrEmpty(productId)) {
                        onFailed?.Invoke("Start transaction failed! Product id is null or empty");
                        return false;
                    } else {
                        this.productId = productId;
                        this.onPurchased = onCompleted;
                        this.onPurchaseFailed = onFailed;
                        isTransacting = true;
                        storeListener.InitiatePurchase(productId);
                        return true;
                    }
                }
            }

            public bool StartRestorePurchase(Action<string[]> onCompleted, Action<string> onFailed) {
                if (isTransacting) {
                    onFailed?.Invoke("The transaction is under process. Please try again later.");
                    return false;
                } else {
                    this.onRestorePurchased = onCompleted;
                    this.onRestorePurchaseFailed = onFailed;
                    isTransacting = true;
                    storeListener.RestorePurchases();
                    return true;
                }
            }

            private void OnPurchased(string productId) {
                if (string.IsNullOrEmpty(this.productId)) {
                    isTransacting = false;
                    onPurchaseFailed?.Invoke("Purchase finished with error! Product id is null or empty");
                } else if (this.productId.Equals(productId)) {
                    isTransacting = false;
                    onPurchased?.Invoke();
                }
            }

            private void OnPurchasFailed(string productId, string error) {
                if (string.IsNullOrEmpty(this.productId)) {
                    isTransacting = false;
                    onPurchaseFailed?.Invoke("Purchase finished with error! Product id is null or empty");
                } else if (this.productId.Equals(productId)) {
                    isTransacting = false;
                    onPurchaseFailed?.Invoke(error);
                }
            }

            private void OnRestorePurchased(string[] products) {
                isTransacting = false;
                onRestorePurchased?.Invoke(products);
            }

            private void OnRestorePurchaseFailed(string error) {
                isTransacting = false;
                onRestorePurchaseFailed?.Invoke(error);
            }
        }
#endif


#if IAP
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE;
            public override InitializeEvent InitializeEvent => IAPManager.initializeEvent;

            public override void Initialize() {
                IAPManager.Initialize();
            }
        }
#endif
    }
}
