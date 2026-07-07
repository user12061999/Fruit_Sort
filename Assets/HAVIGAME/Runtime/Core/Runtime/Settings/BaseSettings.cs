using UnityEngine;

namespace HAVIGAME {

    [SettingMenu(typeof(BaseSettings), "Generic/Base", "", null, 1, "Icons/icon_settings.psd")]
    [CreateAssetMenu(menuName = "HAVIGAME/Settings/Base", fileName = "BaseSettings")]
    public class BaseSettings : Settings<BaseSettings> {

        [Header("[Application]")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool runInBackground = false;
        [SerializeField, Range(-1, 60)] private int targetFrameRate = 60;

        [Header("[Input]")]
        [SerializeField] private bool multiTouchEnabled = true;

        [Header("[Log]")]
        [SerializeField] private LogLevel logLevel = LogLevel.Debug;

        [Header("[Launcher]")]
        [SerializeReference, Subclass] private Launcher launcher;

        public bool AutoInitialize => autoInitialize;
        public bool RunInBackground => runInBackground;
        public int TargetFrameRate => targetFrameRate;
        public bool MultiTouchEnabled => multiTouchEnabled;
        public LogLevel LogLevel => logLevel;
        public Launcher Launcher => launcher;
    }
}
