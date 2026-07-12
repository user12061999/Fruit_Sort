#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FruitSort.EditorTools
{
    /// <summary>
    /// Setup 1 lần UI/asset cho booster NAM CHÂM (hút dot đúng màu về giỏ sắp đầy nhất):
    ///  - GamePanel.prefab: tạo nút magnet (clone style từ btnFreezeTime, fallback btnPause)
    ///    + text số lượng + gán field btnMagnet/txtMagnetCount
    ///  - ItemData "Magnet" (id = ItemID.MagnetBooster) + add vào ItemDatabase
    /// Chạy: menu Tools > FruitSort > Setup Magnet Booster UI.
    /// IDEMPOTENT: chạy lại không tạo trùng (kiểm tra field đã gán / asset đã tồn tại).
    /// Vị trí/màu chỉ là placeholder hợp lý — designer chỉnh lại trong prefab tuỳ ý.
    /// </summary>
    public static class SetupMagnetBoosterUI
    {
        const string GamePanelPath = "Assets/Game/Resources/UI/GamePanel.prefab";
        const string ItemDataPath = "Assets/Game/Data/Items/Magnet.asset";
        const string ItemDatabasePath = "Assets/Game/Resources/ItemDatabase.asset";

        [MenuItem("Tools/FruitSort/Setup Magnet Booster UI")]
        public static void Run()
        {
            string report =
                "- " + SetupGamePanelMagnetButton() + "\n" +
                "- " + SetupMagnetItemData();

            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupMagnetBoosterUI]\n{report}");
            EditorUtility.DisplayDialog("Setup Magnet Booster UI", report, "OK");
        }

        // ================= GAME PANEL: NÚT MAGNET =================

        static string SetupGamePanelMagnetButton()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GamePanelPath);
            try
            {
                GamePanel panel = root.GetComponent<GamePanel>();
                if (panel == null) return "GamePanel: KHÔNG tìm thấy component GamePanel trên root!";

                SerializedObject so = new SerializedObject(panel);
                SerializedProperty btnProp = so.FindProperty("btnMagnet");
                SerializedProperty txtProp = so.FindProperty("txtMagnetCount");
                if (btnProp == null || txtProp == null)
                    return "GamePanel: thiếu field btnMagnet/txtMagnetCount — đợi Unity compile code mới rồi chạy lại.";
                if (btnProp.objectReferenceValue != null)
                    return "GamePanel: btnMagnet đã gán sẵn — bỏ qua.";

                // Template: ưu tiên nút freeze (đã có badge số lượng), fallback nút pause.
                Button template = so.FindProperty("btnFreezeTime").objectReferenceValue as Button;
                bool fromFreeze = template != null;
                if (template == null)
                    template = so.FindProperty("btnPause").objectReferenceValue as Button;
                if (template == null)
                    return "GamePanel: btnFreezeTime lẫn btnPause đều chưa gán — không có template để clone.";

                GameObject magnetGo = Object.Instantiate(template.gameObject, template.transform.parent);
                magnetGo.name = "btnMagnet";

                RectTransform templateRect = template.GetComponent<RectTransform>();
                RectTransform magnetRect = magnetGo.GetComponent<RectTransform>();
                float width = Mathf.Max(80f, templateRect.sizeDelta.x);
                magnetRect.anchoredPosition = templateRect.anchoredPosition - new Vector2(width * 1.3f, 0f);

                // Tint cam đỏ để phân biệt (placeholder tới khi có icon nam châm riêng).
                Image magnetImage = magnetGo.GetComponent<Image>();
                if (magnetImage != null) magnetImage.color = new Color(1f, 0.55f, 0.35f);

                // Badge số lượng: dùng lại text con nếu clone từ nút freeze, không thì tạo mới.
                TextMeshProUGUI countText = fromFreeze
                    ? magnetGo.GetComponentInChildren<TextMeshProUGUI>(true)
                    : null;
                if (countText == null)
                {
                    TextMeshProUGUI fontTemplate = root.GetComponentInChildren<TextMeshProUGUI>(true);
                    var go = new GameObject("txtMagnetCount", typeof(RectTransform));
                    go.transform.SetParent(magnetGo.transform, false);
                    go.layer = magnetGo.layer;
                    RectTransform countRect = go.GetComponent<RectTransform>();
                    countRect.anchorMin = countRect.anchorMax = new Vector2(1f, 0f);
                    countRect.pivot = new Vector2(1f, 0f);
                    countRect.anchoredPosition = new Vector2(12f, -12f);
                    countRect.sizeDelta = new Vector2(90f, 56f);

                    countText = go.AddComponent<TextMeshProUGUI>();
                    if (fontTemplate != null) countText.font = fontTemplate.font;
                    countText.fontSize = 42f;
                    countText.fontStyle = FontStyles.Bold;
                    countText.alignment = TextAlignmentOptions.Center;
                    countText.raycastTarget = false;
                }
                countText.name = "txtMagnetCount";
                countText.text = "0";

                btnProp.objectReferenceValue = magnetGo.GetComponent<Button>();
                txtProp.objectReferenceValue = countText;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, GamePanelPath);
                return "GamePanel: đã tạo 'btnMagnet' (clone style " +
                       (fromFreeze ? "btnFreezeTime" : "btnPause") +
                       ", tint cam) + badge số lượng + gán field. Thay icon khi có art nam châm.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ================= ITEM DATA + DATABASE =================

        static string SetupMagnetItemData()
        {
            bool created = false;
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                SerializedObject so = new SerializedObject(item);
                so.FindProperty("id").intValue = ItemID.MagnetBooster;
                so.FindProperty("displayName").stringValue = "Magnet";
                so.FindProperty("description").stringValue = "Hút dot đúng màu trên băng về giỏ sắp đầy nhất.";
                SerializedProperty price = so.FindProperty("price");
                if (price != null)
                {
                    price.FindPropertyRelative("id").intValue = ItemID.Coin;
                    price.FindPropertyRelative("amount").intValue = 150;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(item, ItemDataPath);
                created = true;
            }

            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            if (database == null)
                return "ItemData: asset OK nhưng KHÔNG tìm thấy ItemDatabase.asset để add!";

            SerializedObject dbSo = new SerializedObject(database);
            SerializedProperty list = dbSo.FindProperty("database");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == item)
                    return created
                        ? "ItemData: đã tạo Magnet.asset (giá 150 coin, chưa có icon) — đã nằm sẵn trong ItemDatabase."
                        : "ItemData: Magnet.asset đã tồn tại và đã nằm trong ItemDatabase — bỏ qua.";

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            dbSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            return created
                ? "ItemData: đã tạo Magnet.asset (giá 150 coin, CHƯA có icon — tự gán sprite) + add vào ItemDatabase."
                : "ItemData: Magnet.asset có sẵn, đã add vào ItemDatabase.";
        }
    }
}
#endif
