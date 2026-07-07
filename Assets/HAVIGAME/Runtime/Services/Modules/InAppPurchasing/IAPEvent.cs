
namespace HAVIGAME.Services.IAP {
    public delegate void IAPDelegate();
    public delegate void IAPErrorDelegate(string error);
    public delegate void IAPPurchaseDelegate(string productId);
    public delegate void IAPPurchaseErrorDelegate(string productId, string error);
    public delegate void IAPRestorePurchaseDelegate(string[] productIds);
}
