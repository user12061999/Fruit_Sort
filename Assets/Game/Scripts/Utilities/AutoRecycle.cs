using HAVIGAME;
using System.Collections;
using UnityEngine;

public class AutoRecycle : MonoBehaviour {
    [SerializeField] private float lifeTime;

    private Coroutine coroutine;
    private WaitForSeconds wait;

    private void Awake() {
        wait = new WaitForSeconds(lifeTime);
    }

    private void OnEnable() {
        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(IERecycle());
    }

    private IEnumerator IERecycle() {
        yield return wait;
        coroutine = null;
        this.Recycle();
    }
}
