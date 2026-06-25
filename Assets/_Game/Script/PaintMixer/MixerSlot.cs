using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Module Máy Trộn: khi có từ 2 PaintStream cùng đi qua vùng slot, gộp chúng thành 1
    /// (pha CMY theo trọng số volume). KHÔNG đóng băng dòng lẻ — nếu chỉ có 1 dòng nó chảy
    /// qua tự do (tránh deadlock trong thiết kế loop-and-catch).
    /// Nếu cả 3 kênh CMY đều > 0.3 sau khi trộn → tạo "Bùn".
    /// </summary>
    public class MixerSlot : ModuleSlot
    {
        [Header("Mixer Settings")]
        [Tooltip("Số stream tối thiểu (cùng lúc trong vùng) để kích hoạt trộn.")]
        [Min(2)] public int minStreamsToMix = 2;

        readonly List<PaintStream> _nearby = new List<PaintStream>(8);

        protected override void OnStreamEnter(PaintStream stream) { }
        protected override void OnStreamExit(PaintStream stream) { }

        protected override void OnUpdate()
        {
            if (currentModule == null || currentModule.moduleType != ModuleType.Mixer) return;

            // Thu thập các stream đang ở trong vùng slot (bỏ qua dòng đang gộp/đóng băng).
            _nearby.Clear();
            var hits = Physics2D.OverlapCircleAll(transform.position, streamDetectRadius);
            foreach (var h in hits)
            {
                var ps = h.GetComponent<PaintStream>();
                if (ps == null || ps.IsMerging || ps.IsFrozen) continue;
                _nearby.Add(ps);
            }

            if (_nearby.Count < minStreamsToMix) return;

            // Gộp tất cả vào dòng có volume lớn nhất (giữ lại nó, hủy phần còn lại).
            PaintStream main = _nearby[0];
            for (int i = 1; i < _nearby.Count; i++)
                if (_nearby[i].volume > main.volume) main = _nearby[i];

            for (int i = 0; i < _nearby.Count; i++)
            {
                if (_nearby[i] == main) continue;
                main.MergeWith(_nearby[i]); // MixCMY + cộng volume + hủy dòng kia
            }

            // Kiểm tra bùn sau khi trộn.
            if (PaintColorUtility.IsMud(main.cmy))
            {
                main.cmy = PaintColorUtility.GetMudCMY();
                main.RefreshVisual();
                Debug.Log("[MixerSlot] Trộn ra bùn! Cần Syringe để xử lý.");
            }

            // Visual flash trên slot.
            _sr.DOKill();
            _sr.DOColor(Color.white, 0.1f).OnComplete(() => _sr.color = currentModule != null ? currentModule.slotColor : emptyColor);
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
        }
    }
}
