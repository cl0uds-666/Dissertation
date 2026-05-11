using UnityEngine;

public class SectionInstance : MonoBehaviour
{
    [Header("Section Info")]
    public int sectionIndex;

    [Header("Enemy Tracking")]
    public int totalEnemies;
    public int enemiesAlive;

    [Header("Section State")]
    public bool sectionCleared;

    [Header("Detection State")]
    public bool isPlayerDetected;

    [Header("Metrics")]
    public SectionMetrics metrics = new SectionMetrics();

    [Header("Generated Walls")]
    public GameObject frontWallObject;
    public GameObject backWallObject;

    private PlayerHealth playerHealth;
    private DifficultyManager difficultyManager;
    private CSVLogger csvLogger;

    private EnemyLineOfSight[] sectionEnemyLineOfSight = new EnemyLineOfSight[0];

    public void Setup(
        int newSectionIndex,
        int enemyCount,
        int coverCount,
        int shooterCount,
        int chaserCount,
        PlayerHealth player,
        DifficultyManager difficulty,
        CSVLogger logger)
    {
        difficultyManager = difficulty;
        csvLogger = logger;
        sectionIndex = newSectionIndex;

        totalEnemies = enemyCount;
        enemiesAlive = enemyCount;
        sectionCleared = enemyCount <= 0;

        playerHealth = player;

        float startingHealth = 100f;
        if (playerHealth != null) { startingHealth = playerHealth.GetCurrentHealth(); }

        metrics.StartSection(sectionIndex, startingHealth, enemyCount, coverCount, shooterCount, chaserCount);

        sectionEnemyLineOfSight = GetComponentsInChildren<EnemyLineOfSight>(true);
        isPlayerDetected = false;

        Debug.Log("Section " + sectionIndex + " started. Enemies: " + enemiesAlive);
    }


    private void Update()
    {
        if (sectionCleared)
        {
            return;
        }

        UpdateDetectionMetrics();
    }

    private void UpdateDetectionMetrics()
    {
        bool detectedThisFrame = IsAnyLivingEnemySeeingPlayer();

        if (detectedThisFrame)
        {
            metrics.timeDetected += Time.deltaTime;
        }
        else
        {
            metrics.timeUndetected += Time.deltaTime;
        }

        // Count only rising edges: false -> true.
        if (!isPlayerDetected && detectedThisFrame)
        {
            metrics.timesDetected++;
        }

        isPlayerDetected = detectedThisFrame;
    }

    private bool IsAnyLivingEnemySeeingPlayer()
    {
        for (int i = 0; i < sectionEnemyLineOfSight.Length; i++)
        {
            EnemyLineOfSight lineOfSight = sectionEnemyLineOfSight[i];

            if (lineOfSight == null)
            {
                continue;
            }

            EnemyHealth enemyHealth = lineOfSight.GetComponent<EnemyHealth>();

            if (enemyHealth != null && enemyHealth.IsDead)
            {
                continue;
            }

            if (lineOfSight.CanSeePlayer)
            {
                return true;
            }
        }

        return false;
    }

    public void RegisterShotFired(){ if (sectionCleared) return; metrics.RecordShotFired(); }
    public void RegisterShotHit(){ if (sectionCleared) return; metrics.RecordShotHit(); }

    public void RegisterEnemyDeath(float enemyTimeToKill, bool wasStealthKill)
    {
        enemiesAlive--;
        enemiesAlive = Mathf.Max(enemiesAlive, 0);
        metrics.RecordEnemyKilled(enemyTimeToKill, wasStealthKill);

        if (enemiesAlive <= 0) { CompleteSection(); }
    }

    private void CompleteSection()
    {
        if (sectionCleared) { return; }
        sectionCleared = true;

        float endingHealth = 100f;
        if (playerHealth != null) { endingHealth = playerHealth.GetCurrentHealth(); }
        metrics.EndSection(endingHealth);

        DifficultyAnalysisResult analysisResult = null;
        if (difficultyManager != null)
        {
            analysisResult = difficultyManager.AnalyseSectionPerformance(metrics);
        }

        if (csvLogger != null && analysisResult != null)
        {
            csvLogger.LogSectionResult(metrics, analysisResult);
        }
    }


    public bool CanProgressViaStealth(float requiredUndetectedRatio, int requiredStealthKills, bool requireZeroDetections)
    {
        float totalTrackedTime = metrics.timeDetected + metrics.timeUndetected;
        float undetectedRatio = totalTrackedTime > 0f ? metrics.timeUndetected / totalTrackedTime : 0f;
        float clampedRequiredRatio = Mathf.Clamp01(requiredUndetectedRatio);

        if (undetectedRatio + 0.0001f < clampedRequiredRatio)
        {
            return false;
        }

        if (metrics.stealthKills < requiredStealthKills)
        {
            return false;
        }

        if (requireZeroDetections)
        {
            bool hasAnyDetectionSignal = metrics.timesDetected > 0 || metrics.timeDetected > 0f || isPlayerDetected;
            if (hasAnyDetectionSignal)
            {
                return false;
            }
        }

        return true;
    }

    public bool CanProgress(){ return sectionCleared; }
}
