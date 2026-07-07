using HAVIGAME.UI;
using UnityEngine;
using UnityEngine.UI;

public class RatePanel : UIFrame {
    [Header("[References]")]
    [SerializeField] private Slider starSlider;
    [SerializeField] private Button btnRate;

    private void Start() {
        btnRate.onClick.AddListener(Rate);
        starSlider.onValueChanged.AddListener(OnRateChanged);
    }

    protected override void OnShow(bool instant = false) {
        base.OnShow(instant);

        starSlider.wholeNumbers = false;
        starSlider.value = starSlider.maxValue;
    }

    protected override void OnBack() {

    }

    private void OnRateChanged(float value) {
        int star = Mathf.CeilToInt(value);

        if (star < 1) star = 1;

        float sliderValue = star;

        if (Mathf.Abs(starSlider.value - sliderValue) > Mathf.Epsilon) {
            starSlider.SetValueWithoutNotify(sliderValue);
        }
    }

    private void Rate() {
        int star = Mathf.CeilToInt(starSlider.value);
        int rateFilter = GameRemoteConfig.RateFilter;

        GameAnalytics.LogEvent(GameAnalytics.GameEvent.Create("rate").Add("star", star));

        if (star >= rateFilter) {
            OpenStoreByUrl();
        }

        Hide();

        GameData.Player.OnRate(star);
    }

    private void OpenStoreByUrl() {
        GameAdvertising.FixAppOpenAdCooldown();

        string url = "market://details?id=" + Application.identifier;
        Application.OpenURL(url);
    }
}
