using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Module Máy Xay: khi quả (có FruitComponent) đi vào trigger,
    /// hủy quả và tạo PaintStream với cmy = fruit.CMYVector, volume = 1.
    /// Nếu đã có PaintStream gần đó, cộng dồn thay vì tạo mới.
    /// </summary>
    public class BlenderSlot : ModuleSlot
    {
        [Header("Blender Settings")]
        [Tooltip("Bán kính dò dot trên băng để xay (dot không có collider nên poll bằng khoảng cách).")]
        public float blendRadius = 0.6f;
        [Tooltip("Bán kính tìm PaintStream lân cận để cộng dồn.")]
        public float mergeRadius = 0.8f;
        [Tooltip("Volume tạo ra mỗi quả.")]
        public float volumePerFruit = 1f;
        [Tooltip("FruitDatabase để tra CMY từ colorId của Dot.")]
        public FruitDatabase fruitDatabase;
        [Tooltip("Hiệu ứng particle khi xay (tuỳ chọn).")]
        public ParticleSystem blendParticle;

        protected override void Awake()
        {
            base.Awake();
            // Tự tìm FruitDatabase trong project nếu chưa gán
            if (fruitDatabase == null)
            {
                var dbs = Resources.FindObjectsOfTypeAll<FruitDatabase>();
                if (dbs != null && dbs.Length > 0) fruitDatabase = dbs[0];
            }
        }

        protected override void OnStreamEnter(PaintStream stream) { }

        /// <summary>
        /// Poll các dot đang chạy trên băng gần slot và xay thành PaintStream.
        /// Dùng polling thay cho OnTriggerEnter2D vì dot do FallingPixelManager điều khiển
        /// bằng transform, KHÔNG có Collider2D/Rigidbody2D nên không sinh trigger event.
        /// </summary>
        protected override void OnUpdate()
        {
            if (currentModule == null || currentModule.moduleType != ModuleType.Blender) return;

            var fm = FallingPixelManager.Instance;
            if (fm == null) return;

            var dots = fm.ActiveDots;
            for (int i = 0; i < dots.Count; i++)
            {
                var dot = dots[i];
                if (dot == null || dot.markedForRemoval) continue;
                if (dot.state != DotState.OnBelt) continue;           // chỉ xay dot đang trên băng
                if (Vector2.Distance(transform.position, dot.transform.position) > blendRadius) continue;

                var fruit = dot.GetComponent<FruitComponent>();
                if (fruit != null) BlendFruit(fruit);
                else BlendDot(dot);
            }
        }

        void BlendFruit(FruitComponent fruit)
        {
            Vector3 cmy = fruit.CMYVector;
            PaintStream nearby = FindNearbyStream(mergeRadius);
            if (nearby != null)
                nearby.AddPaint(cmy, volumePerFruit);
            else
                SpawnPaintStream(cmy, volumePerFruit);

            PlayBlendEffect(fruit.transform.position);

            // KHÔNG Destroy trực tiếp: nếu dot được FallingPixelManager quản lý, chỉ đánh dấu
            // để RemoveDead() hủy an toàn (tránh fake-null trong _dots gây MissingReferenceException).
            var fdot = fruit.GetComponent<Dot>();
            if (fdot != null) fdot.markedForRemoval = true;
            else Destroy(fruit.gameObject);
        }

        void BlendDot(Dot dot)
        {
            // Tra FruitDatabase trước; fallback về RGB-invert nếu không có
            Vector3 cmy;
            if (fruitDatabase != null)
            {
                var fd = fruitDatabase.GetById(dot.colorId);
                if (fd != null)
                {
                    cmy = (fd.colorType == FruitColorType.Mixed) ? Vector3.one : fd.cmyVector;
                }
                else
                {
                    Color c = dot.color;
                    cmy = new Vector3(1f - c.r, 1f - c.g, 1f - c.b);
                }
            }
            else
            {
                Color c = dot.color;
                cmy = new Vector3(1f - c.r, 1f - c.g, 1f - c.b);
            }

            PaintStream nearby = FindNearbyStream(mergeRadius);
            if (nearby != null)
                nearby.AddPaint(cmy, volumePerFruit);
            else
                SpawnPaintStream(cmy, volumePerFruit);

            PlayBlendEffect(dot.transform.position);

            // Chỉ đánh dấu; FallingPixelManager.RemoveDead() sẽ Destroy an toàn trong cùng bước
            // nó gỡ dot khỏi _dots -> vòng đọc vị trí không bao giờ chạm fake-null.
            dot.markedForRemoval = true;
        }

        void PlayBlendEffect(Vector3 pos)
        {
            if (blendParticle != null)
            {
                blendParticle.transform.position = pos;
                blendParticle.Play();
            }
            // Bounce animation trên slot
            _sr.DOKill();
            _sr.DOColor(activeColor, 0.08f).OnComplete(() => _sr.color = currentModule?.slotColor ?? emptyColor);
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.25f, 0.2f, 5, 0.5f);
        }
    }
}
