namespace HAVIGAME.Services.Advertisings {
    public delegate void AdDelegate(AdEventArgs args);
    public delegate void AdRevenuePaidDelegate(AdFormat adFormat, AdRevenuePaid adRevenuePaid);
    public delegate void AdClientRevenuePaidDelegate(AdService client, AdFormat adFormat, AdRevenuePaid adRevenuePaid);
    public delegate void AdClientInitializeDelegate(AdService client, bool isInitialized);

    public sealed class AdEventArgs : IReferencePoolable {
        private AdNetwork adNetwork;
        private AdFormat adFormat;
        private string adUnitId;
        private int adUniqueId;
        private string placement;
        private string error;

        public AdNetwork AdNetwork => adNetwork;
        public AdFormat AdFormat => adFormat;
        public string AdUnitId => adUnitId;
        public int AdUniqueId => adUniqueId;
        public string Placement => placement;
        public string Error => error;

        public override string ToString() {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append($"\n ad_network: {adNetwork}");
            sb.Append($"\n ad_format: {adFormat}");
            sb.Append($"\n ad_unit_id: {adUnitId}");
            sb.Append($"\n ad_unique_id: {adUniqueId}");
            sb.Append($"\n placement: {placement}");
            sb.Append($"\n error: {error}");
            return sb.ToString();
        }

        public void Clear() {

        }

        public void SetParam(AdNetwork adNetwork, AdFormat adFormat, string adUnitId, int adUniqueId, string placement = "", string error = "") {
            this.adNetwork = adNetwork;
            this.adFormat = adFormat;
            this.adUnitId = adUnitId;
            this.adUniqueId = adUniqueId;
            this.placement = placement;
            this.error = error;
        }
    }
}
