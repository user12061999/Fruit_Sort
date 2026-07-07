using HAVIGAME.Audios;

public class SoundToggle : AudioButton {
    protected override bool Volume {
        get => AudioManager.Instance.SoundFXVolume > 0;
        set => AudioManager.Instance.SoundFXVolume = value ? 1 : 0;
    }
}
