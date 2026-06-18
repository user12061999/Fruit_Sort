using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Progress")]
    public int score;
    [Min(1)] public int level = 1;
    public int scorePerDot = 10;
    public int scorePerBucket = 100;

    [Header("References")]
    public PixelGridManager pixelGridManager;
    public FallingPixelManager fallingPixelManager;
    public List<Bucket> buckets = new List<Bucket>();

    [Header("Optional UI")]
    public Text scoreText;
    public Text levelText;
    public Text remainingDotsText;
    public Text bucketFillText;
    [Min(0.05f)] public float uiRefreshInterval = 0.2f;

    private readonly StringBuilder fillBuilder = new StringBuilder(128);
    private float nextUiRefreshTime;

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
        if (pixelGridManager == null)
        {
            pixelGridManager = FindFirstObjectByType<PixelGridManager>();
        }

        if (fallingPixelManager == null)
        {
            fallingPixelManager = FindFirstObjectByType<FallingPixelManager>();
        }

        Bucket[] sceneBuckets = FindObjectsByType<Bucket>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneBuckets.Length; i++)
        {
            RegisterBucket(sceneBuckets[i]);
        }

        RefreshUI();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextUiRefreshTime)
        {
            RefreshUI();
            nextUiRefreshTime = Time.unscaledTime + uiRefreshInterval;
        }
    }

    public void RegisterBucket(Bucket bucket)
    {
        if (bucket != null && !buckets.Contains(bucket))
        {
            buckets.Add(bucket);
        }
    }

    public void UnregisterBucket(Bucket bucket)
    {
        buckets.Remove(bucket);
    }

    public void OnBucketFillChanged(Bucket bucket)
    {
        score += scorePerDot;
        RefreshUI();
    }

    public void OnBucketCompleted(Bucket bucket)
    {
        score += scorePerBucket;
        RefreshUI();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        RefreshUI();
    }

    public int GetRemainingDotCount()
    {
        int gridCount = pixelGridManager != null ? pixelGridManager.RemainingGridDots : 0;
        int activeCount = fallingPixelManager != null ? fallingPixelManager.ActiveDotCount : 0;
        return gridCount + activeCount;
    }

    public void RefreshUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }

        if (levelText != null)
        {
            levelText.text = $"Level: {level}";
        }

        if (remainingDotsText != null)
        {
            remainingDotsText.text = $"Dots: {GetRemainingDotCount()}";
        }

        if (bucketFillText != null)
        {
            fillBuilder.Clear();
            for (int i = buckets.Count - 1; i >= 0; i--)
            {
                Bucket bucket = buckets[i];
                if (bucket == null)
                {
                    buckets.RemoveAt(i);
                    continue;
                }

                fillBuilder.Append("Color ").Append(bucket.colorId).Append(": ")
                    .Append(bucket.currentFill).Append('/').Append(bucket.maxFill);
                if (i > 0)
                {
                    fillBuilder.AppendLine();
                }
            }

            bucketFillText.text = fillBuilder.ToString();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
