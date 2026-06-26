using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace FruitSort
{
    /// <summary>
    /// Nhân vật đứng chờ tại idlePoint. Khi có bucket đầy, di chuyển đến nhặt lên,
    /// mang đến dropZone, đặt xuống rồi quay về chờ tiếp.
    /// </summary>
    public class BucketWorker : MonoBehaviour
    {
        [Header("Điểm chờ và điểm đặt")]
        public Transform idlePoint;
        public Transform dropZone;

        [Header("Máy xay")]
        [Tooltip("Máy xay để giao bucket. Để trống = tự tìm GrinderMachine.Instance. " +
                 "Nếu không có máy xay, bucket bị hủy như cũ.")]
        public GrinderMachine grinder;

        [Header("Di chuyển")]
        public float moveSpeed = 4f;

        [Header("Vị trí cầm bucket (local offset)")]
        public Vector3 carryOffset = new Vector3(0f, 0.7f, 0f);

        enum State { Idle, MovingToPickup, WaitingForPickup, MovingToDropZone, Dropping, ReturningToIdle }
        State _state = State.Idle;

        readonly Queue<Bucket> _queue = new Queue<Bucket>();
        Bucket _target;

        void OnEnable()  => Bucket.OnBucketFull += HandleBucketFull;
        void OnDisable() => Bucket.OnBucketFull -= HandleBucketFull;

        void Start()
        {
            if (idlePoint != null)
                transform.position = idlePoint.position;
        }

        void HandleBucketFull(Bucket b)
        {
            _queue.Enqueue(b);
            if (_state == State.Idle)
                ProcessNext();
        }

        void ProcessNext()
        {
            // Bỏ qua bucket đã bị destroy
            while (_queue.Count > 0 && _queue.Peek() == null)
                _queue.Dequeue();

            if (_queue.Count == 0) { _state = State.Idle; return; }

            _target = _queue.Dequeue();
            MoveToPickup();
        }

        void MoveToPickup()
        {
            _state = State.MovingToPickup;
            float dur = Vector2.Distance(transform.position, _target.transform.position) / moveSpeed;
            transform.DOKill();
            transform.DOMove(_target.transform.position, Mathf.Max(0.05f, dur))
                .SetEase(Ease.Linear)
                .OnComplete(OnReachedBucket);
        }

        void OnReachedBucket()
        {
            if (_target == null) { ProcessNext(); return; }

            if (!_target.IsReadyForPickup)
            {
                _state = State.WaitingForPickup;
                return;
            }
            PickUp();
        }

        void Update()
        {
            if (_state != State.WaitingForPickup) return;
            if (_target == null) { ProcessNext(); return; }
            if (_target.IsReadyForPickup) PickUp();
        }

        void PickUp()
        {
            _target.BePickedUp();
            _target.transform.SetParent(transform);
            _target.transform.localPosition = carryOffset;
            _target.transform.localScale = Vector3.one * 0.8f;

            _state = State.MovingToDropZone;
            Vector3 dest = dropZone != null ? dropZone.position : transform.position + Vector3.right * 4f;
            float dur = Vector2.Distance(transform.position, dest) / moveSpeed;
            transform.DOKill();
            transform.DOMove(dest, Mathf.Max(0.05f, dur))
                .SetEase(Ease.Linear)
                .OnComplete(Drop);
        }

        void Drop()
        {
            _state = State.Dropping;
            if (_target != null)
            {
                _target.transform.SetParent(null);
                Bucket b = _target;
                _target = null;

                GrinderMachine g = grinder != null ? grinder : GrinderMachine.Instance;
                if (g != null)
                {
                    // Giao bucket cho máy xay; máy xay sẽ xay rồi rót ra bình màu và hủy bucket.
                    g.ProcessBucket(b);
                }
                else
                {
                    // Không có máy xay: hủy như cũ.
                    b.transform.DOScale(Vector3.zero, 0.25f)
                        .OnComplete(() => { if (b != null) Destroy(b.gameObject); });
                }
            }
            ReturnToIdle();
        }

        void ReturnToIdle()
        {
            _state = State.ReturningToIdle;
            Vector3 idlePos = idlePoint != null ? idlePoint.position : Vector3.zero;
            float dur = Vector2.Distance(transform.position, idlePos) / moveSpeed;
            transform.DOKill();
            transform.DOMove(idlePos, Mathf.Max(0.05f, dur))
                .SetEase(Ease.Linear)
                .OnComplete(() => ProcessNext());
        }
    }
}
