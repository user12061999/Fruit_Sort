using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int targetAmount = 30;
    [SerializeField] private float levelDuration = 60f;
    [SerializeField] private UIManager uiManager;

    private float timeLeft;
    private int collected;
    private int correctStreak;
    private bool speedBoosted;
    private bool levelEnded;

    public int Collected => collected;
    public int TargetAmount => targetAmount;
    public float TimeLeft => timeLeft;
    public float Progress01 => targetAmount <= 0 ? 1f : Mathf.Clamp01((float)collected / targetAmount);
    public int ComboMultiplier => correctStreak >= 3 ? 2 : 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        timeLeft = levelDuration;
        uiManager?.Refresh(this, false, false);
    }

    private void Update()
    {
        if (levelEnded)
            return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndLevel(false);
            return;
        }

        uiManager?.Refresh(this, false, false);
    }

    public void RegisterCorrectSeed(SeedColor color)
    {
        if (levelEnded)
            return;

        correctStreak++;
        collected += ComboMultiplier;
        collected = Mathf.Min(collected, targetAmount);

        if (!speedBoosted && Progress01 >= 0.5f)
        {
            speedBoosted = true;
            ConveyorManager.Instance.SetSpeed(1.5f);
        }

        if (collected >= targetAmount)
            EndLevel(true);
    }

    public void RegisterMissedSeed(Seed seed)
    {
        if (levelEnded || seed == null)
            return;

        if (correctStreak > 0)
            correctStreak = 0;
    }

    private void EndLevel(bool win)
    {
        if (levelEnded)
            return;

        levelEnded = true;
        uiManager?.Refresh(this, win, !win);
    }
}
