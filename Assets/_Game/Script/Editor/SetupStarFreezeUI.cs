#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FruitSort.EditorTools
{
    /// <summary>
    /// Setup 1 lần các phần UI/asset thủ công cho Star Rating + Time Freeze booster:
    ///  - WinPanel.prefab: tạo 3 icon sao + gán vào WinPanel.starIcons
    ///  - GamePanel.prefab: tạo nút freeze (clone style từ btnPause) + text số lượng + gán field
    ///  - ItemData "TimeFreeze" (id = ItemID.TimeFreezeBooster) + add vào ItemDatabase
    /// Chạy: menu Tools > FruitSort > Setup Star + Freeze UI.
    /// IDEMPOTENT: chạy lại không tạo trùng (kiểm tra field đã gán / asset đã tồn tại).
    /// Vị trí/màu chỉ là placeholder hợp lý — designer chỉnh lại trong prefab tuỳ ý.
    /// </summary>
    public static class SetupStarFreezeUI
    {
        const string WinPanelPath = "Assets/Game/Resources/UI/WinPanel.prefab";
        const string GamePanelPath = "Assets/Game/Resources/UI/GamePanel.prefab";
        const string ItemDataPath = "Assets/Game/Data/Items/TimeFreeze.asset";
        const string ItemDatabasePath = "Assets/Game/Resources/ItemDatabase.asset";

        [MenuItem("Tools/FruitSort/Setup Star + Freeze UI")]
        public static void Run()
        {
            string report =
                "- " + SetupWinPanelStars() + "\n" +
                "- " + SetupGamePanelFreezeButton() + "\n" +
                "- " + SetupTimeFreezeItemData();

            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupStarFreezeUI]\n{report}");
            EditorUtility.DisplayDialog("Setup Star + Freeze UI", report, "OK");
        }

        // ================= WIN PANEL: 3 ICON SAO =================

        static string SetupWinPanelStars()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(WinPanelPath);
            try
            {
                WinPanel panel = root.GetComponent<WinPanel>();
                if (panel == null) return "WinPanel: KHÔNG tìm thấy component WinPanel trên root!";

                SerializedObject so = new SerializedObject(panel);
                SerializedProperty starsProp = so.FindProperty("starIcons");
                if (starsProp == null)
                    return "WinPanel: không có field starIcons — đợi Unity compile code mới rồi chạy lại.";
                if (starsProp.arraySize > 0 &&
                    starsProp.GetArrayElementAtIndex(0).objectReferenceValue != null)
                    return "WinPanel: starIcons đã gán sẵn — bỏ qua.";

                // Sprite sao: mượn từ object "star" có sẵn trong prefab (icon trang trí cũ).
                Sprite starSprite = null;
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject.name == "star" && image.sprite != null)
                    {
                        starSprite = image.sprite;
                        break;
                    }
                }

                Transform parent = FindChildByName(root.transform, "Panel");
                if (parent == null) parent = root.transform;

                RectTransform container = CreateRect("Stars_Auto", parent);
                container.anchorMin = container.anchorMax = new Vector2(0.5f, 1f);
                container.pivot = new Vector2(0.5f, 1f);
                container.anchoredPosition = new Vector2(0f, -40f);
                container.sizeDelta = new Vector2(460f, 170f);

                // Bố cục sao cổ điển: 2 sao bên thấp hơn, sao giữa to và cao hơn.
                float[] xs = { -145f, 0f, 145f };
                float[] ys = { -30f, 0f, -30f };
                float[] sizes = { 115f, 150f, 115f };

                var icons = new GameObject[3];
                for (int i = 0; i < 3; i++)
                {
                    RectTransform iconRect = CreateRect($"Star_{i + 1}", container);
                    iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(xs[i], ys[i]);
                    iconRect.sizeDelta = Vector2.one * sizes[i];

                    Image iconImage = iconRect.gameObject.AddComponent<Image>();
                    iconImage.sprite = starSprite;
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;

                    icons[i] = iconRect.gameObject;
                }

                starsProp.arraySize = 3;
                for (int i = 0; i < 3; i++)
                    starsProp.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, WinPanelPath);
                return starSprite != null
                    ? "WinPanel: đã tạo 'Stars_Auto' (3 icon, sprite mượn từ object 'star') + gán starIcons."
                    : "WinPanel: đã tạo 3 icon + gán starIcons, NHƯNG không tìm thấy sprite 'star' — icon đang trắng, tự gán sprite trong prefab.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ================= GAME PANEL: NÚT FREEZE =================

        static string SetupGamePanelFreezeButton()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GamePanelPath);
            try
            {
                GamePanel panel = root.GetComponent<GamePanel>();
                if (panel == null) return "GamePanel: KHÔNG tìm thấy component GamePanel trên root!";

                SerializedObject so = new SerializedObject(panel);
                SerializedProperty btnProp = so.FindProperty("btnFreezeTime");
                SerializedProperty txtProp = so.FindProperty("txtFreezeCount");
                if (btnProp == null || txtProp == null)
                    return "GamePanel: thiếu field btnFreezeTime/txtFreezeCount — đợi Unity compile code mới rồi chạy lại.";
                if (btnProp.objectReferenceValue != null)
                    return "GamePanel: btnFreezeTime đã gán sẵn — bỏ qua.";

                Button pauseButton = so.FindProperty("btnPause").objectReferenceValue as Button;
                if (pauseButton == null)
                    return "GamePanel: btnPause chưa gán — không có template để clone nút freeze.";

                // Clone nút pause để giữ đúng style (sprite khung, transition...).
                GameObject freezeGo = Object.Instantiate(pauseButton.gameObject, pauseButton.transform.parent);
                freezeGo.name = "btnFreezeTime";

                RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
                RectTransform freezeRect = freezeGo.GetComponent<RectTransform>();
                float width = Mathf.Max(80f, pauseRect.sizeDelta.x);
                freezeRect.anchoredPosition = pauseRect.anchoredPosition - new Vector2(width * 1.3f, 0f);

                // Tint xanh băng để phân biệt với nút pause (placeholder tới khi có icon riêng).
                Image freezeImage = freezeGo.GetComponent<Image>();
                if (freezeImage != null) freezeImage.color = new Color(0.55f, 0.85f, 1f);

                // Badge số lượng ở góc dưới-phải nút; font mượn từ TMP có sẵn trong prefab.
                TextMeshProUGUI fontTemplate = root.GetComponentInChildren<TextMeshProUGUI>(true);
                RectTransform countRect = CreateRect("txtFreezeCount", freezeGo.transform);
                countRect.anchorMin = countRect.anchorMax = new Vector2(1f, 0f);
                countRect.pivot = new Vector2(1f, 0f);
                countRect.anchoredPosition = new Vector2(12f, -12f);
                countRect.sizeDelta = new Vector2(90f, 56f);

                TextMeshProUGUI countText = countRect.gameObject.AddComponent<TextMeshProUGUI>();
                if (fontTemplate != null) countText.font = fontTemplate.font;
                countText.text = "0";
                countText.fontSize = 42f;
                countText.fontStyle = FontStyles.Bold;
                countText.alignment = TextAlignmentOptions.Center;
                countText.raycastTarget = false;

                btnProp.objectReferenceValue = freezeGo.GetComponent<Button>();
                txtProp.objectReferenceValue = countText;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, GamePanelPath);
                return "GamePanel: đã tạo 'btnFreezeTime' (clone style btnPause, tint xanh) + badge số lượng + gán field. " +
                       "Thay icon nút bằng icon đóng băng khi có art.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ================= ITEM DATA + DATABASE =================

        static string SetupTimeFreezeItemData()
        {
            bool created = false;
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemDataPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                SerializedObject so = new SerializedObject(item);
                so.FindProperty("id").intValue = ItemID.TimeFreezeBooster;
                so.FindProperty("displayName").stringValue = "Time Freeze";
                so.FindProperty("description").stringValue = "Đóng băng đồng hồ trong vài giây.";
                SerializedProperty price = so.FindProperty("price");
                if (price != null)
                {
                    price.FindPropertyRelative("id").intValue = ItemID.Coin;
                    price.FindPropertyRelative("amount").intValue = 200;
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
                        ? "ItemData: đã tạo TimeFreeze.asset (giá 200 coin, chưa có icon) — đã nằm sẵn trong ItemDatabase."
                        : "ItemData: TimeFreeze.asset đã tồn tại và đã nằm trong ItemDatabase — bỏ qua.";

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            dbSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);

            return created
                ? "ItemData: đã tạo TimeFreeze.asset (giá 200 coin, CHƯA có icon — tự gán sprite) + add vào ItemDatabase."
                : "ItemData: TimeFreeze.asset có sẵn, đã add vào ItemDatabase.";
        }

        // ================= HELPERS =================

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go.GetComponent<RectTransform>();
        }

        static Transform FindChildByName(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
#endif
