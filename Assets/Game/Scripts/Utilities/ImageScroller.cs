using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ImageScroller : MonoBehaviour {
    [SerializeField] private RawImage image;
    [SerializeField] private Vector2 scrollSpeed;

    private Rect rect;

    private void Start() {
        rect = image.uvRect;
    }

    private void Reset() {
        image = GetComponent<RawImage>();
    }

    private void Update() {
        if (image != null) {
            rect.x += scrollSpeed.x * Time.deltaTime;
            rect.y += scrollSpeed.y * Time.deltaTime;

            image.uvRect = rect;
        }
    }
}
