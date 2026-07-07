using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveCondition : MonoBehaviour {
    [SerializeField] private ConditionType condition;

    private void OnEnable() {
        switch (condition) {
            case ConditionType.Alaway:
                gameObject.SetActive(true);
                break;
            case ConditionType.EditorOnly:
#if UNITY_EDITOR
                gameObject.SetActive(true);
#else
                gameObject.SetActive(false);
#endif
                break;
            case ConditionType.CheatOnly:
#if CHEAT
                gameObject.SetActive(true);
#else
                gameObject.SetActive(false);
#endif
                break;
            case ConditionType.EditorOrCheat:
#if UNITY_EDITOR || CHEAT
                gameObject.SetActive(true);
#else
                gameObject.SetActive(false);
#endif
                break;
            default:
                break;
        }
    }
}

[System.Serializable]
public enum ConditionType {
    Alaway,
    EditorOnly,
    CheatOnly,
    EditorOrCheat,
}
