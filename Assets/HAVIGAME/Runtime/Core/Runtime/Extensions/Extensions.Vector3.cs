using UnityEngine;

namespace HAVIGAME {

    public static partial class Extensions {

        public static float sqrMagnitudeXY(this Vector3 a) {
            return a.x * a.x + a.y * a.y;
        }

        public static float sqrMagnitudeXZ(this Vector3 a) {
            return a.x * a.x + a.z * a.z;
        }

        public static float sqrMagnitudeYZ(this Vector3 a) {
            return a.y * a.y + a.z * a.z;
        }

        public static float magnitudeXY(this Vector3 a) {
            return Mathf.Sqrt(a.sqrMagnitudeXY());
        }

        public static float magnitudeXZ(this Vector3 a) {
            return Mathf.Sqrt(a.sqrMagnitudeXZ());
        }

        public static float magnitudeYZ(this Vector3 a) {
            return Mathf.Sqrt(a.sqrMagnitudeYZ());
        }

        public static float AngleXY(Vector3 a, Vector3 b) {
            float sin = a.x * b.y - b.x * a.y;
            float cos = a.x * b.x + a.y * b.y;
            return Mathf.Atan2(sin, cos) * (180f / Mathf.PI);
        }

        public static float AngleXZ(Vector3 a, Vector3 b) {
            float sin = a.x * b.z - b.x * a.z;
            float cos = a.x * b.x + a.z * b.z;
            return Mathf.Atan2(sin, cos) * (180f / Mathf.PI);
        }

        public static float AngleYZ(Vector3 a, Vector3 b) {
            float sin = a.y * b.z - b.y * a.z;
            float cos = a.y * b.y + a.z * b.z;
            return Mathf.Atan2(sin, cos) * (180f / Mathf.PI);
        }

        public static float DifferentX(Vector3 a, Vector3 b) {
            return Mathf.Abs(a.x - b.x);
        }

        public static float DifferentY(Vector3 a, Vector3 b) {
            return Mathf.Abs(a.y - b.y);
        }

        public static float DifferentZ(Vector3 a, Vector3 b) {
            return Mathf.Abs(a.z - b.z);
        }

        public static Vector3 RotateAround(this Vector3 point, Vector3 pivot, Vector3 angle) {
            return point.RotateAround(pivot, Quaternion.Euler(angle));
        }

        public static Vector3 RotateAround(this Vector3 point, Vector3 pivot, Quaternion rotation) {
            return rotation * (point - pivot) + pivot;
        }

        public static Vector3 RotateAroundAxis(this Vector3 point, float angle, Vector3 axis) {
            return Quaternion.AngleAxis(angle, axis) * point;
        }
    }
}
