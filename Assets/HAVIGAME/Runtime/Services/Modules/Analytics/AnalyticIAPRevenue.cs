namespace HAVIGAME.Services.Analytics {
    public struct AnalyticIAPRevenue {
        private string productId;
        private int quantity;
        private decimal value;
        private string currencyCode;

        public string ProductId => productId;
        public int Quantity => quantity;
        public decimal Value => value;
        public string CurrencyCode => currencyCode;


        public AnalyticIAPRevenue(string productId, int quantity, decimal value, string currencyCode) {
            this.productId = productId;
            this.quantity = quantity;
            this.value = value;
            this.currencyCode = currencyCode;
        }
    }
}
