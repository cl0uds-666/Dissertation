using UnityEngine;

/// <summary>
/// Analyses section metrics and adjusts the difficulty score.
/// 
/// This version uses a simple flow zone scoring system.
/// Each metric is classified as:
///  too easy
///  within flow zone
///  too hard
/// 
/// The combined score decides whether the next section should become easier,
/// harder, or remain stable.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    [Header("Difficulty Score")]
    public int currentDifficultyScore = 3;
    public int minDifficultyScore = 1;
    public int maxDifficultyScore = 10;

    [Header("Health Lost Flow Zone")]
    public float healthLostTooEasyMax = 5f;
    public float healthLostFlowMin = 10f;
    public float healthLostFlowMax = 35f;
    public float healthLostTooHardMin = 45f;

    [Header("Completion Time Flow Zone")]
    public float completionTooFastMax = 8f;
    public float completionFlowMin = 10f;
    public float completionFlowMax = 25f;
    public float completionTooSlowMin = 30f;

    [Header("Accuracy Flow Zone")]
    public float accuracyTooLowMax = 25f;
    public float accuracyFlowMin = 35f;
    public float accuracyFlowMax = 70f;
    public float accuracyTooHighMin = 80f;

    [Header("Enemy TTK Flow Zone")]
    public float ttkTooFastMax = 2f;
    public float ttkFlowMin = 3f;
    public float ttkFlowMax = 8f;
    public float ttkTooSlowMin = 10f;

    [Header("Metric Weights")]
    [Tooltip("Health loss is important because it directly represents player survival pressure.")]
    public int healthLostWeight = 2;

    public int completionTimeWeight = 1;
    public int accuracyWeight = 1;
    public int ttkWeight = 1;

    [Header("Flow Score Thresholds")]
    [Tooltip("If score is this low or lower, the section was too easy.")]
    public int tooEasyScoreThreshold = -2;

    [Tooltip("If score is this high or higher, the section was too hard.")]
    public int tooHardScoreThreshold = 2;

    [Header("Debug")]
    public bool logDifficultyChanges = true;

    public DifficultyProfile GetCurrentProfile()
    {
        switch (currentDifficultyScore)
        {
            case 1:
                return new DifficultyProfile(
                    1,
                    2, 30f, 2.0f, false,
                    true, 5f, 2.4f, 25f, 0.10f, 0.45f,
                    14, 3.0f, 5.0f, 1.3f, 2.4f
                );

            case 2:
                return new DifficultyProfile(
                    2,
                    2, 35f, 2.2f, false,
                    true, 6f, 2.2f, 27f, 0.11f, 0.38f,
                    12, 2.7f, 4.7f, 1.2f, 2.3f
                );

            case 3:
                return new DifficultyProfile(
                    3,
                    3, 45f, 2.6f, false,
                    true, 7f, 2.0f, 30f, 0.12f, 0.32f,
                    10, 2.3f, 4.3f, 1.1f, 2.1f
                );

            case 4:
                return new DifficultyProfile(
                    4,
                    3, 50f, 3.0f, false,
                    true, 8f, 1.8f, 32f, 0.13f, 0.27f,
                    8, 1.8f, 3.8f, 1.0f, 1.9f
                );

            case 5:
                return new DifficultyProfile(
                    5,
                    4, 60f, 3.3f, false,
                    true, 9f, 1.6f, 34f, 0.14f, 0.22f,
                    7, 1.5f, 3.4f, 1.0f, 1.8f
                );

            case 6:
                return new DifficultyProfile(
                    6,
                    4, 70f, 3.6f, false,
                    true, 10f, 1.45f, 36f, 0.15f, 0.18f,
                    6, 1.3f, 3.0f, 0.9f, 1.7f
                );

            case 7:
                return new DifficultyProfile(
                    7,
                    5, 80f, 3.9f, false,
                    true, 11f, 1.3f, 38f, 0.16f, 0.14f,
                    5, 1.2f, 2.7f, 0.9f, 1.6f
                );

            case 8:
                return new DifficultyProfile(
                    8,
                    5, 90f, 4.2f, false,
                    true, 12f, 1.15f, 40f, 0.17f, 0.10f,
                    4, 1.0f, 2.5f, 0.8f, 1.5f
                );

            case 9:
                return new DifficultyProfile(
                    9,
                    6, 100f, 4.5f, false,
                    true, 13f, 1.0f, 42f, 0.18f, 0.07f,
                    4, 0.9f, 2.2f, 0.8f, 1.4f
                );

            case 10:
                return new DifficultyProfile(
                    10,
                    7, 115f, 4.8f, false,
                    true, 15f, 0.85f, 45f, 0.20f, 0.04f,
                    3, 0.8f, 2.0f, 0.7f, 1.3f
                );

            default:
                return new DifficultyProfile(
                    3,
                    3, 45f, 2.6f, false,
                    true, 7f, 2.0f, 30f, 0.12f, 0.32f,
                    10, 2.3f, 4.3f, 1.1f, 2.1f
                );
        }
    }

    public void AnalyseSectionPerformance(SectionMetrics metrics)
    {
        int oldDifficulty = currentDifficultyScore;

        int flowScore = 0;

        string healthResult = EvaluateHealthLost(metrics.playerHealthLost, ref flowScore);
        string timeResult = EvaluateCompletionTime(metrics.completionTime, ref flowScore);
        string accuracyResult = EvaluateAccuracy(metrics.accuracyPercent, ref flowScore);
        string ttkResult = EvaluateTTK(metrics.averageEnemyTimeToKill, ref flowScore);

        string overallResult;

        if (flowScore <= tooEasyScoreThreshold)
        {
            currentDifficultyScore++;
            overallResult = "Too Easy";
        }
        else if (flowScore >= tooHardScoreThreshold)
        {
            currentDifficultyScore--;
            overallResult = "Too Hard";
        }
        else
        {
            overallResult = "Flow Zone";
        }

        currentDifficultyScore = Mathf.Clamp(
            currentDifficultyScore,
            minDifficultyScore,
            maxDifficultyScore
        );

        if (logDifficultyChanges)
        {
            Debug.Log(
                "Flow Analysis After Section " + metrics.sectionIndex +
                "\nHealth Lost: " + metrics.playerHealthLost.ToString("F0") + " -> " + healthResult +
                "\nCompletion Time: " + metrics.completionTime.ToString("F2") + "s -> " + timeResult +
                "\nAccuracy: " + metrics.accuracyPercent.ToString("F1") + "% -> " + accuracyResult +
                "\nAverage Enemy TTK: " + metrics.averageEnemyTimeToKill.ToString("F2") + "s -> " + ttkResult +
                "\nFlow Score: " + flowScore +
                "\nOverall Result: " + overallResult +
                "\nDifficulty: " + oldDifficulty + " -> " + currentDifficultyScore
            );
        }
    }

    private string EvaluateHealthLost(float healthLost, ref int flowScore)
    {
        if (healthLost <= healthLostTooEasyMax)
        {
            flowScore -= healthLostWeight;
            return "Too Easy";
        }

        if (healthLost >= healthLostTooHardMin)
        {
            flowScore += healthLostWeight;
            return "Too Hard";
        }

        if (healthLost >= healthLostFlowMin && healthLost <= healthLostFlowMax)
        {
            return "Flow Zone";
        }

        return "Borderline";
    }

    private string EvaluateCompletionTime(float completionTime, ref int flowScore)
    {
        if (completionTime <= completionTooFastMax)
        {
            flowScore -= completionTimeWeight;
            return "Too Easy";
        }

        if (completionTime >= completionTooSlowMin)
        {
            flowScore += completionTimeWeight;
            return "Too Hard";
        }

        if (completionTime >= completionFlowMin && completionTime <= completionFlowMax)
        {
            return "Flow Zone";
        }

        return "Borderline";
    }

    private string EvaluateAccuracy(float accuracy, ref int flowScore)
    {
        if (accuracy >= accuracyTooHighMin)
        {
            flowScore -= accuracyWeight;
            return "Too Easy";
        }

        if (accuracy <= accuracyTooLowMax)
        {
            flowScore += accuracyWeight;
            return "Too Hard";
        }

        if (accuracy >= accuracyFlowMin && accuracy <= accuracyFlowMax)
        {
            return "Flow Zone";
        }

        return "Borderline";
    }

    private string EvaluateTTK(float averageTTK, ref int flowScore)
    {
        if (averageTTK <= 0f)
        {
            return "No Data";
        }

        if (averageTTK <= ttkTooFastMax)
        {
            flowScore -= ttkWeight;
            return "Too Easy";
        }

        if (averageTTK >= ttkTooSlowMin)
        {
            flowScore += ttkWeight;
            return "Too Hard";
        }

        if (averageTTK >= ttkFlowMin && averageTTK <= ttkFlowMax)
        {
            return "Flow Zone";
        }

        return "Borderline";
    }
}