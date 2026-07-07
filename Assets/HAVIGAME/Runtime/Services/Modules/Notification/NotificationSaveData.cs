using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using HAVIGAME.SaveLoad;

namespace HAVIGAME.Services.Notifications {
    [System.Serializable]
    public class NotificationSaveData : SaveData {
        [SerializeField] private string data;

        public void Serialize(IList<PendingLocalNotification> pendingNotifications) {
            JArray array = new JArray();
            foreach (var item in pendingNotifications) {
                JObject element = new JObject();

                if (item.Notification.Id.HasValue) element.Add("id", item.Notification.Id.Value);

                element.Add("title", item.Notification.Title ?? string.Empty);
                element.Add("body", item.Notification.Body ?? string.Empty);
                element.Add("data", item.Notification.Data ?? string.Empty);
                element.Add("small_icon", item.Notification.SmallIcon ?? string.Empty);
                element.Add("large_icon", item.Notification.LargeIcon ?? string.Empty);
                if (item.Notification.Color.HasValue) element.Add("color", ColorUtility.ToHtmlStringRGBA(item.Notification.Color.Value));

                element.Add("badge_number", item.Notification.BadgeNumber);
                element.Add("delivery_time", item.DeliveryTime.Ticks);
                element.Add("schedule_type", item.ScheduleType);
                element.Add("repeat", item.Repeat);
                
                if (item.Repeat) element.Add("repeat_time", item.RepeatTime.Ticks);

                element.Add("scheduled", item.Scheduled);

                array.Add(element);
            }

            data = array.ToString();

            Log.Debug($"[NotificationSaveData] Save {pendingNotifications.Count} pending notifications.");

            SetChanged();
        }

        public void Deserialize(IList<PendingLocalNotification> pendingNotifications) {

            if (!string.IsNullOrEmpty(data)) {
                JArray array = JArray.Parse(data);

                foreach (JToken element in array) {
                    int? id = (int?)element["id"];

                    string title = (string)element["title"];
                    string body = (string)element["body"];
                    string data = (string)element["data"];
                    string small_icon = (string)element["small_icon"];
                    string large_icon = (string)element["large_icon"];
                    Color? color = null;
                    if (ColorUtility.TryParseHtmlString((string)element["color"], out Color result)) color = result;

                    int badge_number = (int)element["badge_number"];
                    long delivery_time = (long)element["delivery_time"];
                    int schedule_type = (int)element["schedule_type"];
                    bool repeat = (bool)element["repeat"];

                    long repeat_time = repeat ? (long)element["repeat_time"] : 0;

                    bool scheduled = (bool)element["scheduled"];

                    LocalNotification Notification = new LocalNotification() {
                        Id = id,
                        Title = title,
                        Body = body,
                        Data = data,
                        BadgeNumber = badge_number,
                        SmallIcon = small_icon,
                        LargeIcon = large_icon,
                        Color = color,
                    };

                    PendingLocalNotification pendingNotification = new PendingLocalNotification(Notification, new System.DateTime(delivery_time), repeat, new System.TimeSpan(repeat_time), scheduled, schedule_type);

                    pendingNotifications.Add(pendingNotification);
                }
            }

            Log.Debug($"[NotificationSaveData] Load {pendingNotifications.Count} pending notifications.");
        }

        public IList<PendingLocalNotification> Deserialize() {
            IList<PendingLocalNotification> pendingNotifications = new List<PendingLocalNotification>();

            if (!string.IsNullOrEmpty(data)) {
                JArray array = JArray.Parse(data);

                foreach (JToken element in array) {
                    int? id = (int?)element["id"];

                    string title = (string)element["title"];
                    string body = (string)element["body"];
                    string data = (string)element["data"];
                    string small_icon = (string)element["small_icon"];
                    string large_icon = (string)element["large_icon"];
                    Color? color = null;
                    if (ColorUtility.TryParseHtmlString((string)element["color"], out Color result)) color = result;

                    int badge_number = (int)element["badge_number"];
                    long delivery_time = (long)element["badge_number"];
                    int schedule_type = (int)element["schedule_type"];
                    bool repeat = (bool)element["repeat"];

                    long repeat_time = repeat ? (long)element["repeat_time"] : 0;

                    LocalNotification Notification = new LocalNotification() {
                        Id = id,
                        Title = title,
                        Body = body,
                        Data = data,
                        BadgeNumber = badge_number,
                        SmallIcon = small_icon,
                        LargeIcon = large_icon,
                        Color = color,
                    };

                    PendingLocalNotification pendingNotification = new PendingLocalNotification(Notification, new System.DateTime(delivery_time), repeat, new System.TimeSpan(repeat_time), false, schedule_type);

                    pendingNotifications.Add(pendingNotification);
                }
            }

            Log.Debug($"[NotificationSaveData] Load {pendingNotifications.Count} pending notifications.");

            return pendingNotifications;
        }

        public void Clear() {
            data = string.Empty;
            Log.Debug($"[NotificationSaveData] Clear pending notifications.");
            SetChanged();
        }
    }
}
