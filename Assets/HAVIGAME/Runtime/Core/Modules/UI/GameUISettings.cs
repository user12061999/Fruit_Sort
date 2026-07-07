using UnityEngine;

namespace HAVIGAME.UI {

    [SettingMenu(typeof(GameUISettings), "Generic/UI", "", null, 2, "Icons/icon_ui.psd")]
    [CreateAssetMenu(menuName = "HAVIGAME/Settings/Game UI", fileName = "GameUISettings")]
    public class GameUISettings : Settings<GameUISettings> {

        [Header("[Initialize]")]
        [SerializeField] private int initializeCapacity = 8;
        [SerializeReference, Subclass] private FrameFactory frameFactory;

        [Header("[Options]")]
        [SerializeField] private bool orderUIEnabled = true;
        [SerializeField] private bool physicalBackButton = true;
        [SerializeField] private bool clearUIOnSceneLoad = true;

        public int InitializeCapacity => initializeCapacity;
        public bool OrderUIEnabled => orderUIEnabled;
        public bool PhysicalBackButton => physicalBackButton;
        public bool ClearUIOnSceneLoad => clearUIOnSceneLoad;
        public FrameFactory FrameFactory => frameFactory;

    }
}
