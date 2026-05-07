using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores gameplay data collected during one generated combat section.
/// 
/// This data will later be used by the adaptive difficulty system to decide
/// how the next generated section should change.
/// </summary>
[System.Serializable]
public class SectionMetrics
{
    [Header("Section Info")]
    public int sectionIndex;

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
    public List<float> enemyTimeToKillValues = new List<float>();
    public float averageEnemyTimeToKill;

    public void StartSection(int newSectionIndex, float currentPlayerHealth, int enemyCount)
    {
        sectionIndex = newSectionIndex;

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

        enemyTimeToKillValues.Clear();
        averageEnemyTimeToKill = 0f;
    }

    public void RecordShotFired()
    {
        shotsFired++;
        RecalculateAccuracy();
    }

    public void RecordShotHit()
    {
        shotsHit++;
        RecalculateAccuracy();
    }

    public void RecordEnemyKilled(float timeToKill)
    {
        enemiesKilled++;
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
        if (shotsFired <= 0)
        {
            accuracyPercent = 0f;
            return;
        }

        accuracyPercent = ((float)shotsHit / shotsFired) * 100f;
    }

    private void RecalculateAverageTTK()
    {
        if (enemyTimeToKillValues.Count <= 0)
        {
            averageEnemyTimeToKill = 0f;
            return;
        }

        float total = 0f;

        for (int i = 0; i < enemyTimeToKillValues.Count; i++)
        {
            total += enemyTimeToKillValues[i];
        }

        averageEnemyTimeToKill = total / enemyTimeToKillValues.Count;
    }

    public string GetDebugSummary()
    {
        return
            "Section " + sectionIndex + " Metrics\n" +
            "Completion Time: " + completionTime.ToString("F2") + "s\n" +
            "Health Start: " + playerHealthAtStart.ToString("F0") + "\n" +
            "Health End: " + playerHealthAtEnd.ToString("F0") + "\n" +
            "Health Lost: " + playerHealthLost.ToString("F0") + "\n" +
            "Shots Fired: " + shotsFired + "\n" +
            "Shots Hit: " + shotsHit + "\n" +
            "Accuracy: " + accuracyPercent.ToString("F1") + "%\n" +
            "Enemies Killed: " + enemiesKilled + "/" + enemiesSpawned + "\n" +
            "Average Enemy TTK: " + averageEnemyTimeToKill.ToString("F2") + "s";
    }
}