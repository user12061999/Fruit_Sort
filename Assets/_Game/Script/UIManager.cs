using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Slider progressSlider;

    public void Refresh(GameManager game, bool win, bool lose)
    {
        if (game == null)
            return;

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(game.TimeLeft).ToString();

        if (progressText != null)
            progressText.text = game.Collected + "/" + game.TargetAmount;

        if (comboText != null)
            comboText.text = "x" + game.ComboMultiplier;

        if (progressSlider != null)
            progressSlider.value = game.Progress01;

        if (resultText != null)
        {
            if (win)
                resultText.text = "WIN";
            else if (lose)
                resultText.text = "LOSE";
            else
                resultText.text = string.Empty;
        }
    }
}
