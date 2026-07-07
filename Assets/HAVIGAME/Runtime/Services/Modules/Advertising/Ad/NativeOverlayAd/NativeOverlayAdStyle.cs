using UnityEngine;

#if ADMOB
using GoogleMobileAds.Api;
#endif

namespace HAVIGAME.Services.Advertisings {
    [System.Serializable]
    public class NativeOverlayAdStyle {
#if ADMOB
        public const string Small = NativeTemplateId.Small;
        public const string Medium = NativeTemplateId.Medium;
#else
        public const string Small = "small";
        public const string Medium = "medium";
#endif

        [SerializeField, ConstantField(typeof(NativeOverlayAdStyle))] private string templateId = "small";
        [SerializeField] private Color mainBackgroundColor = Color.white;
        [SerializeField] private NativeOverlayAdTextStyle primaryText;
        [SerializeField] private NativeOverlayAdTextStyle secondaryText;
        [SerializeField] private NativeOverlayAdTextStyle tertiaryText;
        [SerializeField] private NativeOverlayAdTextStyle callToActionText;

        public string TemplateId => templateId;
        public Color MainBackgroundColor => mainBackgroundColor;
        public NativeOverlayAdTextStyle PrimaryText => primaryText;
        public NativeOverlayAdTextStyle SecondaryText => secondaryText;
        public NativeOverlayAdTextStyle TertiaryText => tertiaryText;
        public NativeOverlayAdTextStyle CallToActionText => callToActionText;

        public bool Equal(NativeOverlayAdStyle other) {
            return templateId == other.templateId
                && mainBackgroundColor == other.mainBackgroundColor
                && primaryText.Equal(other.primaryText)
                && secondaryText.Equal(other.secondaryText)
                && tertiaryText.Equal(other.tertiaryText)
                && callToActionText.Equal(other.callToActionText);
        }

        public NativeOverlayAdStyle Scale(float scale) {
            NativeOverlayAdStyle style = new NativeOverlayAdStyle();
            style.templateId = templateId;
            style.mainBackgroundColor = mainBackgroundColor;
            style.primaryText = primaryText.Scale(scale);
            style.secondaryText = secondaryText.Scale(scale);
            style.tertiaryText = tertiaryText.Scale(scale);
            style.callToActionText = callToActionText.Scale(scale);
            return style;
        }
#if ADMOB
        public NativeTemplateStyle ToAdmobNativeTemplateStyle() {
            NativeTemplateStyle style = new NativeTemplateStyle();

            style.TemplateId = templateId;
            style.MainBackgroundColor = mainBackgroundColor;

            if (primaryText.Enable) style.PrimaryText = primaryText.ToAdmobNativeTemplateTextStyle();
            if (secondaryText.Enable) style.SecondaryText = secondaryText.ToAdmobNativeTemplateTextStyle();
            if (tertiaryText.Enable) style.TertiaryText = tertiaryText.ToAdmobNativeTemplateTextStyle();
            if (callToActionText.Enable) style.CallToActionText = callToActionText.ToAdmobNativeTemplateTextStyle();

            return style;
        }
#endif
    }

    [System.Serializable]
    public class NativeOverlayAdTextStyle {
        [SerializeField] private bool enable = true;
        [SerializeField, Condition("Enable", true)] private Color backgroundColor = Color.white;
        [SerializeField, Condition("Enable", true)] private Color textColor = Color.black;
        [SerializeField, Condition("Enable", true)] private int fontSize = 12;
        [SerializeField, Condition("Enable", true)] private NativeOverlayAdFontStyle fontStyle = NativeOverlayAdFontStyle.Normal;

        public bool Enable => enable;
        public Color BackgroundColor => backgroundColor;
        public Color TextColor => textColor;
        public int FontSize => fontSize;
        public NativeOverlayAdFontStyle FontStyle => fontStyle;

        public bool Equal(NativeOverlayAdTextStyle other) {
            if (enable == other.enable) {
                if (enable) {
                    return backgroundColor == other.backgroundColor
                        && textColor == other.textColor
                        && fontSize == other.fontSize
                        && fontStyle == other.fontStyle;
                } else {
                    return true;
                }
            } else {
                return false;
            }
        }

        public NativeOverlayAdTextStyle Scale(float scale) {
            NativeOverlayAdTextStyle style = new NativeOverlayAdTextStyle();
            style.enable = enable;
            style.backgroundColor = backgroundColor;
            style.textColor = textColor;
            style.fontSize = Mathf.CeilToInt(fontSize * scale);
            style.fontStyle = fontStyle;
            return style;
        }

#if ADMOB
        public NativeTemplateTextStyle ToAdmobNativeTemplateTextStyle() {
            NativeTemplateTextStyle style = new NativeTemplateTextStyle() {
                BackgroundColor = backgroundColor,
                TextColor = textColor,
                FontSize = fontSize,
                Style = ToAdmobNativeTemplateFontStyle(fontStyle),
            };

            return style;
        }

        public NativeTemplateFontStyle ToAdmobNativeTemplateFontStyle(NativeOverlayAdFontStyle style) {
            switch (style) {
                case NativeOverlayAdFontStyle.Normal: return NativeTemplateFontStyle.Normal;
                case NativeOverlayAdFontStyle.Bold: return NativeTemplateFontStyle.Bold;
                case NativeOverlayAdFontStyle.Italic: return NativeTemplateFontStyle.Italic;
                default: return NativeTemplateFontStyle.Monospace;
            }
        }
#endif
    }

    [System.Serializable]
    public enum NativeOverlayAdFontStyle {
        Normal,
        Bold,
        Italic,
        Monospace
    }
}