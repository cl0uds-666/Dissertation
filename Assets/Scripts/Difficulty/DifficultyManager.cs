using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Mode")]
    [Tooltip("If false, difficulty state stays fixed, but analysis and logging still run.")]
    public bool adaptiveDifficultyEnabled = true;

    [Header("Continuous Difficulty State")]
    [Tooltip("Unbounded latent difficulty state. Higher means more challenge.")]
    public float currentDifficultyState = 0f;

    [Header("Adaptation Dynamics")]
    [Tooltip("How strongly each section's flow score changes difficulty state.")]
    public float adaptationGain = 0.22f;
    [Tooltip("Mean-reversion prevents long-term drift and encourages settling.")]
    public float meanReversion = 0.04f;
    [Tooltip("Hard limit on per-section update magnitude for stability.")]
    public float maxStepPerSection = 0.75f;
    [Tooltip("Scales negative deltas (difficulty downshifts) so hard spikes reduce challenge more gradually.")]
    [Range(0f, 1f)]
    public float downshiftMultiplier = 0.7f;
    [Tooltip("Number of recent sections used to smooth each metric before scoring.")]
    [Min(1)]
    public int smoothingWindow = 3;
    [Tooltip("Minimum confidence factor applied while smoothing history is still filling.")]
    [Range(0f, 1f)]
    public float confidenceFloor = 0.5f;

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
    public int healthLostWeight = 2;
    public int completionTimeWeight = 1;
    public int accuracyWeight = 1;
    public int ttkWeight = 1;

    [Header("Stealth Branch Weights")]
    public int stealthUndetectedWeight = 2;
    public int stealthDetectionWeight = 2;
    public int stealthKillWeight = 1;

    [Header("Stealth Branch Flow Zone")]
    [Range(0f, 1f)] public float stealthUndetectedTooLowMax = 0.55f;
    [Range(0f, 1f)] public float stealthUndetectedFlowMin = 0.75f;
    [Range(0f, 1f)] public float stealthKillTooLowMax = 0.35f;
    [Range(0f, 1f)] public float stealthKillFlowMin = 0.6f;
    [Range(0f, 1f)] public float stealthDetectionTooHighMin = 0.6f;
    [Range(0f, 1f)] public float stealthDetectionFlowMax = 0.3f;

    [Header("Debug")]
    public bool logDifficultyChanges = true;

    [Header("Playstyle Classification")]
    [Tooltip("Weight for undetected time share when inferring stealth intent.")]
    public float stealthIntentUndetectedWeight = 0.5f;
    [Tooltip("Weight for stealth kill share when inferring stealth intent.")]
    public float stealthIntentKillWeight = 0.4f;
    [Tooltip("Penalty weight for detection pressure when inferring stealth intent.")]
    public float stealthIntentDetectionPenaltyWeight = 0.1f;
    [Tooltip("Detection events per second that maps to maximum normalized detection pressure.")]
    public float maxDetectionEventsPerSecond = 0.25f;
    [Range(0f, 1f)]
    [Tooltip("Enter stealth classification when score meets/exceeds this threshold.")]
    public float stealthEnterThreshold = 0.65f;
    [Range(0f, 1f)]
    [Tooltip("Exit stealth classification when score drops below this threshold.")]
    public float stealthExitThreshold = 0.55f;

    private DifficultyAnalysisResult lastAnalysisResult;
    private bool wasClassifiedAsStealthLastSection;

    private readonly System.Collections.Generic.List<float> healthLostHistory = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> completionTimeHistory = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> accuracyHistory = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> ttkHistory = new System.Collections.Generic.List<float>();

    public DifficultyAnalysisResult GetLastAnalysisResult() { return lastAnalysisResult; }

    public DifficultyProfile GetCurrentProfile()
    {
        float d = currentDifficultyState;

        float enemyPressure = Sigmoid(d * 0.95f);
        float lethalityPressure = Sigmoid(d * 0.85f);
        float coverRelief = Sigmoid(-d * 0.75f);

        int enemyCount = Mathf.Clamp(Mathf.RoundToInt(2f + Mathf.Exp(Mathf.Clamp(d, -2f, 4f)) * 0.95f), 2, 24);
        float enemyHealth = Mathf.Clamp(35f + (enemyPressure * 95f), 25f, 150f);
        float enemyMoveSpeed = Mathf.Clamp(2.1f + (enemyPressure * 2.8f), 1.6f, 5.2f);

        float enemyShotDamage = Mathf.Clamp(5.5f + (lethalityPressure * 9f), 4f, 15.5f);
        float enemyFireCooldown = Mathf.Clamp(2.55f - (lethalityPressure * 1.75f), 0.75f, 2.8f);
        float enemyShotRange = Mathf.Clamp(24f + (enemyPressure * 23f), 20f, 48f);
        float enemyShotRadius = Mathf.Clamp(0.10f + (enemyPressure * 0.11f), 0.08f, 0.24f);
        float enemyShotSpread = Mathf.Clamp(0.50f - (enemyPressure * 0.42f), 0.04f, 0.58f);
        float peekDamageChance = Mathf.Clamp(0.18f + (lethalityPressure * 0.64f), 0.08f, 0.86f);

        int coverCount = Mathf.Clamp(Mathf.RoundToInt(12f - (enemyPressure * 9f) + (coverRelief * 4f)), 3, 16);
        float coverMinSize = Mathf.Clamp(0.9f + (coverRelief * 1.8f), 0.8f, 3.2f);
        float coverMaxSize = Mathf.Clamp(2.4f + (coverRelief * 2.8f), 1.8f, 5.8f);
        float coverMinHeight = Mathf.Clamp(0.8f + (coverRelief * 0.55f), 0.7f, 1.5f);
        float coverMaxHeight = Mathf.Clamp(1.3f + (coverRelief * 1.35f), 1.0f, 2.8f);

        bool sideCoverEnabled = d < 2.1f;
        float sideCoverContinuity = Mathf.Clamp(0.96f - (enemyPressure * 0.88f), 0f, 1f);
        float sideCoverHeight = Mathf.Clamp(1.0f + (coverRelief * 0.55f), 0.9f, 1.7f);
        float sideCoverSegmentLength = Mathf.Clamp(3.0f - (enemyPressure * 2.1f), 0.9f, 3.2f);
        float sideCoverGapMin = Mathf.Clamp(0.75f + (enemyPressure * 3.1f), 0.6f, 4.2f);
        float sideCoverGapMax = Mathf.Clamp(1.7f + (enemyPressure * 3.8f), 1.2f, 5.0f);

        return new DifficultyProfile(
            d,
            enemyCount,
            enemyHealth,
            enemyMoveSpeed,
            true,
            true,
            enemyShotDamage,
            enemyFireCooldown,
            enemyShotRange,
            enemyShotRadius,
            enemyShotSpread,
            peekDamageChance,
            coverCount,
            coverMinSize,
            coverMaxSize,
            coverMinHeight,
            coverMaxHeight,
            sideCoverEnabled,
            sideCoverContinuity,
            sideCoverHeight,
            sideCoverSegmentLength,
            sideCoverGapMin,
            sideCoverGapMax
        );
    }

    public DifficultyAnalysisResult AnalyseSectionPerformance(SectionMetrics metrics)
    {
        float oldDifficulty = currentDifficultyState;
        int flowScore = 0;

        float smoothedHealthLost = GetSmoothedMetric(healthLostHistory, metrics.playerHealthLost);
        float smoothedCompletionTime = GetSmoothedMetric(completionTimeHistory, metrics.completionTime);
        float smoothedAccuracy = GetSmoothedMetric(accuracyHistory, metrics.accuracyPercent);
        float smoothedTTK = GetSmoothedMetric(ttkHistory, metrics.averageEnemyTimeToKill);

        PlaystyleSnapshot playstyle = ClassifyPlaystyle(metrics);

        string healthResult = "Not Used";
        string timeResult = "Not Used";
        string accuracyResult = "Not Used";
        string ttkResult = "Not Used";
        string stealthUndetectedResult = "Not Used";
        string stealthDetectionResult = "Not Used";
        string stealthKillResult = "Not Used";
        string scoringBranch = playstyle.isStealth ? "Stealth" : "Assault";

        if (playstyle.isStealth)
        {
            stealthUndetectedResult = EvaluateStealthUndetected(playstyle.undetectedRatio, ref flowScore);
            stealthDetectionResult = EvaluateStealthDetection(playstyle.detectionPressure, ref flowScore);
            stealthKillResult = EvaluateStealthKill(playstyle.stealthKillRatio, ref flowScore);
        }
        else
        {
            healthResult = EvaluateHealthLost(smoothedHealthLost, ref flowScore);
            timeResult = EvaluateCompletionTime(smoothedCompletionTime, ref flowScore);
            accuracyResult = EvaluateAccuracy(smoothedAccuracy, ref flowScore);
            ttkResult = EvaluateTTK(smoothedTTK, ref flowScore);
        }

        string overallResult = "Flow Zone";
        if (flowScore < 0) { overallResult = "Too Easy"; }
        else if (flowScore > 0) { overallResult = "Too Hard"; }

        float rawDelta = (-flowScore * adaptationGain) - (meanReversion * oldDifficulty);
        float confidence = GetSmoothingConfidence();
        rawDelta *= confidence;

        if (rawDelta < 0f)
        {
            rawDelta *= Mathf.Clamp01(downshiftMultiplier);
        }

        float delta = Mathf.Clamp(rawDelta, -maxStepPerSection, maxStepPerSection);
        float newDifficulty = oldDifficulty;

        if (adaptiveDifficultyEnabled)
        {
            newDifficulty = oldDifficulty + delta;
            currentDifficultyState = newDifficulty;
        }

        DifficultyAnalysisResult result = new DifficultyAnalysisResult();
        result.difficultyStateBefore = oldDifficulty;
        result.difficultyStateAfter = adaptiveDifficultyEnabled ? currentDifficultyState : oldDifficulty;
        result.flowScore = flowScore;
        result.flowResult = overallResult;
        result.healthRating = healthResult;
        result.completionTimeRating = timeResult;
        result.accuracyRating = accuracyResult;
        result.ttkRating = ttkResult;
        result.playstyleClassification = playstyle.isStealth ? "Stealth" : "Non-Stealth";
        result.stealthIntentScore = playstyle.stealthIntentScore;
        result.undetectedRatio = playstyle.undetectedRatio;
        result.stealthKillRatio = playstyle.stealthKillRatio;
        result.detectionPressure = playstyle.detectionPressure;
        result.scoringBranchUsed = scoringBranch;
        result.stealthUndetectedRating = stealthUndetectedResult;
        result.stealthDetectionRating = stealthDetectionResult;
        result.stealthKillRating = stealthKillResult;

        lastAnalysisResult = result;

        if (logDifficultyChanges)
        {
            Debug.Log(
                "Flow Analysis After Section " + metrics.sectionIndex +
                "\nHealth Lost: " + metrics.playerHealthLost.ToString("F0") + " -> " + healthResult +
                "\nCompletion Time: " + metrics.completionTime.ToString("F2") + "s -> " + timeResult +
                "\nAccuracy: " + metrics.accuracyPercent.ToString("F1") + "% -> " + accuracyResult +
                "\nAverage Enemy TTK: " + metrics.averageEnemyTimeToKill.ToString("F2") + "s -> " + ttkResult +
                "\nSmoothed Metrics [N=" + Mathf.Max(1, smoothingWindow) + "]: HL=" + smoothedHealthLost.ToString("F1") + ", CT=" + smoothedCompletionTime.ToString("F2") + "s, ACC=" + smoothedAccuracy.ToString("F1") + "%, TTK=" + smoothedTTK.ToString("F2") + "s" +
                "\nSmoothing Confidence: " + confidence.ToString("F2") +
                "\nPlaystyle: " + result.playstyleClassification +
                " (score " + playstyle.stealthIntentScore.ToString("F2") +
                ", undetected " + playstyle.undetectedRatio.ToString("P0") +
                ", stealth kills " + playstyle.stealthKillRatio.ToString("P0") +
                ", detection pressure " + playstyle.detectionPressure.ToString("F2") + ")" +
                "\nScoring Branch: " + scoringBranch +
                "\nStealth Ratings: Undetected=" + stealthUndetectedResult + ", Detection=" + stealthDetectionResult + ", StealthKills=" + stealthKillResult +
                "\nFlow Score: " + flowScore +
                "\nOverall Result: " + overallResult +
                "\nAdaptive Enabled: " + adaptiveDifficultyEnabled +
                "\nDifficulty State: " + oldDifficulty.ToString("F2") + " -> " + result.difficultyStateAfter.ToString("F2") +
                "\nDelta (raw/clamped): " + rawDelta.ToString("F3") + " / " + delta.ToString("F3")
            );
        }

        return result;
    }

    private float GetSmoothedMetric(System.Collections.Generic.List<float> history, float currentValue)
    {
        if (currentValue <= 0f && history == ttkHistory)
        {
            return currentValue;
        }

        int window = Mathf.Max(1, smoothingWindow);
        history.Add(currentValue);

        if (history.Count > window)
        {
            history.RemoveAt(0);
        }

        float sum = 0f;
        for (int i = 0; i < history.Count; i++)
        {
            sum += history[i];
        }

        return sum / history.Count;
    }

    private float GetSmoothingConfidence()
    {
        int window = Mathf.Max(1, smoothingWindow);
        int samples = Mathf.Min(healthLostHistory.Count, Mathf.Min(completionTimeHistory.Count, Mathf.Min(accuracyHistory.Count, ttkHistory.Count)));
        float historyRatio = Mathf.Clamp01((float)samples / window);
        return Mathf.Lerp(Mathf.Clamp01(confidenceFloor), 1f, historyRatio);
    }


    private struct PlaystyleSnapshot
    {
        public bool isStealth;
        public float stealthIntentScore;
        public float undetectedRatio;
        public float stealthKillRatio;
        public float detectionPressure;
    }

    private PlaystyleSnapshot ClassifyPlaystyle(SectionMetrics metrics)
    {
        float totalTrackedTime = metrics.timeDetected + metrics.timeUndetected;
        float undetectedRatio = totalTrackedTime > 0f ? metrics.timeUndetected / totalTrackedTime : 0f;

        float stealthKillRatio = metrics.enemiesKilled > 0
            ? (float)metrics.stealthKills / metrics.enemiesKilled
            : 0f;

        float completionTime = Mathf.Max(metrics.completionTime, 0.0001f);
        float detectionRate = metrics.timesDetected / completionTime;
        float normalizedDetectionPressure = maxDetectionEventsPerSecond > 0f
            ? Mathf.Clamp01(detectionRate / maxDetectionEventsPerSecond)
            : 1f;

        float weightedScore =
            (undetectedRatio * stealthIntentUndetectedWeight) +
            (stealthKillRatio * stealthIntentKillWeight) -
            (normalizedDetectionPressure * stealthIntentDetectionPenaltyWeight);

        float totalWeight = Mathf.Max(0.0001f,
            Mathf.Abs(stealthIntentUndetectedWeight) +
            Mathf.Abs(stealthIntentKillWeight) +
            Mathf.Abs(stealthIntentDetectionPenaltyWeight));

        float stealthIntentScore = Mathf.Clamp01(weightedScore / totalWeight);

        bool isStealth = wasClassifiedAsStealthLastSection
            ? stealthIntentScore >= Mathf.Clamp01(stealthExitThreshold)
            : stealthIntentScore >= Mathf.Clamp01(stealthEnterThreshold);

        wasClassifiedAsStealthLastSection = isStealth;

        PlaystyleSnapshot snapshot;
        snapshot.isStealth = isStealth;
        snapshot.stealthIntentScore = stealthIntentScore;
        snapshot.undetectedRatio = undetectedRatio;
        snapshot.stealthKillRatio = stealthKillRatio;
        snapshot.detectionPressure = normalizedDetectionPressure;
        return snapshot;
    }

    private float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }

    private string EvaluateHealthLost(float healthLost, ref int flowScore)
    {
        if (healthLost <= healthLostTooEasyMax) { flowScore -= healthLostWeight; return "Too Easy"; }
        if (healthLost >= healthLostTooHardMin) { flowScore += healthLostWeight; return "Too Hard"; }
        if (healthLost >= healthLostFlowMin && healthLost <= healthLostFlowMax) { return "Flow Zone"; }
        return "Borderline";
    }

    private string EvaluateCompletionTime(float completionTime, ref int flowScore)
    {
        if (completionTime <= completionTooFastMax) { flowScore -= completionTimeWeight; return "Too Easy"; }
        if (completionTime >= completionTooSlowMin) { flowScore += completionTimeWeight; return "Too Hard"; }
        if (completionTime >= completionFlowMin && completionTime <= completionFlowMax) { return "Flow Zone"; }
        return "Borderline";
    }

    private string EvaluateAccuracy(float accuracy, ref int flowScore)
    {
        if (accuracy >= accuracyTooHighMin) { flowScore -= accuracyWeight; return "Too Easy"; }
        if (accuracy <= accuracyTooLowMax) { flowScore += accuracyWeight; return "Too Hard"; }
        if (accuracy >= accuracyFlowMin && accuracy <= accuracyFlowMax) { return "Flow Zone"; }
        return "Borderline";
    }

    private string EvaluateTTK(float averageTTK, ref int flowScore)
    {
        if (averageTTK <= 0f) { return "No Data"; }
        if (averageTTK <= ttkTooFastMax) { flowScore -= ttkWeight; return "Too Easy"; }
        if (averageTTK >= ttkTooSlowMin) { flowScore += ttkWeight; return "Too Hard"; }
        if (averageTTK >= ttkFlowMin && averageTTK <= ttkFlowMax) { return "Flow Zone"; }
        return "Borderline";
    }

    private string EvaluateStealthUndetected(float undetectedRatio, ref int flowScore)
    {
        if (undetectedRatio <= stealthUndetectedTooLowMax) { flowScore += stealthUndetectedWeight; return "Too Hard"; }
        if (undetectedRatio >= stealthUndetectedFlowMin) { flowScore -= stealthUndetectedWeight; return "Too Easy"; }
        return "Borderline";
    }

    private string EvaluateStealthDetection(float detectionPressure, ref int flowScore)
    {
        if (detectionPressure >= stealthDetectionTooHighMin) { flowScore += stealthDetectionWeight; return "Too Hard"; }
        if (detectionPressure <= stealthDetectionFlowMax) { flowScore -= stealthDetectionWeight; return "Too Easy"; }
        return "Borderline";
    }

    private string EvaluateStealthKill(float stealthKillRatio, ref int flowScore)
    {
        if (stealthKillRatio <= stealthKillTooLowMax) { flowScore += stealthKillWeight; return "Too Hard"; }
        if (stealthKillRatio >= stealthKillFlowMin) { flowScore -= stealthKillWeight; return "Too Easy"; }
        return "Borderline";
    }
}
