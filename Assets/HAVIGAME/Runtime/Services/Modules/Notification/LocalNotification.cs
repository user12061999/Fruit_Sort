using UnityEngine;

#if NOTIFICATION
using Unity.Notifications;
#endif

namespace HAVIGAME.Services.Notifications {
    public class LocalNotification {
#if NOTIFICATION
        private Notification internalNotification;
        public Notification InternalNotification => internalNotification;
#endif

        public int? Id {
            get {
#if NOTIFICATION
                return internalNotification.Identifier;
#else
                return null;
#endif
            }
            set {
#if NOTIFICATION
                internalNotification.Identifier = value;
#endif
            }
        }

        public string Title {
            get {
#if NOTIFICATION
                return internalNotification.Title;
#else
                return null;
#endif
            }
            set {
#if NOTIFICATION
                internalNotification.Title = value;
#endif
            }
        }

        public string Body {
            get {
#if NOTIFICATION
                return internalNotification.Text;
#else
                return null;
#endif
            }
            set {
#if NOTIFICATION
                internalNotification.Text = value;
#endif
            }
        }

        public string Data {
            get {
#if NOTIFICATION
                return internalNotification.Data;
#else
                return null;
#endif
            }
            set {
#if NOTIFICATION
                internalNotification.Data = value;
#endif
            }
        }

        public int BadgeNumber {
            get {
#if NOTIFICATION
                return internalNotification.Badge;
#else
                return 0;
#endif
            }
            set {
#if NOTIFICATION
                internalNotification.Badge = value;
#endif
            }
        }

        public string SmallIcon { get; set; }
        public string LargeIcon { get; set; }
        public Color? Color { get; set; }

        public LocalNotification() {
#if NOTIFICATION
            internalNotification = new Notification { ShowInForeground = true };
#endif
        }
    }
}
