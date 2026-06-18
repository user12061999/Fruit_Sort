using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PixelGridManager : MonoBehaviour
{
    [Header("Grid Source")]
    public Dot dotPrefab;
    public Texture2D sourceTexture;
    public List<Color> colorPalette = new List<Color>();
    public bool buildOnStart = true;
    public bool skipTransparentPixels = true;
    [Range(0f, 1f)] public float alphaThreshold = 0.1f;

    [Header("Generated Grid")]
    public int fallbackWidth = 8;
    public int fallbackHeight = 8;
    [Min(0.01f)] public float dotSize = 0.25f;
    [Min(1)] public int defaultHp = 3;
    public Transform dotRoot;
    public List<Dot> gridDots = new List<Dot>();

    public int RemainingGridDots => gridDots.Count;

    private void Start()
    {
        if (buildOnStart && gridDots.Count == 0)
        {
            BuildGrid();
        }
    }

    [ContextMenu("Build Grid")]
    public void BuildGrid()
    {
        if (dotPrefab == null)
        {
            Debug.LogError("PixelGridManager requires a Dot prefab.", this);
            return;
        }

        ClearGrid();
        int width = sourceTexture != null ? sourceTexture.width : Mathf.Max(1, fallbackWidth);
        int height = sourceTexture != null ? sourceTexture.height : Mathf.Max(1, fallbackHeight);
        Transform parent = dotRoot != null ? dotRoot : transform;
        Vector2 origin = new Vector2(-(width - 1) * dotSize * 0.5f,
            -(height - 1) * dotSize * 0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = sourceTexture != null ? sourceTexture.GetPixel(x, y) : Color.white;
                if (skipTransparentPixels && pixel.a <= alphaThreshold)
                {
                    continue;
                }

                Dot dot = Instantiate(dotPrefab, parent);
                dot.transform.localPosition = new Vector3(origin.x + x * dotSize,
                    origin.y + y * dotSize, 0f);
                dot.transform.localScale = Vector3.one * dotSize;
                dot.Initialize(FindClosestColorId(pixel), defaultHp, new Vector2Int(x, y));
                dot.gridOwner = this;
                ApplySpriteColor(dot, pixel);
                gridDots.Add(dot);
            }
        }
    }

    public void ReleaseDot(Dot dot)
    {
        if (dot == null || !gridDots.Remove(dot))
        {
            return;
        }

        dot.transform.SetParent(null, true);
        dot.gridOwner = null;
        if (FallingPixelManager.Instance != null)
        {
            FallingPixelManager.Instance.AddDot(dot);
        }
        else
        {
            Debug.LogError("No FallingPixelManager exists in the scene.", this);
            Destroy(dot.gameObject);
        }
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        for (int i = gridDots.Count - 1; i >= 0; i--)
        {
            if (gridDots[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(gridDots[i].gameObject);
                }
                else
                {
                    DestroyImmediate(gridDots[i].gameObject);
                }
            }
        }

        gridDots.Clear();
    }

    private int FindClosestColorId(Color color)
    {
        if (colorPalette.Count == 0)
        {
            return 0;
        }

        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < colorPalette.Count; i++)
        {
            Vector3 delta = new Vector3(color.r - colorPalette[i].r,
                color.g - colorPalette[i].g, color.b - colorPalette[i].b);
            float distance = delta.sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static void ApplySpriteColor(Dot dot, Color color)
    {
        SpriteRenderer spriteRenderer = dot.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}
