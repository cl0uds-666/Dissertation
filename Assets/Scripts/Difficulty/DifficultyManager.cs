using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public bool adaptiveDifficultyEnabled = true;
    public int currentDifficultyScore = 3;
    public int minDifficultyScore = 1;
    public int maxDifficultyScore = 10;
    public float healthLostTooEasyMax = 5f, healthLostFlowMin = 10f, healthLostFlowMax = 35f, healthLostTooHardMin = 45f;
    public float completionTooFastMax = 8f, completionFlowMin = 10f, completionFlowMax = 25f, completionTooSlowMin = 30f;
    public float accuracyTooLowMax = 25f, accuracyFlowMin = 35f, accuracyFlowMax = 70f, accuracyTooHighMin = 80f;
    public float ttkTooFastMax = 2f, ttkFlowMin = 3f, ttkFlowMax = 8f, ttkTooSlowMin = 10f;
    public int healthLostWeight = 2, completionTimeWeight = 1, accuracyWeight = 1, ttkWeight = 1;
    public int tooEasyScoreThreshold = -2, tooHardScoreThreshold = 2;
    public bool logDifficultyChanges = true;
    private DifficultyAnalysisResult lastAnalysisResult;

    public DifficultyAnalysisResult GetLastAnalysisResult() { return lastAnalysisResult; }
    public DifficultyProfile GetCurrentProfile(){ DifficultyProfile p = DifficultyProfile.CreateDefault(); float t=currentDifficultyScore/10f; p.difficultyScore=currentDifficultyScore; p.enemyCount=Mathf.Clamp(2+currentDifficultyScore/2,2,7); p.enemyHealth=30f+currentDifficultyScore*8f; p.enemyShotDamage=4f+currentDifficultyScore; p.enemyFireCooldown=Mathf.Lerp(2.4f,0.9f,t); p.enemyShotSpread=Mathf.Lerp(0.45f,0.06f,t); p.coverCount=Mathf.Clamp(14-currentDifficultyScore,3,14); p.enemyVisionRange=Mathf.Lerp(15f,24f,t); p.enemyVisionAngle=Mathf.Lerp(95f,60f,t); p.enemyReactionTime=Mathf.Lerp(0.55f,0.1f,t); p.enemyPatrolSpeed=Mathf.Lerp(2f,3.8f,t); p.enemyChaseSpeed=Mathf.Lerp(2.8f,4.8f,t); p.enemyRushSpeed=Mathf.Lerp(4f,6.2f,t); p.enemyPeekDuration=Mathf.Lerp(0.8f,1.5f,t); p.enemyHideDuration=Mathf.Lerp(2.2f,1f,t); p.blockerAheadDistance=Mathf.Lerp(8f,4f,t); p.suppressorFireCooldownMultiplier=Mathf.Lerp(0.85f,0.55f,t); return p; }

    public DifficultyAnalysisResult AnalyseSectionPerformance(SectionMetrics metrics)
    {
        int oldDifficulty = currentDifficultyScore; int flowScore = 0;
        string healthResult = EvaluateHealthLost(metrics.playerHealthLost, ref flowScore);
        string timeResult = EvaluateCompletionTime(metrics.completionTime, ref flowScore);
        string accuracyResult = EvaluateAccuracy(metrics.accuracyPercent, ref flowScore);
        string ttkResult = EvaluateTTK(metrics.averageEnemyTimeToKill, ref flowScore);
        string overallResult = "Flow Zone"; int proposedDifficulty = oldDifficulty;
        if (flowScore <= tooEasyScoreThreshold) { proposedDifficulty++; overallResult = "Too Easy"; } else if (flowScore >= tooHardScoreThreshold) { proposedDifficulty--; overallResult = "Too Hard"; }
        int newDifficulty = Mathf.Clamp(proposedDifficulty, minDifficultyScore, maxDifficultyScore); if (adaptiveDifficultyEnabled) currentDifficultyScore = newDifficulty;
        DifficultyAnalysisResult result = new DifficultyAnalysisResult{ difficultyBefore = oldDifficulty, difficultyAfter = adaptiveDifficultyEnabled ? currentDifficultyScore : oldDifficulty, flowScore = flowScore, flowResult = overallResult, healthRating = healthResult, completionTimeRating = timeResult, accuracyRating = accuracyResult, ttkRating = ttkResult};
        lastAnalysisResult = result; return result;
    }
    private string EvaluateHealthLost(float v, ref int s){ if (v<=healthLostTooEasyMax){s-=healthLostWeight; return "Too Easy";} if(v>=healthLostTooHardMin){s+=healthLostWeight; return "Too Hard";} if(v>=healthLostFlowMin&&v<=healthLostFlowMax) return "Flow Zone"; return "Borderline"; }
    private string EvaluateCompletionTime(float v, ref int s){ if (v<=completionTooFastMax){s-=completionTimeWeight; return "Too Easy";} if(v>=completionTooSlowMin){s+=completionTimeWeight; return "Too Hard";} if(v>=completionFlowMin&&v<=completionFlowMax) return "Flow Zone"; return "Borderline"; }
    private string EvaluateAccuracy(float v, ref int s){ if (v>=accuracyTooHighMin){s-=accuracyWeight; return "Too Easy";} if(v<=accuracyTooLowMax){s+=accuracyWeight; return "Too Hard";} if(v>=accuracyFlowMin&&v<=accuracyFlowMax) return "Flow Zone"; return "Borderline"; }
    private string EvaluateTTK(float v, ref int s){ if(v<=0f) return "No Data"; if(v<=ttkTooFastMax){s-=ttkWeight; return "Too Easy";} if(v>=ttkTooSlowMin){s+=ttkWeight; return "Too Hard";} if(v>=ttkFlowMin&&v<=ttkFlowMax) return "Flow Zone"; return "Borderline"; }
}
