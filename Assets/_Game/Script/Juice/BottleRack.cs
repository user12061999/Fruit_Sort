using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Giá đựng bình: gồm nhiều ô (slot) cố định. Máy xay gọi DispenseJuice() để rót một bình
    /// màu mới ra giá. Bình đã có sẵn trong scene (đặt làm con của slot) sẽ tự đăng ký homeSlot.
    /// Ưu tiên tái sử dụng bình rỗng; nếu không có thì tạo bình mới ở ô trống.
    /// </summary>
    public class BottleRack : MonoBehaviour
    {
        [Header("Cấu hình")]
        [Tooltip("Prefab JuiceBottle để tạo bình mới khi cần.")]
        public JuiceBottle bottlePrefab;
        [Tooltip("Các ô đặt bình (Transform). Thứ tự = thứ tự rót.")]
        public Transform[] slots;

        // bottle theo từng slot (null = ô trống, chưa có bình nào).
        readonly Dictionary<Transform, JuiceBottle> _occupants = new Dictionary<Transform, JuiceBottle>();

        void Awake()
        {
            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                // Nếu ô đã có sẵn 1 bình (đặt tay trong scene) thì đăng ký nó.
                var existing = slot.GetComponentInChildren<JuiceBottle>();
                _occupants[slot] = existing;
                if (existing != null) existing.homeSlot = slot;
            }
        }

        /// <summary>
        /// Rót một bình màu <paramref name="color"/> ra giá. Trả về bình vừa rót, hoặc null nếu hết chỗ.
        /// </summary>
        public JuiceBottle DispenseJuice(Color color, float volume)
        {
            // 1) Tái sử dụng một bình rỗng đang có trên giá.
            foreach (var slot in slots)
            {
                if (slot == null || !_occupants.TryGetValue(slot, out var b) || b == null) continue;
                if (b.IsEmpty)
                {
                    b.SetJuice(color, volume);
                    PopIn(b.transform);
                    return b;
                }
            }

            // 2) Tạo bình mới ở ô trống đầu tiên.
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                if (!_occupants.TryGetValue(slot, out var b) || b == null)
                {
                    if (bottlePrefab == null) return null;
                    JuiceBottle nb = Instantiate(bottlePrefab, slot.position, Quaternion.identity, slot);
                    nb.homeSlot = slot;
                    nb.SetJuice(color, volume);
                    _occupants[slot] = nb;
                    PopIn(nb.transform);
                    return nb;
                }
            }

            // 3) Hết chỗ.
            return null;
        }

        /// <summary>Còn chỗ để rót bình mới không (ô trống hoặc bình rỗng)?</summary>
        public bool HasRoom()
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                if (!_occupants.TryGetValue(slot, out var b) || b == null) return true;
                if (b.IsEmpty) return true;
            }
            return false;
        }

        static void PopIn(Transform t)
        {
            t.DOKill(true);
            t.localScale = Vector3.zero;
            t.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }
}
