using System;
using UnityEngine;

namespace HAVIGAME.Services.Notifications {
    [DefineSymbols(NotificationManager.DEFINE_SYMBOL)]
    [SettingMenu(typeof(NotificationSettings), "Services/Notification", "", null, 101, "Icons/icon_notification.psd")]
    [CreateAssetMenu(fileName = "NotificationSettings", menuName = "HAVIGAME/Settings/Services/Notification")]
    public class NotificationSettings : Settings<NotificationSettings> {
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private OperatingMode mode = OperatingMode.Queue;
        [SerializeField] private bool autoBadging = true;
        [SerializeField] private string channelId = "notifications";
        [SerializeField] private string channelName = "Notifications";
        [SerializeField] private string channelDescription = "Game Notifications";
        [SerializeField] private NotificationPresentation presentationOptions = NotificationPresentation.Badge | NotificationPresentation.Sound;
        [SerializeField] private bool saveNotificationData = true;
        [SerializeField, Condition("SaveNotificationData", true)] private string saveId = "notifications";

        public bool AutoInitialize => autoInitialize;
        public OperatingMode Mode => mode;
        public bool AutoBadging => autoBadging;
        public string ChannelId => channelId;
        public string ChannelName => channelName;
        public string ChannelDescription => channelDescription;
        public NotificationPresentation PresentationOptions => presentationOptions;
        public bool SaveNotificationData => saveNotificationData;
        public string SaveId => saveId;

        public enum OperatingMode {
            NoQueue = 0,
            Queue = 1,
        }

        [Flags]
        public enum NotificationPresentation {
            Alert = 1 << 0,
            Badge = 1 << 1,
            Sound = 1 << 2,
            Vibrate = 1 << 3,
        }
    }
}