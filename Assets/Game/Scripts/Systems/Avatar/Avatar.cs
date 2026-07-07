using HAVIGAME;
using UnityEngine;

[CreateAssetMenu(fileName = "New Avatar", menuName = "Game/Data/Avatar")]
public class Avatar : ScriptableObject, IIdentify<string> {
    [SerializeField] private string id;
    [SerializeField, ResourcePath(typeof(Sprite))] private string icon;

    private Sprite iconSprite;

    public string Id => id;
    public Sprite Icon {
        get {
            if (iconSprite == null) {
                iconSprite = Resources.Load<Sprite>(icon);
            }

            return iconSprite;
        }
    }

    public void Reset() {
        iconSprite = null;
    }
}
