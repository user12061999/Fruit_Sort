namespace HAVIGAME.Services.Analytics {
    public struct AnalyticAdRevenue {
        private string adPlatform;
        private string adSource;
        private string adUnit;
        private string adFormat;
        private double value;
        private string currencyCode;

        public string AdPlatform  => adPlatform;
        public string AdSource  => adSource;
        public string AdUnit  => adUnit; 
        public string AdFormat  => adFormat;
        public double Value  => value;
        public string CurrencyCode  => currencyCode;

        public AnalyticAdRevenue(string adPlatform, string adSource, string adUnit, string adFormat, double value, string currencyCode) {
            this.adPlatform = adPlatform;
            this.adSource = adSource;
            this.adUnit = adUnit;
            this.adFormat = adFormat;
            this.value = value;
            this.currencyCode = currencyCode;
        }
    }
}
