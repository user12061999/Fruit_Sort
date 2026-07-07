using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSelectFill : MonoBehaviour {
    [SerializeField] private Image background;
    [SerializeField] private RectTransform active;
    [SerializeField] private int capacity;
    [SerializeField] private int current;

    private void OnValidate() {
        SetCapacity(capacity);
    }

    public void SetCapacity(int capacity) {
        this.capacity = capacity;

        int texWidth = background.sprite.texture.width;
        int texHeight = background.sprite.texture.height;

        int width = texWidth * capacity;
        int height = texHeight;

        RectTransform backgroundRect = background.transform as RectTransform;
        backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(width, height);

        SetCurrent(current);
    }

    public void SetCurrent(int current) {
        this.current = current;

        float step = 1f / capacity;
        float position = step * (current - 0.5f);

        active.anchorMin = active.anchorMax = new Vector2(position, 0.5f);
        active.anchoredPosition = Vector2.zero;
    }
}
