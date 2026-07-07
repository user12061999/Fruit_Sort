using UnityEngine;
using HAVIGAME;

[ExecuteAlways]
public class CameraController : Singleton<CameraController>
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Vector2 widthRange = new Vector2(0.1f, 0.9f);
    [SerializeField] private Vector2 heightRange = new Vector2(0.1f, 0.9f);

    public override bool IsDontDestroyOnLoad => false;
    public Camera MainCamera => mainCamera;

    public Vector2 HeightRange
    {
        get => heightRange;
        set => heightRange = value;
    }

    private void OnValidate()
    {
        widthRange = NormalizeRange(widthRange);
        heightRange = NormalizeRange(heightRange);

        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        Apply2DMode();
    }

    public void UpdateCamera(Bounds bounds)
    {
        if (mainCamera == null)
        {
            return;
        }

        Apply2DMode();

        Vector2 normalizedWidthRange = NormalizeRange(widthRange);
        Vector2 normalizedHeightRange = NormalizeRange(heightRange);

        float widthPercent = Mathf.Max(0.01f, normalizedWidthRange.y - normalizedWidthRange.x);
        float heightPercent = Mathf.Max(0.01f, normalizedHeightRange.y - normalizedHeightRange.x);
        float safeWidth = Mathf.Max(bounds.size.x, 0.01f);
        float safeHeight = Mathf.Max(bounds.size.y, 0.01f);

        float boundsAspect = safeWidth / safeHeight;
        float cameraAspect = Mathf.Max(mainCamera.aspect, 0.01f);
        float limitCameraAspect = cameraAspect * (widthPercent / heightPercent);

        float cameraWidth;
        float cameraHeight;

        if (boundsAspect > limitCameraAspect)
        {
            cameraWidth = safeWidth / widthPercent;
            cameraHeight = cameraWidth / cameraAspect;
        }
        else
        {
            cameraHeight = safeHeight / heightPercent;
            cameraWidth = cameraHeight * cameraAspect;
        }

        mainCamera.orthographicSize = cameraHeight / 2f;

        float left = (-0.5f + normalizedWidthRange.x) * cameraWidth;
        float right = (normalizedWidthRange.y - 0.5f) * cameraWidth;
        float top = (normalizedHeightRange.y - 0.5f) * cameraHeight;
        float bottom = (-0.5f + normalizedHeightRange.x) * cameraHeight;

        Vector3 offset = new Vector3((left + right) / 2f, (top + bottom) / 2f, 0f);
        Vector3 nextPosition = bounds.center - offset;
        nextPosition.z = mainCamera.transform.position.z;
        mainCamera.transform.position = nextPosition;
    }

    private void Apply2DMode()
    {
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.orthographic = true;
    }

    private static Vector2 NormalizeRange(Vector2 range)
    {
        float min = Mathf.Clamp01(Mathf.Min(range.x, range.y));
        float max = Mathf.Clamp01(Mathf.Max(range.x, range.y));

        if (Mathf.Approximately(min, max))
        {
            max = Mathf.Min(1f, min + 0.01f);
        }

        return new Vector2(min, max);
    }
}
