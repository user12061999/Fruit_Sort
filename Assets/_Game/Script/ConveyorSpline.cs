using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public class ConveyorSpline : MonoBehaviour
{
    public SplineContainer splineContainer;
    [Min(0.01f)] public float beltWidth = 4f;

    public float HalfWidth => beltWidth * 0.5f;

    private void Reset()
    {
        splineContainer = GetComponent<SplineContainer>();
    }

    private void Awake()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
        }
    }

    public Vector3 GetPositionOnSpline(float t, float lateralOffset)
    {
        if (splineContainer == null || splineContainer.Spline.Count < 2)
        {
            return transform.position;
        }

        float clampedT = Mathf.Clamp01(t);
        float3 position = splineContainer.EvaluatePosition(clampedT);
        float3 tangent3 = splineContainer.EvaluateTangent(clampedT);
        Vector2 tangent = new Vector2(tangent3.x, tangent3.y).normalized;
        if (tangent.sqrMagnitude < 0.001f)
        {
            tangent = Vector2.right;
        }

        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        float clampedOffset = Mathf.Clamp(lateralOffset, -HalfWidth, HalfWidth);
        return new Vector3(position.x + normal.x * clampedOffset,
            position.y + normal.y * clampedOffset, position.z);
    }

    public Vector2 GetNormalOnSpline(float t)
    {
        if (splineContainer == null || splineContainer.Spline.Count < 2)
        {
            return Vector2.up;
        }

        float3 tangent3 = splineContainer.EvaluateTangent(Mathf.Clamp01(t));
        Vector2 tangent = new Vector2(tangent3.x, tangent3.y).normalized;
        return tangent.sqrMagnitude < 0.001f
            ? Vector2.up
            : new Vector2(-tangent.y, tangent.x);
    }

    public float GetSplineLength()
    {
        return splineContainer == null ? 0f : splineContainer.CalculateLength();
    }
}
