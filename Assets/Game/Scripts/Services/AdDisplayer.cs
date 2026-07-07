using HAVIGAME.Services.Advertisings;
using System.Collections.Generic;
using UnityEngine;

public abstract class AdDisplayer : MonoBehaviour {
    protected static Dictionary<string, AdDisplayer> displayers = new Dictionary<string, AdDisplayer>();

    public static bool TryGetDisplayer(string id, out AdDisplayer displayer) {
        return displayers.TryGetValue(id, out displayer);
    }

    public static bool TryGetDisplayer(AdGroup group, out AdDisplayer displayer) {
        foreach (var item in displayers.Values) {
            if (group.HasFlag(item.filter.Group)) {
                displayer = item;
                return true;
            }
        }

        displayer = null;
        return false;
    }

    public static void HideAll() {
        foreach (var item in displayers.Values) {
            item.Hide();
        }
    }

    public static void DestroyAll() {
        foreach (var item in displayers.Values) {
            item.Destroy();
        }
    }

    [SerializeField] protected string id;
    [SerializeField] protected string placement;
    [SerializeField] protected AdFilter filter = new AdFilter(AdNetwork.All, AdGroup.All);

    public string Id => id;

    protected virtual void Awake() {
        displayers.Add(id, this);
    }

    protected virtual void OnDestroy() {
        displayers.Remove(id);
    }

    public abstract void Show();
    public abstract void Hide();
    public abstract void Destroy();
}
