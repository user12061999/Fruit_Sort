using HAVIGAME.UI;
using UnityEngine;

public class HomeSceneController : SceneController {
    protected override void OnSceneStart() {
        UIManager.Instance.Push<HomeTabController>();
    }
}
