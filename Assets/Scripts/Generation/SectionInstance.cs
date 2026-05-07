using UnityEngine;

/// <summary>
/// Represents one generated combat section.
/// 
/// This script tracks:
/// - how many enemies are alive
/// - whether the section is cleared
/// - gameplay metrics for this section
/// 
/// Later, these metrics will be passed into the adaptive difficulty system.
/// </summary>
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

    /// <summary>
    /// Called by the SectionGenerator after spawning enemies.
    /// </summary>
    public void Setup(int newSectionIndex, int enemyCount, PlayerHealth player, DifficultyManager difficulty)
    {
        difficultyManager = difficulty;
        sectionIndex = newSectionIndex;

        totalEnemies = enemyCount;
        enemiesAlive = enemyCount;

        sectionCleared = enemyCount <= 0;

        playerHealth = player;

        float startingHealth = 100f;

        if (playerHealth != null)
        {
            startingHealth = playerHealth.GetCurrentHealth();
        }

        metrics.StartSection(sectionIndex, startingHealth, enemyCount);

        Debug.Log("Section " + sectionIndex + " started. Enemies: " + enemiesAlive);
    }

    /// <summary>
    /// Called by PlayerShooter whenever the player fires.
    /// </summary>
    public void RegisterShotFired()
    {
        if (sectionCleared)
        {
            return;
        }

        metrics.RecordShotFired();
    }

    /// <summary>
    /// Called by PlayerShooter whenever the player hits an enemy.
    /// </summary>
    public void RegisterShotHit()
    {
        if (sectionCleared)
        {
            return;
        }

        metrics.RecordShotHit();
    }

    /// <summary>
    /// Called by enemies when they die.
    /// </summary>
    public void RegisterEnemyDeath(float enemyTimeToKill)
    {
        enemiesAlive--;

        enemiesAlive = Mathf.Max(enemiesAlive, 0);

        metrics.RecordEnemyKilled(enemyTimeToKill);

        Debug.Log("Section " + sectionIndex + " enemy killed. Remaining: " + enemiesAlive);

        if (enemiesAlive <= 0)
        {
            CompleteSection();
        }
    }

    private void CompleteSection()
    {
        if (sectionCleared)
        {
            return;
        }

        sectionCleared = true;

        float endingHealth = 100f;

        if (playerHealth != null)
        {
            endingHealth = playerHealth.GetCurrentHealth();
        }

        metrics.EndSection(endingHealth);

        Debug.Log("Section " + sectionIndex + " cleared. Move to the end trigger.");
        Debug.Log(metrics.GetDebugSummary());

        if (difficultyManager != null)
        {
            difficultyManager.AnalyseSectionPerformance(metrics);
        }
    }

    public bool CanProgress()
    {
        return sectionCleared;
    }
}