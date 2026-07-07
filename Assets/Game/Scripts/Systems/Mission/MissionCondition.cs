using UnityEngine;

public abstract class MissionCondition : ScriptableObject {
    public abstract bool CheckCondition(Mission mission);
}
