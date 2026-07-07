using UnityEngine;
using Newtonsoft.Json.Linq;

namespace HAVIGAME.Services.IAP {

    [DefineSymbols(IAPManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(IAPSettings), "Services/In-App Puchasing", "", null, 101, "Icons/icon_iap.psd")]
    [CreateAssetMenu(fileName = "IAPSettings", menuName = "HAVIGAME/Settings/Services/In-App Purchasing")]
    public class IAPSettings : Settings<IAPSettings> {

        [SerializeField] private bool gameServiceEnabled = false;
        [Header("[Validator]")]
        [SerializeReference, Subclass] private IAPValidator validator;
        [Header("[Products]")]
        [SerializeField] private IAPProduct[] products;
        [Header("[Taxs]")]
        [SerializeField, TextArea(3,5), ButtonField("Update", "UpdateTaxs", 60)] private string taxJsonText;
        [SerializeField] private TaxInfo[] taxInfos;

        public bool GameServiceEnabled => gameServiceEnabled;
        public IAPValidator Validator => validator;
        public IAPProduct[] Products => products;
        public TaxInfo[] Taxs => taxInfos;

        public IAPProduct GetProduct(string productId) {
            foreach (IAPProduct product in products) {
                if (product.Id.Equals(productId)) {
                    return product;
                }
            }

            return null;
        }

        private void UpdateTaxs() {
            JArray array = JArray.Parse(taxJsonText);

            if (array != null) {
                taxInfos = new TaxInfo[array.Count];

                for (int i = 0; i < array.Count; i++) {
                    taxInfos[i] = new TaxInfo((string)array[i]["country"], (int)array[i]["tax_rate"]);
                }
            }
        }
    }

    [System.Serializable]
    public class TaxInfo {
        public string countryCode;
        public int taxRate;

        public TaxInfo(string countryCode, int taxRate) {
            this.countryCode = countryCode;
            this.taxRate = taxRate;
        }
    }
}
