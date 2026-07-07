using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HAVIGAME.SaveLoad;

using static HAVIGAME.Services.Notifications.NotificationSettings;
using static HAVIGAME.GameManager;

#if NOTIFICATION
using Unity.Notifications;


#if UNITY_ANDROID
using Unity.Notifications.Android;
#else
using Unity.Notifications.iOS;
#endif

#endif

namespace HAVIGAME.Services.Notifications {
    public static class NotificationManager {

        public const string DEFINE_SYMBOL = "NOTIFICATION";
        public const string PERMISSION_POST_NOTIFICATIONS = "android.permission.POST_NOTIFICATIONS";

        public static readonly InitializeEvent initializeEvent = new InitializeEvent();

        public static event NotificationDelegate onNotificationReceived;
        public static event EmptyDelegate onForeground;
        public static event EmptyDelegate onBackground;

        private static readonly TimeSpan MinimumNotificationTime = new TimeSpan(0, 0, 2);

        private static DataHolder<NotificationSaveData> dataHolder;
        private static OperatingMode mode;
        private static bool autoBadging;
        private static string channelId;
        private static bool inForeground = true;
        private static List<PendingLocalNotification> pendingNotifications;

        public static bool IsInitialized => initializeEvent.IsInitialized;
        public static OperatingMode Mode => mode;
        public static bool AutoBadging => autoBadging;

        public static void Initialize() {
#if NOTIFICATION

            if (initializeEvent.IsRunning) {
                Log.Warning("[NotificationManager] Notification is running with initialize state {0}.", IsInitialized);
                return;
            }

            NotificationSettings settings = NotificationSettings.Instance;

            mode = settings.Mode;
            autoBadging = settings.AutoBadging;
            channelId = settings.ChannelId;
            pendingNotifications = new List<PendingLocalNotification>();

            if (settings.SaveNotificationData) {
                dataHolder = SaveLoadManager.Create<NotificationSaveData>(settings.SaveId);
                dataHolder.Data.Deserialize(pendingNotifications);
            }

            NotificationCenterArgs args = NotificationCenterArgs.Default;
            args.PresentationOptions = (Unity.Notifications.NotificationPresentation)settings.PresentationOptions;
            args.AndroidChannelId = settings.ChannelId;
            args.AndroidChannelName = settings.ChannelName;
            args.AndroidChannelDescription = settings.ChannelDescription;

            NotificationCenter.Initialize(args);

            Database.Unload(settings);

            RequestPermission();
#endif
        }

#if NOTIFICATION

#if UNITY_ANDROID
        private static void RequestPermission() {
            bool requireRequestPermission = !UnityEngine.Android.Permission.HasUserAuthorizedPermission(PERMISSION_POST_NOTIFICATIONS)
                && AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.NotRequested
                && AndroidNotificationCenter.ShouldShowPermissionToPostRationale;

            if (requireRequestPermission) {
                UnityEngine.Android.PermissionCallbacks callback = new UnityEngine.Android.PermissionCallbacks();

                callback.PermissionDenied += PermissionCallback_PermissionDenied;
                callback.PermissionGranted += PermissionCallback_PermissionGranted;
                callback.PermissionDeniedAndDontAskAgain += PermissionCallback_PermissionDeniedAndDontAskAgain;

                UnityEngine.Android.Permission.RequestUserPermission(PERMISSION_POST_NOTIFICATIONS, callback);
            } else {
                OnInitializeCompleted();
            }
        }

        private static void PermissionCallback_PermissionDeniedAndDontAskAgain(string obj) {
            Log.Debug($"[NotificationManager] Notification premission status update DeniedAndDontAskAgain.");
            OnInitializeCompleted();
        }

        private static void PermissionCallback_PermissionGranted(string obj) {
            Log.Debug($"[NotificationManager] Notification premission status update Granted.");
            OnInitializeCompleted();
        }

        private static void PermissionCallback_PermissionDenied(string obj) {
            Log.Debug($"[NotificationManager] Notification premission status update Denied.");
            OnInitializeCompleted();
        }
#else
        private static void RequestPermission() {
            Executor.Instance.Run(IERequestPermission());
        }

        private static System.Collections.IEnumerator IERequestPermission() {
            NotificationsPermissionRequest request = NotificationCenter.RequestPermission();
            yield return request;
            Log.Debug($"[NotificationManager] Notification premission status {request.Status}.");
            
            OnInitializeCompleted();
        }
#endif

        private static void OnInitializeCompleted() {
            GameManager.onBeforeApplicationFocus += OnApplicationFocus;
            NotificationCenter.OnNotificationReceived += OnNotificationReceived;

            Log.Info("[NotificationManager] Initialize completed.");
            initializeEvent.Invoke(true);

            if (TryGetLastRespondedNotification(out string notificationData)) {
                Log.Info($"[NotificationManager] Last responded avaliable with custom data = {notificationData}");
            }

            OnForeground();
        }

        private static void OnApplicationFocus(bool focus) {
            if (!IsInitialized) {
                return;
            }

            inForeground = focus;

#if NOTIFICATION
            if (inForeground) {
                OnForeground();
            } else {
                OnBackground();
            }
#endif
        }

#endif

#if NOTIFICATION
        private static void OnNotificationReceived(Notification notification) {
            onNotificationReceived?.Invoke(notification.Identifier.Value, notification.Title, notification.Text, notification.Data);

            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Notification received, id = {notification.Identifier.Value}, title = {notification.Title}, body = {notification.Text}, data = {notification.Data}");
        }
#endif
        public static LocalNotification CreateNotification() {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Create notification failed! Notification is not initialized.");
                return null;
            }

            LocalNotification notification = new LocalNotification();
            return notification;
        }

        public static bool TryGetLastRespondedNotification(out string notificationData) {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Notification is not initialized.");
                notificationData = null;
                return false;
            }

#if NOTIFICATION
            Notification? notification = NotificationCenter.LastRespondedNotification;
            if (notification.HasValue) {
                notificationData = notification.Value.Data;
                return true;
            } else {
                notificationData = null;
                return false;
            }
#else
            notificationData = null;
            return false;
#endif
        }

        public static PendingLocalNotification ScheduleNotification(LocalNotification notification, DateTime deliveryTime, bool repeat, TimeSpan repeatTime) {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Schedule notification failed! Notification is not initialized.");
                return null;
            }

#if NOTIFICATION
            bool scheduled = false;

            if (!mode.HasFlag(OperatingMode.Queue)) {
                int notificationId = DateTimeScheduleNotification(notification, deliveryTime, repeat ? repeatTime : null);
                scheduled = true;
            }
            if (!notification.Id.HasValue) {
                int id = new Guid().GetHashCode() + DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode();
                notification.Id = id;
            }

            PendingLocalNotification pendingNotification = new PendingLocalNotification(notification, deliveryTime, repeat, repeatTime, scheduled, 1);
            pendingNotifications.Add(pendingNotification);

            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Schedule pending notification, id = {notification.Id.Value},  delivery time = {deliveryTime}, repeat = {repeat}, repeat time = {repeatTime}");

            return pendingNotification;
#else
            return null;
#endif
        }

        public static PendingLocalNotification ScheduleNotification(LocalNotification notification, TimeSpan delayTime, bool repeat, TimeSpan repeatTime) {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Schedule notification failed! Notification is not initialized.");
                return null;
            }

#if NOTIFICATION
            bool scheduled = false;
            DateTime deliveryTime = DateTime.Now.Add(delayTime);

            if (!mode.HasFlag(OperatingMode.Queue)) {
                int notificationId = DateTimeScheduleNotification(notification, deliveryTime, repeat ? repeatTime : null);
                scheduled = true;
            }
            if (!notification.Id.HasValue) {
                int id = new Guid().GetHashCode() + DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode();
                notification.Id = id;
            }

            PendingLocalNotification pendingNotification = new PendingLocalNotification(notification, deliveryTime, repeat, repeatTime, scheduled, 1);
            pendingNotifications.Add(pendingNotification);

            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Schedule pending notification, id = {notification.Id.Value},  delay time = {delayTime}, repeat = {repeat}, repeat time = {repeatTime}");

            return pendingNotification;
#else
            return null;
#endif
        }

        public static PendingLocalNotification ScheduleNotification(LocalNotification notification, DateTime deliveryTime, bool repeat) {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Schedule notification failed! Notification is not initialized.");
                return null;
            }

#if NOTIFICATION
            bool scheduled = false;
            TimeSpan delayTime = deliveryTime - DateTime.Now;

            if (!mode.HasFlag(OperatingMode.Queue)) {
                int notificationId = IntervalScheduleNotification(notification, deliveryTime, repeat);
                notification.Id = notificationId;
                scheduled = true;
            }
            if (!notification.Id.HasValue) {
                int id = new Guid().GetHashCode() + DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode();
                notification.Id = id;
            }

            PendingLocalNotification pendingNotification = new PendingLocalNotification(notification, deliveryTime, repeat, delayTime, scheduled, 2);
            pendingNotifications.Add(pendingNotification);

            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Schedule pending notification, id = {notification.Id.Value}, delivery time = {deliveryTime}, repeat = {repeat}");

            return pendingNotification;
#else
            return null;
#endif
        }

        public static PendingLocalNotification ScheduleNotification(LocalNotification notification, TimeSpan delayTime, bool repeat) {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Schedule notification failed! Notification is not initialized.");
                return null;
            }

#if NOTIFICATION
            bool scheduled = false;
            DateTime deliveryTime = DateTime.Now.Add(delayTime);

            if (!mode.HasFlag(OperatingMode.Queue)) {
                int notificationId = IntervalScheduleNotification(notification, deliveryTime, repeat);
                notification.Id = notificationId;
                scheduled = true;
            }
            if (!notification.Id.HasValue) {
                int id = new Guid().GetHashCode() + DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode();
                notification.Id = id;
            }

            PendingLocalNotification pendingNotification = new PendingLocalNotification(notification, deliveryTime, repeat, delayTime, scheduled, 2);
            pendingNotifications.Add(pendingNotification);

            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Schedule pending notification, id = {notification.Id.Value}, delay time = {delayTime}, repeat = {repeat}");

            return pendingNotification;
#else
            return null;
#endif
        }

        public static void CancelNotification(int notificationId) {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Cancel notification failed! Notification is not initialized.");
                return;
            }

#if NOTIFICATION
            NotificationCenter.CancelScheduledNotification(notificationId);

            int index = pendingNotifications.FindIndex(scheduledNotification => scheduledNotification.Notification.Id == notificationId);

            if (index >= 0) {
                PendingLocalNotification pendingNotificationToRemove = pendingNotifications[index];
                pendingNotifications.RemoveAt(index);

                if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Cancel notification, id = {pendingNotificationToRemove.Notification.Id}, delivery time = {pendingNotificationToRemove.DeliveryTime}, title = {pendingNotificationToRemove.Notification.Title}, body = {pendingNotificationToRemove.Notification.Body}");
            }
#endif
        }

        public static void CancelAllNotifications() {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Cancel all notifications failed! Notification is not initialized.");
                return;
            }

#if NOTIFICATION
            for (int i = pendingNotifications.Count - 1; i >= 0; i--) {
                if (pendingNotifications[i].Notification.Id.HasValue) {
                    NotificationCenter.CancelScheduledNotification(pendingNotifications[i].Notification.Id.Value);

                    if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Cancel notification, id = {pendingNotifications[i].Notification.Id}, delivery time = {pendingNotifications[i].DeliveryTime}, title = {pendingNotifications[i].Notification.Title}, body = {pendingNotifications[i].Notification.Body}");
                }
            }

            NotificationCenter.CancelAllScheduledNotifications();

            pendingNotifications.Clear();

            if (dataHolder != null) {
                dataHolder.Data.Clear();
            }

            Log.Debug("[NotificationManager] Cancel all notificatiosn.");
#endif
        }

        public static void DismissNotification(int notificationId) {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Dismiss notification failed! Notification is not initialized.");
                return;
            }

#if NOTIFICATION
            NotificationCenter.CancelDeliveredNotification(notificationId);
            Log.Debug($"[NotificationManager] Dismiss notification, id = {notificationId}");
#endif
        }

        public static void DismissAllNotifications() {
            if (!IsInitialized) {
                Log.Warning("[NotificationManager] Dismiss all notifications failed! Notification is not initialized.");
                return;
            }

#if NOTIFICATION
            NotificationCenter.CancelAllDeliveredNotifications();
            Log.Debug("[NotificationManager] Dismiss all notificatiosn.");
#endif
        }

        private static int DateTimeScheduleNotification(LocalNotification notification, DateTime deliveryTime, TimeSpan? repeatTime) {
            int notificationId = -1;

#if NOTIFICATION

#if UNITY_ANDROID
            AndroidNotification androidNotification = (AndroidNotification)notification.InternalNotification;
            androidNotification.FireTime = deliveryTime;
            androidNotification.RepeatInterval = repeatTime;
            androidNotification.SmallIcon = notification.SmallIcon;
            androidNotification.LargeIcon = notification.LargeIcon;
            androidNotification.Color = notification.Color;

            notificationId = AndroidNotificationCenter.SendNotification(androidNotification, channelId);
#else
            if (repeatTime.HasValue) {
                notificationId = NotificationCenter.ScheduleNotification(notification.InternalNotification, new NotificationDateTimeSchedule(deliveryTime, NotificationRepeatInterval.Daily));
            } else {
                notificationId = NotificationCenter.ScheduleNotification(notification.InternalNotification, new NotificationDateTimeSchedule(deliveryTime));
            }
#endif
            notification.Id = notificationId;
#endif

            if (Log.DebugEnabled) {
                if (repeatTime.HasValue) {
                    Log.Debug($"[NotificationManager] Schedule system notification, id = {notification.Id.Value},  delivery time = {deliveryTime}, repeat = {repeatTime.HasValue}, repeat time = {repeatTime.Value}");
                } else {
                    Log.Debug($"[NotificationManager] Schedule system notification, id = {notification.Id.Value},  delivery time = {deliveryTime}, repeat = {repeatTime.HasValue}");
                }
            }

            return notificationId;
        }

        private static int IntervalScheduleNotification(LocalNotification notification, DateTime deliveryTime, bool repeat) {
            int notificationId = -1;

            TimeSpan delayTime = deliveryTime - DateTime.Now;

#if NOTIFICATION

#if UNITY_ANDROID
            AndroidNotification androidNotification = (AndroidNotification)notification.InternalNotification;
            androidNotification.FireTime = deliveryTime;
            androidNotification.RepeatInterval = repeat ? delayTime : null;
            androidNotification.SmallIcon = notification.SmallIcon;
            androidNotification.LargeIcon = notification.LargeIcon;
            androidNotification.Color = notification.Color;

            notificationId = AndroidNotificationCenter.SendNotification(androidNotification, channelId);
#else
            notificationId = NotificationCenter.ScheduleNotification(notification.InternalNotification, new NotificationIntervalSchedule(delayTime, repeat));
#endif

            notification.Id = notificationId;

#endif

            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Schedule system notification, id = {notification.Id.Value}, delay time = {delayTime}, repeat = {repeat}");

            return notificationId;
        }

#if NOTIFICATION
        private static void OnForeground() {
            onForeground?.Invoke();

            NotificationCenter.ClearBadge();

            if (mode.HasFlag(OperatingMode.Queue)) {
                for (int i = pendingNotifications.Count - 1; i >= 0; i--) {
                    PendingLocalNotification pendingNotification = pendingNotifications[i];
                    if (!pendingNotification.Scheduled) {
                        continue;
                    }

                    NotificationCenter.CancelScheduledNotification(pendingNotification.Notification.Id.Value);
                    pendingNotification.Unschedule();

                    if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Cancel notification, id = {pendingNotification.Notification.Id}, delivery time = {pendingNotification.DeliveryTime}, title = {pendingNotification.Notification.Title}, body = {pendingNotification.Notification.Body}");
                }
            }
        }

        private static void OnBackground() {
            onBackground?.Invoke();

            if (mode.HasFlag(OperatingMode.Queue)) {
                for (int i = pendingNotifications.Count - 1; i >= 0; i--) {
                    PendingLocalNotification pendingNotification = pendingNotifications[i];
                    if (pendingNotification.Scheduled) {
                        continue;
                    }

                    if (pendingNotification.DeliveryTime - DateTime.Now < MinimumNotificationTime) {
                        if (!pendingNotification.Repeat) {
                            pendingNotification.Update();
                            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Update pending notification expried, id = {pendingNotification.Notification.Id}, delivery time = {pendingNotification.DeliveryTime}, title = {pendingNotification.Notification.Title}, body = {pendingNotification.Notification.Body}");
                        } else {
                            pendingNotifications.RemoveAt(i);
                            if (Log.DebugEnabled) Log.Debug($"[NotificationManager] Remove pending notification expried, id = {pendingNotification.Notification.Id}, delivery time = {pendingNotification.DeliveryTime}, title = {pendingNotification.Notification.Title}, body = {pendingNotification.Notification.Body}");
                        }
                    }
                }

                bool noBadgeNumbersSet = AutoBadging ? false : pendingNotifications.All(notification => notification.Notification.BadgeNumber == 0); 

                if (noBadgeNumbersSet && AutoBadging) {
                    pendingNotifications.Sort((a, b) => a.DeliveryTime.CompareTo(b.DeliveryTime));

                    int badgeNum = 1;
                    foreach (PendingLocalNotification pendingNotification in pendingNotifications) {
                        if (!pendingNotification.Scheduled) {
                            pendingNotification.Notification.BadgeNumber = badgeNum++;
                        }
                    }
                }

                for (int i = pendingNotifications.Count - 1; i >= 0; i--) {
                    PendingLocalNotification pendingNotification = pendingNotifications[i];
                    if (pendingNotification.Scheduled) {
                        continue;
                    }

                    switch (pendingNotification.ScheduleType) {
                        case 1:
                            DateTimeScheduleNotification(pendingNotification.Notification, pendingNotification.DeliveryTime, pendingNotification.RepeatTime);
                            break;
                        case 2:
                            IntervalScheduleNotification(pendingNotification.Notification, pendingNotification.DeliveryTime, pendingNotification.Repeat);
                            break;
                        default:
                            break;
                    }
                    pendingNotification.Schedule();
                }

                if (noBadgeNumbersSet && AutoBadging) {
                    foreach (PendingLocalNotification pendingNotification in pendingNotifications) {
                        pendingNotification.Notification.BadgeNumber = 0;
                    }
                }
            }

            if (dataHolder != null) {
                List<PendingLocalNotification> notificationsToSave = new List<PendingLocalNotification>(pendingNotifications.Count);
                foreach (PendingLocalNotification pendingNotification in pendingNotifications) {
                    if (pendingNotification.Scheduled) {
                        notificationsToSave.Add(pendingNotification);
                    }
                }

                dataHolder.Data.Serialize(notificationsToSave);
            }
        }
#endif

#if NOTIFICATION
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE;
            public override InitializeEvent InitializeEvent => NotificationManager.initializeEvent;

            public override void Initialize() {
                if (NotificationSettings.Instance.AutoInitialize) {
                    NotificationManager.Initialize();
                }
            }
        }
#endif
    }
}
