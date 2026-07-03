using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Vẽ đường mờ từ dot đang bị hút (DotState.Attracting) tới miệng bucket, để người chơi
    /// hiểu vì sao dot "tự bay". Màu đường = màu dot, mờ dần về phía bucket.
    ///
    /// KHÔNG cần setup: tự spawn khi vào Play nếu scene có FallingPixelManager.
    /// Muốn chỉnh (width/alpha/maxLines) hoặc tắt hẳn: tự đặt 1 GameObject với component này
    /// vào scene (đã có sẵn thì không auto-spawn nữa) và chỉnh field / disable nó.
    /// </summary>
    public class DotAttractionLines : MonoBehaviour
    {
        [Tooltip("Để trống = FallingPixelManager.Instance.")]
        public FallingPixelManager manager;
        [Tooltip("Số đường vẽ tối đa cùng lúc.")]
        [Min(1)] public int maxLines = 24;
        [Tooltip("Bề rộng đường ở đầu dot (thon dần về bucket).")]
        [Min(0.005f)] public float width = 0.05f;
        [Tooltip("Độ mờ ở đầu dot (0 = tàng hình).")]
        [Range(0f, 1f)] public float alpha = 0.35f;
        [Tooltip("Sorting order của đường (nên thấp hơn dot).")]
        public int sortingOrder = 1;

        readonly List<LineRenderer> _pool = new List<LineRenderer>();
        Material _mat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FallingPixelManager.Instance == null) return;
            if (FindObjectsByType<DotAttractionLines>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0) return;
            new GameObject("_DotAttractionLines").AddComponent<DotAttractionLines>();
        }

        void Awake()
        {
            _mat = new Material(Shader.Find("Sprites/Default"));
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        LineRenderer GetLine(int i)
        {
            while (_pool.Count <= i)
            {
                var go = new GameObject("AttractLine_" + _pool.Count);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.material = _mat;
                lr.positionCount = 2;
                lr.startWidth = width;
                lr.endWidth = width * 0.35f;
                lr.sortingOrder = sortingOrder;
                lr.useWorldSpace = true;
                _pool.Add(lr);
            }
            return _pool[i];
        }

        void LateUpdate()
        {
            FallingPixelManager fm = manager != null ? manager : FallingPixelManager.Instance;
            int used = 0;

            if (fm != null)
            {
                IReadOnlyList<Dot> dots = fm.Dots;
                for (int i = 0; i < dots.Count && used < maxLines; i++)
                {
                    Dot d = dots[i];
                    if (d == null || d.state != DotState.Attracting || d.targetBucket == null) continue;

                    LineRenderer lr = GetLine(used++);
                    lr.enabled = true;
                    lr.SetPosition(0, d.transform.position);
                    lr.SetPosition(1, d.targetBucket.MouthPosition);
                    Color c = d.color;
                    lr.startColor = new Color(c.r, c.g, c.b, alpha);
                    lr.endColor = new Color(c.r, c.g, c.b, 0f); // mờ dần về phía bucket
                }
            }

            // Tắt các đường thừa trong pool.
            for (int i = used; i < _pool.Count; i++)
                if (_pool[i] != null && _pool[i].enabled) _pool[i].enabled = false;
        }
    }
}
