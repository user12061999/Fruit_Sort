using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central game manager: tracks score, level, UI updates.
/// Singleton. Coordinates between PixelGridManager, FallingPixelManager, and Buckets.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int score;
    public int level = 1;
    public bool isGameOver;

    [Header("Scoring")]
    public int pointsPerDotBreak = 10;   // Points for breaking a dot
    public int pointsPerBucketFill = 50; // Bonus for completing a bucket
    public int penaltyPerLostDot = -5;   // Penalty when dot falls off belt

    [Header("UI References (optional)")]
    public Text scoreText;
    public Text levelText;
    public Text dotsRemainingText;
    public Text bucketStatusText;
    public GameObject gameOverPanel;

    [Header("References")]
    public PixelGridManager gridManager;
    public FallingPixelManager fallingManager;

    // Bucket tracking
    private List<Bucket> activeBuckets = new List<Bucket>();
    private Dictionary<int, int> collectedByColor = new Dictionary<int, int>();

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
        // Auto-find references
        if (gridManager == null)
            gridManager = FindObjectOfType<PixelGridManager>();
        if (fallingManager == null)
            fallingManager = FindObjectOfType<FallingPixelManager>();

        // Subscribe to grid events
        if (gridManager != null)
        {
            gridManager.OnDotsChanged += UpdateDotsUI;
        }

        // Find all initial buckets
        activeBuckets = new List<Bucket>(FindObjectsByType<Bucket>(FindObjectsSortMode.None));
        foreach (var bucket in activeBuckets)
        {
            bucket.OnBucketFull += OnBucketFullCallback;
        }

        UpdateUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        if (isGameOver) return;

        // Update UI each frame
        UpdateUI();

        // Check win/lose conditions
        CheckGameEnd();
    }

    // ========== EVENTS ==========

    /// <summary>
    /// Called when a dot is successfully collected by a matching bucket.
    /// </summary>
    public void OnDotCollected(int colorID)
    {
        if (!collectedByColor.ContainsKey(colorID))
            collectedByColor[colorID] = 0;

        collectedByColor[colorID]++;
        score += pointsPerDotBreak;
    }

    /// <summary>
    /// Called when a bucket is fully filled and removed.
    /// </summary>
    public void OnBucketCompleted(Bucket bucket)
    {
        score += pointsPerBucketFill;
        activeBuckets.Remove(bucket);
    }

    /// <summary>
    /// Called when a dot falls off the belt without being collected.
    /// </summary>
    public void OnDotLost(int colorID)
    {
        score += penaltyPerLostDot;
        score = Mathf.Max(0, score);
    }

    /// <summary>
    /// Callback for bucket full event.
    /// </summary>
    private void OnBucketFullCallback(int colorID)
    {
        // Additional effects can be added here
    }

    // ========== UI ==========

    private void UpdateDotsUI(int remaining, int total)
    {
        if (dotsRemainingText != null)
            dotsRemainingText.text = $"Dots: {remaining}/{total}";
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (levelText != null)
            levelText.text = $"Level: {level}";

        if (dotsRemainingText != null && gridManager != null)
        {
            int remaining = gridManager.GetRemainingDots();
            int total = gridManager.GetTotalDots();
            dotsRemainingText.text = $"Grid: {remaining}/{total}";
        }

        // Bucket status
        if (bucketStatusText != null)
        {
            string status = "Buckets: ";
            Bucket[] buckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);
            foreach (var b in buckets)
            {
                status += $"[C{b.colorID}: {b.currentFill}/{b.maxFill}] ";
            }
            bucketStatusText.text = status;
        }
    }

    // ========== GAME FLOW ==========

    private void CheckGameEnd()
    {
        if (isGameOver) return;

        // Win: no dots left in grid AND no dots on belt
        bool gridEmpty = gridManager != null && gridManager.GetRemainingDots() <= 0;
        bool beltEmpty = fallingManager != null && fallingManager.GetDotCount() <= 0;

        if (gridEmpty && beltEmpty)
        {
            // All dots processed — level complete logic can go here
        }
    }

    /// <summary>
    /// Start the next level.
    /// </summary>
    public void NextLevel()
    {
        level++;

        // Clear existing falling dots
        if (fallingManager != null)
        {
            foreach (var dot in fallingManager.GetActiveDots())
            {
                if (dot != null) Destroy(dot.gameObject);
            }
        }

        if (gridManager != null)
        {
            gridManager.ClearAllDots();
            gridManager.SpawnBatch();
        }
    }

    /// <summary>
    /// End the game.
    /// </summary>
    public void GameOver()
    {
        isGameOver = true;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log($"Game Over! Final Score: {score}");
    }

    /// <summary>
    /// Reset the entire game.
    /// </summary>
    public void ResetGame()
    {
        score = 0;
        level = 1;
        isGameOver = false;
        collectedByColor.Clear();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gridManager != null)
        {
            gridManager.ClearAllDots();
            gridManager.SpawnBatch();
        }
    }
}
