using HAVIGAME;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRecycleParticle : MonoBehaviour {
    private void OnParticleSystemStopped() {
        this.Recycle();
    }
}
