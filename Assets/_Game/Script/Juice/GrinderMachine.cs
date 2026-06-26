using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Máy xay: nhận bucket đầy (do BucketWorker mang tới), "xay" trong grindDuration giây,
    /// rồi rót ra một bình nước cùng màu với bucket vào BottleRack.
    /// Nếu đang bận hoặc giá đầy, bucket được xếp hàng chờ.
    /// </summary>
    public class GrinderMachine : MonoBehaviour
    {
        public static GrinderMachine Instance { get; private set; }

        [Header("Refs")]
        [Tooltip("Giá đựng bình để rót nước ra.")]
        public BottleRack rack;
        [Tooltip("Điểm hiển thị bucket khi đang xay. Để trống = dùng transform máy xay.")]
        public Transform intakePoint;

        [Header("Cấu hình")]
        [Tooltip("Thời gian xay 1 bucket (giây).")]
        public float grindDuration = 1.2f;
        [Tooltip("Thể tích nước tạo ra từ mỗi bucket.")]
        public float volumePerBucket = 1f;

        [Header("Hiệu ứng")]
        [Tooltip("Particle khi đang xay (tuỳ chọn).")]
        public ParticleSystem grindParticle;
        [Tooltip("Phần thân máy để rung khi xay (tuỳ chọn).")]
        public Transform shakeBody;

        readonly Queue<Bucket> _queue = new Queue<Bucket>();
        bool _busy;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        /// <summary>BucketWorker gọi khi đặt một bucket đầy vào máy xay.</summary>
        public void ProcessBucket(Bucket bucket)
        {
            if (bucket == null) return;
            _queue.Enqueue(bucket);
            if (!_busy) TryStartNext();
        }

        void TryStartNext()
        {
            // Bỏ qua bucket đã bị hủy.
            while (_queue.Count > 0 && _queue.Peek() == null) _queue.Dequeue();
            if (_queue.Count == 0) { _busy = false; return; }

            // Nếu giá đầy, chờ rồi thử lại.
            if (rack != null && !rack.HasRoom())
            {
                _busy = true;
                DOVirtual.DelayedCall(0.5f, TryStartNext);
                return;
            }

            _busy = true;
            Bucket bucket = _queue.Dequeue();
            StartGrind(bucket);
        }

        void StartGrind(Bucket bucket)
        {
            Color color = bucket.color;

            // Đưa bucket vào miệng máy.
            Vector3 intake = intakePoint != null ? intakePoint.position : transform.position;
            bucket.transform.SetParent(transform);
            bucket.transform.DOKill();
            bucket.transform.DOMove(intake, 0.25f).SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    // Hủy bucket (đã được xay).
                    bucket.transform.DOScale(Vector3.zero, 0.2f)
                        .OnComplete(() => { if (bucket != null) Destroy(bucket.gameObject); });

                    if (grindParticle != null) grindParticle.Play();
                    if (shakeBody != null)
                    {
                        shakeBody.DOKill();
                        shakeBody.DOShakePosition(grindDuration, 0.08f, 20, 90f, false, false);
                    }

                    DOVirtual.DelayedCall(grindDuration, () => FinishGrind(color));
                });
        }

        void FinishGrind(Color color)
        {
            if (grindParticle != null) grindParticle.Stop();

            if (rack != null)
                rack.DispenseJuice(color, volumePerBucket);

            // Xử lý bucket tiếp theo.
            TryStartNext();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
