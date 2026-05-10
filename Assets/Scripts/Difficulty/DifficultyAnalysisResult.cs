using UnityEngine;

/// <summary>
/// Stores the result of analysing one section's metrics.
/// 
/// This is returned by DifficultyManager so other systems (UI, CSV logger)
/// can use exactly the same analysis data.
/// </summary>
[System.Serializable]
public class DifficultyAnalysisResult
{
    [Header("Difficulty State Change")]
    public float difficultyStateBefore;
    public float difficultyStateAfter;

    [Header("Flow Result")]
    public int flowScore;
    public string flowResult;

    [Header("Per-Metric Ratings")]
    public string healthRating;
    public string completionTimeRating;
    public string accuracyRating;
    public string ttkRating;
}
