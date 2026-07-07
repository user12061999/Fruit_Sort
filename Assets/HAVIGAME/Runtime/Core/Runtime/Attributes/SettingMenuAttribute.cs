using System;

namespace HAVIGAME {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SettingMenuAttribute : Attribute {
        public string Name { get; }
        public string Icon { get; }
        public string FullName { get; }
        public string Description { get; }
        public string Document { get; }
        public int Order { get; }
        public Type Type { get; }

        public SettingMenuAttribute(Type type, string menuName, string description = null, string document = null, int order = 0, string icon = null) {
            string[] categories = menuName.Split('/');
            Name = categories[^1];
            Icon = icon;
            FullName = menuName;
            Type = type;
            Description = description;
            Document = document;
            Order = order;
        }
    }
}