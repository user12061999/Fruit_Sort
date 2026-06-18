using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

/// <summary>
/// Wrapper around Unity's SplineContainer to provide convenient methods
/// for querying positions along the conveyor belt path.
/// </summary>
[RequireComponent(typeof(SplineContainer))]
public class ConveyorSpline : MonoBehaviour
{
    [Header("Belt Settings")]
    public float beltWidth = 2f;       // Total width of the belt (left to right)
    public float scrollSpeed = 0.05f;  // How fast dots move along spline (progress/sec)

    private SplineContainer splineContainer;
    private Spline mainSpline;

    private void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        if (splineContainer != null && splineContainer.Splines.Count > 0)
        {
            mainSpline = splineContainer.Splines[0];
        }
        else
        {
            Debug.LogWarning("[ConveyorSpline] No spline found on SplineContainer!");
        }
    }

    /// <summary>
    /// Get world position on the spline at normalized progress t (0..1).
    /// lateralOffset shifts perpendicular to spline direction (clamped by beltWidth/2).
    /// </summary>
    public Vector3 GetPositionOnSpline(float t, float lateralOffset = 0f)
    {
        if (mainSpline == null) return transform.position;

        t = Mathf.Clamp01(t);
        lateralOffset = Mathf.Clamp(lateralOffset, -beltWidth * 0.5f, beltWidth * 0.5f);

        // Evaluate position and tangent on spline
        SplineUtility.Evaluate(mainSpline, t, out float3 pos, out float3 tangent, out float3 normal);

        Vector3 worldPos = splineContainer.transform.TransformPoint(new Vector3(pos.x, pos.y, pos.z));

        // Perpendicular direction (2D: rotate tangent 90 degrees)
        Vector3 tangentWorld = splineContainer.transform.TransformDirection(new Vector3(tangent.x, tangent.y, tangent.z));
        Vector3 perp = new Vector3(-tangentWorld.y, tangentWorld.x, 0f).normalized;

        return worldPos + perp * lateralOffset;
    }

    /// <summary>
    /// Get the tangent direction at progress t (world space, normalized).
    /// </summary>
    public Vector3 GetTangentAt(float t)
    {
        if (mainSpline == null) return Vector3.right;

        t = Mathf.Clamp01(t);
        SplineUtility.Evaluate(mainSpline, t, out float3 pos, out float3 tangent, out float3 normal);

        Vector3 tangentWorld = splineContainer.transform.TransformDirection(new Vector3(tangent.x, tangent.y, tangent.z));
        return tangentWorld.normalized;
    }

    /// <summary>
    /// Get total spline length in world units.
    /// </summary>
    public float GetSplineLength()
    {
        if (mainSpline == null) return 0f;
        return SplineUtility.CalculateLength(mainSpline, splineContainer.transform.localToWorldMatrix);
    }

    /// <summary>
    /// Get the normalized progress (0..1) of a world position projected onto the spline.
    /// Uses nearest-point approximation.
    /// </summary>
    public float GetClosestProgress(Vector3 worldPos)
    {
        if (mainSpline == null) return 0f;

        // Transform world pos to local spline space
        Vector3 localPos = splineContainer.transform.InverseTransformPoint(worldPos);
        SplineUtility.GetNearestPoint(mainSpline, new float3(localPos.x, localPos.y, localPos.z),
            out float3 nearest, out float t);

        return t;
    }
}
