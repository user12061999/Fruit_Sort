using DG.Tweening;
using UnityEngine;

namespace FruitSort
{
    [DisallowMultipleComponent]
    public class ConveyorGrinder : MonoBehaviour
    {
        public Transform crushPoint;
        [Min(0.05f)] public float jumpDuration = 0.22f;
        [Min(0f)] public float jumpHeight = 0.5f;

        public Vector3 EntryPosition => crushPoint != null ? crushPoint.position : transform.position;

        public bool TryCrush(Dot dot)
        {
            if (dot == null || !isActiveAndEnabled || dot.capturedByGrinder) return false;
            dot.capturedByGrinder = true;
            dot.markedForRemoval = true;
            dot.targetBucket = null;
            dot.transform.DOKill();
            dot.transform.SetParent(transform, true);
            dot.transform.DOJump(EntryPosition, jumpHeight, 1, jumpDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => { if (dot != null) Destroy(dot.gameObject); });
            return true;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.85f, 0.2f, 0.15f, 0.9f);
            Gizmos.DrawWireCube(transform.position, new Vector3(1.1f, 0.8f, 0f));
            Gizmos.DrawLine(transform.position + new Vector3(-0.35f, 0.25f), transform.position + new Vector3(0.35f, -0.25f));
            Gizmos.DrawLine(transform.position + new Vector3(-0.35f, -0.25f), transform.position + new Vector3(0.35f, 0.25f));
        }
    }
}
