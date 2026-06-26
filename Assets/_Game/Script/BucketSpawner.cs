using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Sinh và TÁI SINH bucket tại các ô cố định. Mỗi ô gắn một colorId (màu quả).
    /// Khi worker nhấc một bucket đi (Bucket.BePickedUp), bucket gọi RespawnSlot() để
    /// ô đó lập tức có một bucket rỗng MỚI cùng màu -> vòng phân loại không bao giờ gián đoạn.
    /// </summary>
    public class BucketSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            [Tooltip("Vị trí đặt bucket.")]
            public Transform point;
            [Tooltip("colorId (khớp FruitData/Dot). Bucket ở ô này luôn mang màu này.")]
            public int colorId;
        }

        [Header("Refs")]
        [Tooltip("Prefab bucket (Cage).")]
        public Bucket bucketPrefab;
        [Tooltip("Tra màu theo colorId để tô bucket cho khớp quả/nước ép.")]
        public FruitDatabase fruitDatabase;

        [Header("Các ô bucket")]
        public List<Slot> slots = new List<Slot>();

        void Start()
        {
            for (int i = 0; i < slots.Count; i++)
                SpawnAt(i);
        }

        /// <summary>Sinh bucket mới ở ô index.</summary>
        public Bucket SpawnAt(int index)
        {
            if (index < 0 || index >= slots.Count) return null;
            Slot slot = slots[index];
            if (slot == null || slot.point == null || bucketPrefab == null) return null;

            Bucket b = Instantiate(bucketPrefab, slot.point.position, slot.point.rotation);
            b.transform.SetParent(slot.point, true);

            Color c = Color.white;
            FruitData fd = fruitDatabase != null ? fruitDatabase.GetById(slot.colorId) : null;
            if (fd != null) { c = fd.color; c.a = 1f; }

            b.spawner = this;
            b.spawnerSlotIndex = index;
            b.Configure(slot.colorId, c);
            return b;
        }

        /// <summary>Tái sinh bucket ở ô index (gọi khi bucket cũ bị mang đi).</summary>
        public void RespawnSlot(int index) => SpawnAt(index);
    }
}
