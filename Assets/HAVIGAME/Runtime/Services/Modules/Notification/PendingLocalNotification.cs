using System;
using UnityEngine;

namespace HAVIGAME.Services.Notifications {
    public class PendingLocalNotification {
        public readonly LocalNotification Notification;
        public readonly bool Repeat;
        public readonly TimeSpan RepeatTime;

        public bool Reschedule;
        public int ScheduleType;
        public DateTime DeliveryTime { get;private set; }
        public bool Scheduled { get; private set; }

        public PendingLocalNotification(LocalNotification notification, DateTime deliveryTime, bool repeat = false, TimeSpan repeatTime = default, bool scheduled = false, int scheduleType = 0) {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
            DeliveryTime = deliveryTime;
            Repeat = repeat;
            RepeatTime = repeatTime;
            Scheduled = scheduled;
            ScheduleType = scheduleType;
        }

        public void Update() {
            if (Repeat) {
                if (ScheduleType == 1) {
                    double jumpCount = ((DateTime.Now - DeliveryTime).TotalSeconds / RepeatTime.TotalSeconds);
                    DeliveryTime = DeliveryTime + Mathf.Max(1, Mathf.CeilToInt((float)jumpCount)) * RepeatTime;
                } else if (ScheduleType == 2) {
                    DeliveryTime = DateTime.Now.Add(RepeatTime);
                }
            }
        }

        public void Schedule() {
            Scheduled = true;
        }

        public void Unschedule() {
            Scheduled = false;
        }
    }
}
