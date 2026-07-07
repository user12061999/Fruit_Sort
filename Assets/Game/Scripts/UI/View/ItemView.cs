using HAVIGAME.UI;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class ItemView : View<ItemStack> {
    [SerializeField] protected Image imgIcon;
    [SerializeField] protected TextMeshProUGUI txtName;
    [SerializeField] protected TextMeshProUGUI txtAmount;
    [SerializeField] protected string customValueFomat;
    [SerializeField] protected bool animationValue;

    private int previousValue;
    private Tween animTween;

    public override void Show() {
        animTween?.Kill();

        ItemData itemData = ItemDatabase.Instance.GetDataById(Model.Id);
        if(imgIcon) imgIcon.sprite = itemData.Icon;
        if (txtName) txtName.text = itemData.Name;

        if (animationValue) {
            SetAmount(Model.Amount, previousValue);
        }
        else {
            SetAmount(Model.Amount);
        }

        previousValue = Model.Amount;
    }

    private void SetAmount(int amount, int previousAmount) {
        int diff = amount - previousAmount;

        animTween = DOVirtual.Float(0f, 1f, 0.5f,
            (val) => {
                int value = previousAmount + (int)(diff * val);
                SetAmount(value);
            });
    }

    private void SetAmount(int amount) {
        if (string.IsNullOrEmpty(customValueFomat)) {
            if (txtAmount) txtAmount.text = amount.ToString(ConfigDatabase.Instance.DefaultValueFormat);
        }
        else {
            if (txtAmount) txtAmount.text = amount.ToString(customValueFomat);
        }
    }
}
