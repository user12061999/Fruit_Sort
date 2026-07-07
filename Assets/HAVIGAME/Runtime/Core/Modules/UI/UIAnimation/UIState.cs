using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.UI {

    [System.Serializable]
    public abstract class UIState {
        [SerializeField] private bool reverse;

        public bool Reverse => reverse;

        public virtual void Initialize() { }

        public void Show() {
            if (reverse) {
                SetState(false);
            } else {
                SetState(true);
            }
        }
        public void Hide() {
            if (reverse) {
                SetState(true);
            } else {
                SetState(false);
            }
        }

        protected abstract void SetState(bool isActive);
    }


    [CategoryMenu("Activate", -1)]
    [System.Serializable]
    public class UIActivateState : UIState {
        [SerializeField] private GameObject target;

        protected override void SetState(bool isActive) {
            target.SetActive(isActive);
        }
    }


    [CategoryMenu("Enable", -1)]
    [System.Serializable]
    public class UIEnableState : UIState {
        [SerializeField] private MonoBehaviour target;

        protected override void SetState(bool isActive) {
            target.enabled = isActive;
        }
    }


    [CategoryMenu("Transform/Position")]
    [System.Serializable]
    public class UIPositionState : UIState {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 activePosition;
        [SerializeField] private Vector3 deactivePosition;
        [SerializeField] private bool localSpace;

        protected override void SetState(bool isActive) {
            if (localSpace) {
                target.localPosition = isActive ? activePosition : deactivePosition;
            } else {
                target.position = isActive ? activePosition : deactivePosition;
            }
        }
    }


    [CategoryMenu("Transform/Rotation")]
    [System.Serializable]
    public class UIRotationState : UIState {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 activeRotation;
        [SerializeField] private Vector3 deactiveRotation;
        [SerializeField] private bool localSpace;

        protected override void SetState(bool isActive) {
            if (localSpace) {
                target.localRotation = isActive ? Quaternion.Euler(activeRotation) : Quaternion.Euler(deactiveRotation);
            } else {
                target.rotation = isActive ? Quaternion.Euler(activeRotation) : Quaternion.Euler(deactiveRotation);
            }
        }
    }


    [CategoryMenu("Transform/Scale")]
    [System.Serializable]
    public class UIScaleState : UIState {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 activeScale;
        [SerializeField] private Vector3 deactiveScale;

        protected override void SetState(bool isActive) {
            target.localScale = isActive ? activeScale : deactiveScale;
        }
    }


    [CategoryMenu("Rect Transform/Anchor Position")]
    [System.Serializable]
    public class UIAnchorPositionState : UIState {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector3 activeAnchorPosition;
        [SerializeField] private Vector3 deactiveAnchorPosition;

        protected override void SetState(bool isActive) {
            target.anchoredPosition = isActive ? activeAnchorPosition : deactiveAnchorPosition;
        }
    }


    [CategoryMenu("Rect Transform/Anchor Min")]
    [System.Serializable]
    public class UIAnchorMinState : UIState {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 activeAnchorMin;
        [SerializeField] private Vector2 deactiveAnchorMin;

        protected override void SetState(bool isActive) {
            target.anchorMin = isActive ? activeAnchorMin : deactiveAnchorMin;
        }
    }


    [CategoryMenu("Rect Transform/Anchor Max")]
    [System.Serializable]
    public class UIAnchorMaxState : UIState {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 activeAnchorMax;
        [SerializeField] private Vector2 deactiveAnchorMax;

        protected override void SetState(bool isActive) {
            target.anchorMax = isActive ? activeAnchorMax : deactiveAnchorMax;
        }
    }


    [CategoryMenu("Rect Transform/Size Delta")]
    [System.Serializable]
    public class UISizeDeltaState : UIState {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 activeSizeDelta;
        [SerializeField] private Vector2 deactiveSizeDelta;

        protected override void SetState(bool isActive) {
            target.sizeDelta = isActive ? activeSizeDelta : deactiveSizeDelta;
        }
    }


    [CategoryMenu("Graphic/Alpha")]
    [System.Serializable]
    public class UIFadeState : UIState {
        [SerializeField] private Graphic target;
        [SerializeField, Range(0, 1)] private float activeAlpha;
        [SerializeField, Range(0, 1)] private float deactiveAlpha;

        protected override void SetState(bool isActive) {
            Color cache = target.color;
            cache.a = isActive ? activeAlpha : deactiveAlpha;
            target.color = cache;
        }
    }


    [CategoryMenu("Graphic/Color")]
    [System.Serializable]
    public class UIColorState : UIState {
        [SerializeField] private Graphic target;
        [SerializeField] private Color activeColor;
        [SerializeField] private Color deactiveColor;

        protected override void SetState(bool isActive) {
            target.color = isActive ? activeColor : deactiveColor;
        }
    }


    [CategoryMenu("Graphic/Material")]
    [System.Serializable]
    public class UIMaterialState : UIState {
        [SerializeField] private Graphic target;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material deactiveMaterial;

        protected override void SetState(bool isActive) {
            target.material = isActive ? activeMaterial : deactiveMaterial;
        }
    }

    [CategoryMenu("Graphic/Raycast Target")]
    [System.Serializable]
    public class UIRaycastTargetState : UIState {
        [SerializeField] private Graphic target;

        protected override void SetState(bool isActive) {
            target.raycastTarget = isActive;
        }
    }


    [CategoryMenu("Selectable/Interactable")]
    [System.Serializable]
    public class UIInteractableState : UIState {
        [SerializeField] private Selectable target;

        protected override void SetState(bool isActive) {
            target.interactable = isActive;
        }
    }


    [CategoryMenu("Image/Sprite")]
    [System.Serializable]
    public class UISpriteState : UIState {
        [SerializeField] private Image target;
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite deactiveSprite;

        protected override void SetState(bool isActive) {
            target.sprite = isActive ? activeSprite : deactiveSprite;
        }
    }


    [CategoryMenu("TMPro/Text")]
    [System.Serializable]
    public class UITextState : UIState {
        [SerializeField] private TextMeshProUGUI target;
        [SerializeField] private string activeText;
        [SerializeField] private string deactiveText;

        protected override void SetState(bool isActive) {
            target.text = isActive ? activeText : deactiveText;
        }
    }

    [CategoryMenu("TMPro/Font Material")]
    [System.Serializable]
    public class UIMaterialPresetState : UIState {
        [SerializeField] private TextMeshProUGUI target;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material deactiveMaterial;

        protected override void SetState(bool isActive) {
            target.fontSharedMaterial = isActive ? activeMaterial : deactiveMaterial;
        }
    }
}