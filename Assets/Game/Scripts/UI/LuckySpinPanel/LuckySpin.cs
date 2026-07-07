using UnityEngine;
using DG.Tweening;
using System;
using HAVIGAME;

public class LuckySpin : MonoBehaviour {
    [SerializeField] private Transform wheel;
    [Tooltip("Require the x-axis of the arrow to point in the direction of the reward.")]
    [SerializeField] private Transform arrow;
    [SerializeField] private LuckySpinSlot[] slots;
    [SerializeField] private bool focusAtCenter;
    [SerializeField] private bool autoRotation;

    private Tween tweenRotate;
    private Tween tweenAutoRotate;

    public bool IsRotating => tweenRotate != null;
    public int SlotCount => slots.Length;

    public void Refresh() {
        foreach (var item in slots) {
            item.Show();
        }
    }

    public bool Rotate(Action<LuckySpinSlot> onCompleted) {
        if (IsRotating) return false;

        tweenAutoRotate?.Kill();

        LuckySpinSlot slot = GetSlotRandom();
        float wheelAngle = GetWheelAngle();
        float arrowAngle = GetArrowAngle();
        float slotAngle = GetSlotAngle(slot);

        float finishAngle = wheelAngle + (arrowAngle - slotAngle);

        if (!focusAtCenter) {
            float anglePerSlot = 360f / SlotCount;
            float randomRange = anglePerSlot * 0.45f;
            finishAngle += UnityEngine.Random.Range(-randomRange, randomRange);
        }

        finishAngle -= 360 * 15;

        tweenRotate = wheel.DORotate(new Vector3(0, 0, finishAngle), 10.6f, RotateMode.FastBeyond360)
            .SetRecyclable(true)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() => {
                Log.Debug($"[LuckySpin] Completed: {slot.name}", slot.gameObject);
                tweenRotate = null;
                onCompleted?.Invoke(slot);
            });

        return true;
    }

    public void AutoRotate() {
        if (IsRotating) return;

        tweenAutoRotate?.Kill();
        tweenAutoRotate = wheel.DORotate(new Vector3(0, 0, -360f), 5, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetSpeedBased(true)
            .SetRelative(true);
    }

    private LuckySpinSlot GetSlotRandom() {
        float totalChance = 0f;
        foreach (var item in slots) {
            totalChance += item.Chance;
        }

        float random = UnityEngine.Random.Range(0, totalChance);

        foreach (var item in slots) {
            if (random <= item.Chance) return item;
            else random -= item.Chance;
        }

        return null;
    }
    public float GetWheelAngle() {
        return wheel.rotation.eulerAngles.z;
    }

    public float GetArrowAngle() {
        return arrow.rotation.eulerAngles.z;
    }

    private float GetSlotAngle(LuckySpinSlot slot) {
        return slot.transform.rotation.eulerAngles.z;
    }

    private Vector2 GetSlotAngleRange(LuckySpinSlot slot) {
        float anglePerSlot = 360f / SlotCount;
        float slotAngle = GetSlotAngle(slot);

        return new Vector2(slotAngle - anglePerSlot / 2f, slotAngle + anglePerSlot / 2f);
    }

    private LuckySpinSlot GetSpinSlot(float angle) {
        float angleClamp = Mathf.Repeat(angle, 360f);

        foreach (var item in slots) {
            Vector2 range = GetSlotAngleRange(item);

            if (angleClamp >= range.x || angleClamp <= range.y) return item;
        }

        return null;
    }
}
