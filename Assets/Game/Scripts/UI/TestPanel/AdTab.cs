using UnityEngine;

public class AdTab : MonoBehaviour {
    public void ShowAppOpenAd() {
        GameAdvertising.TryShowAppOpenAd();
    }
    public void ShowRewardedAd() {
        GameAdvertising.TryShowRewardedAd();
    }
    public void ShowInterstitialAd() {
        GameAdvertising.TryShowInterstitialAd();
    }
    public void ShowRewardedInterstitialAd() {
        GameAdvertising.TryShowRewardedInterstitialAd();
    }
    public void ShowBannerAd() {
        GameAdvertising.TryShowBannerAd(GameAdvertising.GameAdPosition.BottomCenter);
    }
    public void HideBannerAd() {
        GameAdvertising.TryHideBannerAd();
    }
    public void ShowMrectAd() {
        GameAdvertising.TryShowMediumRectangleAd(GameAdvertising.GameAdPosition.BottomCenter);
    }
    public void HideMrectAd() {
        GameAdvertising.TryHideMediumRectangleAd();
    }
    public void ShowNativeOverlayAd() {
        //GameAdvertising.TryShowNativeOverlayAd(GameAdvertising.GameAdPosition.BottomCenter);
    }
    public void HideNativeOverlayAd() {
        //GameAdvertising.TryHideNativeOverlayAd();
    }
}
