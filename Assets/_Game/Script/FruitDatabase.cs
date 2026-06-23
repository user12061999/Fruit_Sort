using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Bộ sưu tập toàn bộ FruitData trong game.
    /// Tạo bằng Create > FruitSort > FruitDatabase.
    /// Tra cứu bằng colorId qua GetById().
    /// </summary>
    [CreateAssetMenu(fileName = "FruitDatabase", menuName = "FruitSort/FruitDatabase")]
    public class FruitDatabase : ScriptableObject
    {
        public FruitData[] fruits = new FruitData[0];

        /// <summary>Tra cứu FruitData theo colorId. Trả về null nếu không tìm thấy.</summary>
        public FruitData GetById(int colorId)
        {
            for (int i = 0; i < fruits.Length; i++)
                if (fruits[i] != null && fruits[i].colorId == colorId)
                    return fruits[i];
            return null;
        }


#if UNITY_EDITOR
        void OnValidate()
        {
            // Cảnh báo nếu có 2 FruitData trùng colorId.
            for (int i = 0; i < fruits.Length; i++)
            {
                if (fruits[i] == null) continue;
                for (int j = i + 1; j < fruits.Length; j++)
                {
                    if (fruits[j] == null) continue;
                    if (fruits[i].colorId == fruits[j].colorId)
                        Debug.LogWarning($"[FruitDatabase] Trùng colorId={fruits[i].colorId}: " +
                                         $"'{fruits[i].name}' và '{fruits[j].name}'", this);
                }
            }
        }
#endif
    }
}
