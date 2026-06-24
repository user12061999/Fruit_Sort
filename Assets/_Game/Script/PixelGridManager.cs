using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Tạo lưới dot (random theo palette, hoặc lấy màu từ 1 texture).
    /// Là "đầu mối" nhận sát thương: khi dot vỡ -> tách khỏi lưới và đẩy sang FallingPixelManager.
    /// </summary>
    public class PixelGridManager : MonoBehaviour
    {
        [Header("Refs")]
        public Dot dotPrefab;
        [Tooltip("Để trống sẽ tự dùng FallingPixelManager.Instance.")]
        public FallingPixelManager fallingManager;
        [Tooltip("Để tra sprite theo colorId. Gán -> dot đổi sprite thành art của màu tương ứng.")]
        public FruitDatabase fruitDatabase;
        [Tooltip("Góc dưới-trái của lưới. Để trống thì dùng vị trí của object này.")]
        public Transform gridOrigin;

        [Header("Kích thước lưới")]
        public int columns = 16;
        public int rows = 10;
        [Tooltip("Khoảng cách tâm-đến-tâm giữa các dot (world).")]
        public float spacing = 0.55f;
        [Tooltip("Scale áp cho mỗi dot (khớp với dotSize của FallingPixelManager).")]
        public float dotScale = 0.5f;

        [Header("Dot")]
        public int dotHP = 3;
        [Tooltip("Bảng màu. Index = colorId. Để khớp với colorId của Bucket.")]
        public Color[] palette = new Color[]
        {
            new Color(0.93f, 0.26f, 0.21f), // 0 đỏ
            new Color(0.30f, 0.69f, 0.31f), // 1 xanh lá
            new Color(0.13f, 0.59f, 0.95f), // 2 xanh dương
            new Color(1.00f, 0.76f, 0.03f), // 3 vàng
        };

        [Header("Tuỳ chọn: lấy màu từ texture")]
        [Tooltip("Nếu gán, màu mỗi ô lấy từ ảnh (cần Read/Write Enabled). Màu được map về palette gần nhất.")]
        public Texture2D sourceTexture;

        [Header("Tự build khi Start")]
        public bool buildOnStart = true;

        readonly List<Dot> _dots = new List<Dot>();

        /// <summary>Số dot còn sống trong lưới.</summary>
        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _dots.Count; i++)
                    if (_dots[i] != null && _dots[i].state == DotState.InGrid) n++;
                return n;
            }
        }

        void Start()
        {
            if (fallingManager == null) fallingManager = FallingPixelManager.Instance;
            if (buildOnStart) BuildGrid();
        }

        /// <summary>Xoá lưới cũ và dựng lưới mới.</summary>
        public void BuildGrid()
        {
            ClearGrid();
            if (dotPrefab == null) { Debug.LogError("[PixelGridManager] Chưa gán dotPrefab."); return; }
            if (palette == null || palette.Length == 0) palette = new[] { Color.white };

            Vector3 origin = gridOrigin != null ? gridOrigin.position : transform.position;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    PickColor(x, y, out int colorId, out Color c);
                    Vector3 pos = origin + new Vector3(x * spacing, y * spacing, 0f);

                    Dot d = Instantiate(dotPrefab, pos, Quaternion.identity, transform);
                    d.transform.localScale = Vector3.one * dotScale;
                    Sprite spr = fruitDatabase != null ? fruitDatabase.GetById(colorId)?.sprite : null;
                    d.Init(colorId, c, dotHP, new Vector2Int(x, y), spr);
                    _dots.Add(d);
                }
            }
        }

        public void ClearGrid()
        {
            for (int i = _dots.Count - 1; i >= 0; i--)
                if (_dots[i] != null) Destroy(_dots[i].gameObject);
            _dots.Clear();
        }

        // Chọn màu cho ô (x,y): từ texture (nếu có) hoặc random palette.
        void PickColor(int x, int y, out int colorId, out Color color)
        {
            if (sourceTexture != null)
            {
                int px = columns > 1 ? Mathf.RoundToInt(x / (float)(columns - 1) * (sourceTexture.width - 1)) : 0;
                int py = rows > 1 ? Mathf.RoundToInt(y / (float)(rows - 1) * (sourceTexture.height - 1)) : 0;
                Color sample = sourceTexture.GetPixel(px, py);
                colorId = NearestPaletteIndex(sample);
            }
            else
            {
                colorId = Random.Range(0, palette.Length);
            }
            color = palette[Mathf.Clamp(colorId, 0, palette.Length - 1)];
        }

        int NearestPaletteIndex(Color c)
        {
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < palette.Length; i++)
            {
                float dr = palette[i].r - c.r, dg = palette[i].g - c.g, db = palette[i].b - c.b;
                float d = dr * dr + dg * dg + db * db;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>Gây sát thương cho 1 dot. Nếu vỡ -> tách khỏi lưới và cho rơi.</summary>
        public void DamageDot(Dot d, int dmg)
        {
            if (d == null || d.state != DotState.InGrid) return;
            if (d.TakeDamage(dmg))
            {
                _dots.Remove(d);
                FallingPixelManager fm = fallingManager != null ? fallingManager : FallingPixelManager.Instance;
                if (fm != null) fm.AddDot(d);
                else Destroy(d.gameObject);
            }
        }

        /// <summary>
        /// Bắn tia từ origin theo dir, trả về dot trong-lưới gần origin nhất nằm trong bán kính hit.
        /// Thuần hình học (không cần Physics2D / collider).
        /// </summary>
        public Dot RaycastDot(Vector2 origin, Vector2 dir, float maxDist, float hitRadius)
        {
            dir = dir.normalized;
            float effR = hitRadius + dotScale * 0.5f;
            Dot best = null; float bestProj = float.MaxValue;

            for (int i = 0; i < _dots.Count; i++)
            {
                Dot d = _dots[i];
                if (d == null || d.state != DotState.InGrid) continue;

                Vector2 to = (Vector2)d.transform.position - origin;
                float proj = Vector2.Dot(to, dir);
                if (proj < 0f || proj > maxDist) continue;

                Vector2 closest = origin + dir * proj;
                float perp = Vector2.Distance(closest, (Vector2)d.transform.position);
                if (perp <= effR && proj < bestProj) { bestProj = proj; best = d; }
            }
            return best;
        }
    }
}
