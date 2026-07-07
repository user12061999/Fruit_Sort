using System;

namespace HAVIGAME {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CategoryMenuAttribute : Attribute {
        public string Name { get; }
        public string FullName { get; }
        public int Order { get; }

        public CategoryMenuAttribute(string menuName, int order = 0) {
            string[] categories = menuName.Split('/');
            Name = categories[^1];
            FullName = menuName;
            Order = order;
        }
    }
}