using HAVIGAME.Audios;

public class VibrateToggle : AudioButton {
    protected override bool Volume {
        get => AudioManager.Instance.VibrateEnabled;
        set => AudioManager.Instance.VibrateEnabled = value;
    }
}