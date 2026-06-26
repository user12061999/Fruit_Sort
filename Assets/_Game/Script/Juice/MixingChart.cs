using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Bảng pha màu: tự sinh công thức từ các màu gốc, dùng đúng phép trộn của JuiceColorUtility
    /// (trung bình theo thể tích) nên luôn khớp gameplay. Hiển thị:
    /// hàng nguyên liệu gốc, công thức 2 màu (1:1), công thức theo TỈ LỆ 2:1, và trộn cả 3 màu.
    /// Bố cục nhiều cột cho gọn. Vẽ bằng swatch (SpriteRenderer) world-space.
    /// </summary>
    public class MixingChart : MonoBehaviour
    {
        [Header("Sprites")]
        public Sprite swatchSprite;
        public Sprite plusSprite;
        public Sprite arrowSprite;

        [Header("Màu gốc")]
        [Tooltip("Để trống = lấy từ JuiceGameManager.baseColors trong scene.")]
        public Color[] baseColors;

        [Header("Nội dung bảng")]
        [Tooltip("Hàng các màu gốc (nguyên liệu) ở trên cùng.")]
        public bool showBaseLegend = true;
        [Tooltip("Công thức trộn 2 màu tỉ lệ 1:1 (mọi cặp).")]
        public bool showPairs = true;
        [Tooltip("Công thức theo TỈ LỆ 2:1 (vd 2 đỏ + 1 lục = cam).")]
        public bool showRatios = true;
        [Tooltip("Công thức trộn cả 3 màu gốc đầu tiên.")]
        public bool showTripleMix = true;

        [Header("Bố cục")]
        [Tooltip("Số cột xếp công thức (legend luôn nằm full ở trên).")]
        [Min(1)] public int columns = 2;
        public float swatchSize = 0.36f;
        public float iconSize = 0.22f;
        [Tooltip("Khoảng cách giữa các phần tử trong 1 hàng.")]
        public float itemGap = 0.08f;
        [Tooltip("Khoảng cách giữa các hàng (dọc).")]
        public float rowGap = 0.2f;
        [Tooltip("Khoảng cách giữa các cột (ngang).")]
        public float colGap = 0.5f;
        public int sortingOrder = 25;

        [Header("Khung nền (tuỳ chọn)")]
        public bool drawBackground = true;
        public Color backgroundColor = new Color(0f, 0f, 0f, 0.4f);
        public Vector2 backgroundPadding = new Vector2(0.45f, 0.45f);

        class Row
        {
            public List<Color> inputs = new List<Color>();
            public bool hasResult;
            public Color result;
            public bool plusBetween;
        }

        void Start() => Build();

        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (Application.isPlaying) Destroy(c.gameObject); else DestroyImmediate(c.gameObject);
            }

            Color[] cols = (baseColors != null && baseColors.Length > 0) ? baseColors : GetColorsFromManager();
            if (cols == null || cols.Length < 2 || swatchSprite == null) return;

            // ---- Sinh công thức ----
            var recipes = new List<Row>();

            if (showPairs)
                for (int i = 0; i < cols.Length; i++)
                    for (int j = i + 1; j < cols.Length; j++)
                        recipes.Add(MakeRecipe(cols, new[] { i, j }));

            if (showRatios)
                for (int i = 0; i < cols.Length; i++)
                    for (int j = 0; j < cols.Length; j++)
                        if (i != j) recipes.Add(MakeRecipe(cols, new[] { i, i, j })); // 2 phần i + 1 phần j

            if (showTripleMix && cols.Length >= 3)
                recipes.Add(MakeRecipe(cols, new[] { 0, 1, 2 }));

            if (recipes.Count == 0 && !showBaseLegend) return;

            // ---- Bố cục ----
            float thickness = Mathf.Max(swatchSize, iconSize);
            int nCols = Mathf.Max(1, columns);
            int rowsPerCol = Mathf.CeilToInt(recipes.Count / (float)nCols);
            if (rowsPerCol < 1) rowsPerCol = 0;

            float maxRecipeW = 0f;
            for (int k = 0; k < recipes.Count; k++)
                maxRecipeW = Mathf.Max(maxRecipeW, MeasureRow(recipes[k]));

            float gridH = rowsPerCol > 0 ? rowsPerCol * thickness + (rowsPerCol - 1) * rowGap : 0f;
            float legendBlock = showBaseLegend ? thickness + rowGap : 0f;
            float totalH = legendBlock + gridH;
            float topY = totalH * 0.5f;

            // Legend trên cùng.
            float legendW = 0f;
            if (showBaseLegend)
            {
                var legend = new Row { plusBetween = false, hasResult = false };
                legend.inputs.AddRange(cols);
                legendW = BuildRow(legend, new Vector3(0f, topY - thickness * 0.5f, 0f));
            }

            // Lưới công thức (xếp theo cột: cột 0 đầy trước).
            float colSpacing = maxRecipeW + colGap;
            float gridTopY = topY - legendBlock;
            for (int m = 0; m < recipes.Count; m++)
            {
                int col = m / rowsPerCol;
                int rowInCol = m % rowsPerCol;
                float x = (col - (nCols - 1) * 0.5f) * colSpacing;
                float y = gridTopY - thickness * 0.5f - rowInCol * (thickness + rowGap);
                BuildRow(recipes[m], new Vector3(x, y, 0f));
            }

            if (drawBackground)
            {
                float gridW = nCols * maxRecipeW + (nCols - 1) * colGap;
                float bgW = Mathf.Max(gridW, legendW);
                var bg = MakeItem(Vector3.zero, 1f, backgroundColor, swatchSprite);
                bg.transform.localScale = new Vector3(bgW + backgroundPadding.x, totalH + backgroundPadding.y, 1f);
                bg.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder - 1;
            }
        }

        /// <summary>Tạo 1 công thức từ danh sách chỉ số màu gốc (có thể lặp để biểu diễn tỉ lệ).</summary>
        Row MakeRecipe(Color[] cols, int[] idxs)
        {
            var r = new Row { plusBetween = true, hasResult = true };
            Color acc = cols[idxs[0]];
            float vol = 1f;
            r.inputs.Add(cols[idxs[0]]);
            for (int k = 1; k < idxs.Length; k++)
            {
                r.inputs.Add(cols[idxs[k]]);
                acc = JuiceColorUtility.MixRGB(acc, vol, cols[idxs[k]], 1f);
                vol += 1f;
            }
            r.result = JuiceColorUtility.Vivid(acc); // hiển thị tươi như màu trong bình/đơn hàng
            return r;
        }

        float MeasureRow(Row r)
        {
            int items = r.inputs.Count + (r.plusBetween ? r.inputs.Count - 1 : 0) + (r.hasResult ? 2 : 0);
            float total = r.inputs.Count * swatchSize;
            if (r.plusBetween) total += (r.inputs.Count - 1) * iconSize;
            if (r.hasResult) total += iconSize + swatchSize;
            total += (items - 1) * itemGap;
            return total;
        }

        /// <summary>Dựng 1 hàng, trả về bề rộng của hàng.</summary>
        float BuildRow(Row r, Vector3 center)
        {
            var items = new List<(Sprite sprite, float size, Color color)>();
            for (int i = 0; i < r.inputs.Count; i++)
            {
                if (i > 0 && r.plusBetween) items.Add((plusSprite, iconSize, Color.white));
                items.Add((swatchSprite, swatchSize, r.inputs[i]));
            }
            if (r.hasResult)
            {
                items.Add((arrowSprite, iconSize, Color.white));
                items.Add((swatchSprite, swatchSize, r.result));
            }

            float total = 0f;
            for (int i = 0; i < items.Count; i++) total += items[i].size;
            total += (items.Count - 1) * itemGap;

            float x = -total * 0.5f;
            for (int i = 0; i < items.Count; i++)
            {
                x += items[i].size * 0.5f;
                MakeItem(center + new Vector3(x, 0f, 0f), items[i].size, items[i].color, items[i].sprite);
                x += items[i].size * 0.5f + itemGap;
            }
            return total;
        }

        GameObject MakeItem(Vector3 localPos, float worldSize, Color color, Sprite sprite)
        {
            var go = new GameObject("chart_item");
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            float ppuScale = sprite != null ? worldSize / sprite.bounds.size.x : worldSize;
            go.transform.localScale = Vector3.one * ppuScale;
            go.transform.localRotation = Quaternion.identity;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        Color[] GetColorsFromManager()
        {
            var jgm = FindObjectOfType<JuiceGameManager>();
            return jgm != null ? jgm.baseColors : null;
        }
    }
}
