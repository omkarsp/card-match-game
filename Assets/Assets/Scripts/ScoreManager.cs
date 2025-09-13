using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private int basePointsPerMatch = 1;
    [SerializeField] private bool enableComboSystem = true;
    [SerializeField] private int comboMultiplierThreshold = 3;
    [SerializeField] private float comboMultiplier = 1.5f;
    [SerializeField] private int maxComboMultiplier = 3;

    // Current score state
    public int TotalScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public int ConsecutiveMatches { get; private set; }
    public float CurrentMultiplier { get; private set; } = 1f;

    // Events
    public Action<int> OnScoreChanged;
    public Action<int> OnComboChanged;
    public Action<float> OnMultiplierChanged;

    private void Awake()
    {
        ResetScore();
    }

    public void ResetScore()
    {
        TotalScore = 0;
        CurrentCombo = 0;
        ConsecutiveMatches = 0;
        CurrentMultiplier = 1f;
        
        NotifyListeners();
    }

    public int AddMatch()
    {
        // Calculate points for this match
        int basePoints = basePointsPerMatch;
        int pointsEarned = basePoints;

        if (enableComboSystem)
        {
            ConsecutiveMatches++;
            
            // Update combo and multiplier
            UpdateCombo();
            
            // Apply multiplier to points
            pointsEarned = Mathf.RoundToInt(basePoints * CurrentMultiplier);
        }

        // Add to total score
        TotalScore += pointsEarned;

        // Notify listeners
        NotifyListeners();

        Debug.Log($"Match scored! Base: {basePoints}, Multiplier: {CurrentMultiplier:F1}x, Earned: {pointsEarned}, Total: {TotalScore}");

        return pointsEarned;
    }

    public void AddMismatch()
    {
        if (enableComboSystem)
        {
            // Reset combo on mismatch
            ResetCombo();
        }

        // Notify listeners (score doesn't change but combo might)
        NotifyListeners();

        Debug.Log($"Mismatch! Combo reset. Current score: {TotalScore}");
    }

    private void UpdateCombo()
    {
        if (!enableComboSystem) return;

        // Update combo count
        if (ConsecutiveMatches >= comboMultiplierThreshold)
        {
            CurrentCombo = ConsecutiveMatches - comboMultiplierThreshold + 1;
            
            // Calculate multiplier based on combo
            CurrentMultiplier = 1f + (CurrentCombo * (comboMultiplier - 1f));
            
            // Cap the multiplier
            CurrentMultiplier = Mathf.Min(CurrentMultiplier, maxComboMultiplier);
        }
        else
        {
            CurrentCombo = 0;
            CurrentMultiplier = 1f;
        }
    }

    private void ResetCombo()
    {
        ConsecutiveMatches = 0;
        CurrentCombo = 0;
        CurrentMultiplier = 1f;
    }

    private void NotifyListeners()
    {
        OnScoreChanged?.Invoke(TotalScore);
        OnComboChanged?.Invoke(CurrentCombo);
        OnMultiplierChanged?.Invoke(CurrentMultiplier);
    }

    // Public getters for UI
    public string GetScoreString()
    {
        return TotalScore.ToString();
    }

    public string GetComboString()
    {
        if (!enableComboSystem || CurrentCombo <= 0)
            return "";
        
        return $"Combo x{CurrentCombo}";
    }

    public string GetMultiplierString()
    {
        if (!enableComboSystem || CurrentMultiplier <= 1f)
            return "";
            
        return $"{CurrentMultiplier:F1}x";
    }

    // Configuration methods
    public void SetBasePointsPerMatch(int points)
    {
        basePointsPerMatch = Mathf.Max(1, points);
    }

    public void SetComboSystemEnabled(bool enabled)
    {
        enableComboSystem = enabled;
        if (!enabled)
        {
            ResetCombo();
            NotifyListeners();
        }
    }

    public void SetComboSettings(int threshold, float multiplier, int maxMultiplier)
    {
        comboMultiplierThreshold = Mathf.Max(2, threshold);
        comboMultiplier = Mathf.Max(1f, multiplier);
        this.maxComboMultiplier = Mathf.Max(1, maxMultiplier);
    }

    // Save/Load support
    [System.Serializable]
    public class ScoreData
    {
        public int totalScore;
        public int currentCombo;
        public int consecutiveMatches;
        public float currentMultiplier;
    }

    public ScoreData GetScoreData()
    {
        return new ScoreData
        {
            totalScore = TotalScore,
            currentCombo = CurrentCombo,
            consecutiveMatches = ConsecutiveMatches,
            currentMultiplier = CurrentMultiplier
        };
    }

    public void LoadScoreData(ScoreData data)
    {
        if (data == null) return;

        TotalScore = data.totalScore;
        CurrentCombo = data.currentCombo;
        ConsecutiveMatches = data.consecutiveMatches;
        CurrentMultiplier = data.currentMultiplier;

        NotifyListeners();
    }

    // Debug methods
    public void LogScoreStatus()
    {
        Debug.Log("=== SCORE STATUS ===");
        Debug.Log($"Total Score: {TotalScore}");
        Debug.Log($"Current Combo: {CurrentCombo}");
        Debug.Log($"Consecutive Matches: {ConsecutiveMatches}");
        Debug.Log($"Current Multiplier: {CurrentMultiplier:F2}x");
        Debug.Log($"Combo System: {(enableComboSystem ? "Enabled" : "Disabled")}");
    }

    private void OnValidate()
    {
        // Ensure valid values in inspector
        if (basePointsPerMatch < 1) basePointsPerMatch = 1;
        if (comboMultiplierThreshold < 2) comboMultiplierThreshold = 2;
        if (comboMultiplier < 1f) comboMultiplier = 1f;
        if (maxComboMultiplier < 1) maxComboMultiplier = 1;
    }
}