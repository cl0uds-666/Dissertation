using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores gameplay data collected during one generated combat section.
/// 
/// This data is used by the adaptive difficulty system and CSV logging.
/// </summary>
[System.Serializable]
public class SectionMetrics
{
    [Header("Section Info")]
    public int sectionIndex;

    [Header("Section Composition")]
    public int coverCount;
    public int shooterCount;
    public int chaserCount;

    [Header("Time Metrics")]
    public float sectionStartTime;
    public float sectionEndTime;
    public float completionTime;

    [Header("Health Metrics")]
    public float playerHealthAtStart;
    public float playerHealthAtEnd;
    public float playerHealthLost;

    [Header("Shooting Metrics")]
    public int shotsFired;
    public int shotsHit;
    public float accuracyPercent;

    [Header("Enemy Metrics")]
    public int enemiesSpawned;
    public int enemiesKilled;
    public int stealthKills;
    public int detectedKills;

    [Header("Detection Metrics")]
    public float timeDetected;
    public float timeUndetected;
    public int timesDetected;
    public List<float> enemyTimeToKillValues = new List<float>();
    public float averageEnemyTimeToKill;

    public void StartSection(
        int newSectionIndex,
        float currentPlayerHealth,
        int enemyCount,
        int newCoverCount,
        int newShooterCount,
        int newChaserCount)
    {
        sectionIndex = newSectionIndex;

        coverCount = newCoverCount;
        shooterCount = newShooterCount;
        chaserCount = newChaserCount;

        sectionStartTime = Time.time;
        sectionEndTime = 0f;
        completionTime = 0f;

        playerHealthAtStart = currentPlayerHealth;
        playerHealthAtEnd = currentPlayerHealth;
        playerHealthLost = 0f;

        shotsFired = 0;
        shotsHit = 0;
        accuracyPercent = 0f;

        enemiesSpawned = enemyCount;
        enemiesKilled = 0;
        stealthKills = 0;
        detectedKills = 0;

        timeDetected = 0f;
        timeUndetected = 0f;
        timesDetected = 0;

        enemyTimeToKillValues.Clear();
        averageEnemyTimeToKill = 0f;
    }

    public void RecordShotFired(){ shotsFired++; RecalculateAccuracy(); }
    public void RecordShotHit(){ shotsHit++; RecalculateAccuracy(); }
    public void RecordEnemyKilled(float timeToKill, bool wasStealthKill)
    {
        enemiesKilled++;

        if (wasStealthKill)
        {
            stealthKills++;
        }
        else
        {
            detectedKills++;
        }

        enemyTimeToKillValues.Add(timeToKill);
        RecalculateAverageTTK();
    }

    public void EndSection(float currentPlayerHealth)
    {
        sectionEndTime = Time.time;
        completionTime = sectionEndTime - sectionStartTime;
        playerHealthAtEnd = currentPlayerHealth;
        playerHealthLost = playerHealthAtStart - playerHealthAtEnd;
        RecalculateAccuracy();
        RecalculateAverageTTK();
    }

    private void RecalculateAccuracy()
    {
        if (shotsFired <= 0) { accuracyPercent = 0f; return; }
        accuracyPercent = ((float)shotsHit / shotsFired) * 100f;
    }

    private void RecalculateAverageTTK()
    {
        if (enemyTimeToKillValues.Count <= 0) { averageEnemyTimeToKill = 0f; return; }
        float total = 0f;
        for (int i = 0; i < enemyTimeToKillValues.Count; i++) { total += enemyTimeToKillValues[i]; }
        averageEnemyTimeToKill = total / enemyTimeToKillValues.Count;
    }
}
