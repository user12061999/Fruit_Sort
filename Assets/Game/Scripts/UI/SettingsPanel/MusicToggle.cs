using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HAVIGAME.Audios;

public class MusicToggle : AudioButton {
    protected override bool Volume {
        get => AudioManager.Instance.MusicVolume > 0;
        set => AudioManager.Instance.MusicVolume = value ? 1 : 0;
    }
}
