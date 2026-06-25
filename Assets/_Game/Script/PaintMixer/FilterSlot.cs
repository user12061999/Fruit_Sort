using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Module Bộ Lọc: loại bỏ một kênh màu CMY khỏi mọi PaintStream đi qua.
    /// Người chơi click để chọn kênh lọc (C / M / Y).
    /// </summary>
    public class FilterSlot : ModuleSlot
    {
        public enum FilterChannel { Cyan = 0, Magenta = 1, Yellow = 2 }

        [Header("Filter Settings")]
        [Tooltip("Kênh màu bị lọc bỏ.")]
        public FilterChannel filterChannel = FilterChannel.Cyan;

        static readonly Color[] ChannelColors = {
            new Color(0f, 1f, 1f),     // Cyan
            new Color(1f, 0f, 1f),     // Magenta
            new Color(1f, 1f, 0f)      // Yellow
        };

        protected override void OnSlotClicked()
        {
            if (currentModule == null || currentModule.moduleType != ModuleType.Filter) return;

            // Click để chuyển sang kênh tiếp theo (C → M → Y → C)
            filterChannel = (FilterChannel)(((int)filterChannel + 1) % 3);
            UpdateFilterVisual();

            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);

            Debug.Log($"[FilterSlot] Lọc kênh: {filterChannel}");
        }

        protected override void OnStreamEnter(PaintStream stream)
        {
            if (stream == null) return;
            if (currentModule == null || currentModule.moduleType != ModuleType.Filter) return;

            stream.FilterChannel((int)filterChannel);

            // Flash màu kênh lọc
            _sr.DOKill();
            Color fc = ChannelColors[(int)filterChannel];
            _sr.DOColor(fc, 0.1f).OnComplete(UpdateFilterVisual);

            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.15f, 0.15f, 4, 0.4f);
        }

        void UpdateFilterVisual()
        {
            if (_sr == null) return;
            Color base_c = currentModule != null ? currentModule.slotColor : emptyColor;
            // Tô thêm màu kênh đang lọc
            Color channel_c = ChannelColors[(int)filterChannel];
            _sr.color = Color.Lerp(base_c, channel_c, 0.5f);
        }

        public override void SetModule(ModuleData module)
        {
            base.SetModule(module);
            UpdateFilterVisual();
        }
    }
}
