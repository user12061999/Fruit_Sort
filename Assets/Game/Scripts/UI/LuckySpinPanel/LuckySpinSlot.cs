using UnityEngine;

public abstract class LuckySpinSlot : MonoBehaviour {
    [SerializeField] private float chance;

    public virtual float Chance => chance;

    public virtual void Show() { }

    public abstract void Collect();
}
