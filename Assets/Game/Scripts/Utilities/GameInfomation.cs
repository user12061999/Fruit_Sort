using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameInfomation : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI txtInfomation;

    private void Start() {
        string infomation = $"{Application.productName} v{Application.version}";
        txtInfomation.text = infomation;
    }
}