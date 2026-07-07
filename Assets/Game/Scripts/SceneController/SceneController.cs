using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SceneController : MonoBehaviour {
    private void Start() {
        OnSceneStart();
    }

    protected abstract void OnSceneStart();
}
