using UnityEngine;

namespace FruitSort
{
    /// <summary>Trạng thái vòng đời của 1 dot.</summary>
    public enum DotState
    {
        InGrid = 0,     // Đang nằm trong lưới, có thể bị bắn
        Falling = 1,    // Đã vỡ, đang rơi tự do xuống băng chuyền
        OnBelt = 2,     // Đang chạy dọc băng chuyền (spline)
        Attracting = 3, // Đang bị một bucket hút vào
        Approaching = 4, // Bay THẲNG từ điểm spawn tới miệng băng chuyền (spawn từ model 3D)
        Launched = 5    // Phóng theo hướng cố định, tự phát hiện và lên băng chuyền khi chạm
    }

    /// <summary>
    /// 1 pixel/dot: có màu (ID), HP, trạng thái và các thông số chuyển động runtime.
    /// KHÔNG dùng Rigidbody — mọi chuyển động do FallingPixelManager điều khiển qua Transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class Dot : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("ID màu, trùng với index trong Palette của PixelGridManager và colorId của Bucket.")]
        public int colorId = 0;
        public Color color = Color.white;

        [Header("Health")]
        public int maxHP = 3;
        public int currentHP = 3;

        [Header("State (runtime)")]
        public DotState state = DotState.InGrid;
        public Vector2Int gridPos;

        [Header("Belt motion (runtime, chỉ đọc tham khảo)")]
        [Range(0f, 1f)] public float beltProgress = 0f; // vị trí dọc spline 0..1
        public float lateralOffset = 0f;                // lệch ngang so với tâm băng chuyền
        public float fallSpeed = 0f;                    // vận tốc rơi hiện tại
        public float spin = 0f;                         // tốc độ xoay (độ/giây)
        public Bucket targetBucket;                     // bucket đang hút (nếu có)

        // ---- Bay thẳng vào băng chuyền (state Approaching). Không serialize. ----
        [System.NonSerialized] public Vector3 approachTarget;     // điểm đích trên spline cần bay tới
        [System.NonSerialized] public float beltEntryProgress;    // progress (t) sẽ vào belt khi tới đích
        [System.NonSerialized] public float entryLateral;         // lệch ngang khi vào belt

        // ---- Phóng theo hướng (state Launched). Không serialize. ----
        [System.NonSerialized] public Vector2 launchVelocity;     // vận tốc hiện tại (world unit/giây)

        // Băng chuyền dot đang chạy trên đó (đổi khi đi qua liên kết sang băng kế). Không serialize.
        [System.NonSerialized] public ConveyorSpline conveyor;

        // Hệ số tốc độ riêng từng dot để tạo nhiễu (set khi lên belt). Không serialize.
        [System.NonSerialized] public float beltSpeedFactor = 1f;
        // Cờ đánh dấu để xoá an toàn bằng for-loop ngược. Không serialize.
        [System.NonSerialized] public bool markedForRemoval = false;
        // Đã được bucket "nhận nuôi" (xếp vào giỏ) -> gỡ khỏi manager nhưng KHÔNG destroy. Không serialize.
        [System.NonSerialized] public bool capturedByBucket = false;

        SpriteRenderer _sr;
        public SpriteRenderer Sr
        {
            get { if (_sr == null) _sr = GetComponent<SpriteRenderer>(); return _sr; }
        }

        public bool IsAlive => currentHP > 0;

        // Có sprite riêng theo màu hay không. Nếu có thì KHÔNG tint để giữ nguyên art của sprite.
        [System.NonSerialized] public bool hasColorSprite = false;

        /// <summary>Khởi tạo dot khi sinh ra trong lưới.</summary>
        public void Init(int newColorId, Color newColor, int hp, Vector2Int gp, Sprite newSprite = null)
        {
            colorId = newColorId;
            color = newColor;
            maxHP = Mathf.Max(1, hp);
            currentHP = maxHP;
            gridPos = gp;
            state = DotState.InGrid;
            markedForRemoval = false;
            ApplySprite(newSprite);
            ApplyColor();
        }

        /// <summary>Đổi sprite của dot theo data màu (null = giữ sprite mặc định của prefab).</summary>
        public void ApplySprite(Sprite newSprite)
        {
            hasColorSprite = newSprite != null;
            if (hasColorSprite && Sr != null) Sr.sprite = newSprite;
        }

        /// <summary>Đẩy màu hiện tại ra SpriteRenderer.</summary>
        public void ApplyColor()
        {
            if (Sr == null) return;
            // Có sprite riêng theo màu -> để trắng cho hiện đúng art; ngược lại tint bằng color.
            Sr.color = hasColorSprite ? Color.white : color;
        }

        /// <summary>
        /// Nhận sát thương. Chỉ áp dụng khi dot còn trong lưới.
        /// Trả về true nếu dot vỡ (HP về 0) ở lần này.
        /// </summary>
        public bool TakeDamage(int dmg)
        {
            if (state != DotState.InGrid) return false;
            currentHP -= Mathf.Max(1, dmg);

            // Nhấp nháy nhẹ để feedback trúng đạn (lerp về trắng theo % HP mất).
            if (Sr != null)
            {
                float t = 1f - Mathf.Clamp01(currentHP / (float)maxHP);
                Color baseColor = hasColorSprite ? Color.white : color;
                Sr.color = Color.Lerp(baseColor, Color.white, 0.4f * t + 0.2f);
            }

            if (currentHP <= 0)
            {
                currentHP = 0;
                return true;
            }
            return false;
        }
    }
}
