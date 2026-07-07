using System;
using UnityEngine;

namespace HAVIGAME
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConditionAttribute : PropertyAttribute
    {
        public string CompareName { get; }
        public object CompareValue { get; }

        public ConditionAttribute(string compareName, object compareValue)
        {
            CompareName = compareName;
            CompareValue = compareValue;
        }
    }
}