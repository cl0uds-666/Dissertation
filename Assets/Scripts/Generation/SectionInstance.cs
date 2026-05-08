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

    [Header("Metrics")]
    public SectionMetrics metrics = new SectionMetrics();

    private PlayerHealth playerHealth;
    private DifficultyManager difficultyManager;
    private CSVLogger csvLogger;

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

        Debug.Log("Section " + sectionIndex + " started. Enemies: " + enemiesAlive);
    }

    public void RegisterShotFired(){ if (sectionCleared) return; metrics.RecordShotFired(); }
    public void RegisterShotHit(){ if (sectionCleared) return; metrics.RecordShotHit(); }

    public void RegisterEnemyDeath(float enemyTimeToKill)
    {
        enemiesAlive--;
        enemiesAlive = Mathf.Max(enemiesAlive, 0);
        metrics.RecordEnemyKilled(enemyTimeToKill);

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

    public bool CanProgress(){ return sectionCleared; }
}
