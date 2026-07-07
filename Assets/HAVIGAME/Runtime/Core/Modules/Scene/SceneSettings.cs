using UnityEngine;

namespace HAVIGAME.Scenes {

    [SettingMenu(typeof(SceneSettings), "Generic/Scene", "", null, 2, "Icons/icon_scene.psd")]
    [CreateAssetMenu(menuName = "HAVIGAME/Settings/Scene", fileName = "SceneSettings")]
    public class SceneSettings : Settings<SceneSettings> {

        [SerializeField] private float minLoadingDuration;
        [SerializeField] private LoadingView[] loadingViews;

        public float MinLoadingDuration => minLoadingDuration;
        public LoadingView[] LoadingViews => loadingViews;

    }
}
