using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes one CSV row per completed section.
/// 
/// File location:
/// Application.persistentDataPath/section_metrics_log.csv
/// </summary>
public class CSVLogger : MonoBehaviour
{
    private const string FileName = "section_metrics_log.csv";

    private string filePath;

    private void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, FileName);
        EnsureFileExistsWithHeader();
    }

    private void EnsureFileExistsWithHeader()
    {
        if (File.Exists(filePath))
        {
            return;
        }

        string header =
            "SectionIndex,DifficultyBefore,DifficultyAfter,FlowScore,FlowResult," +
            "EnemyCount,ShooterCount,ChaserCount,CoverCount," +
            "CompletionTime,HealthStart,HealthEnd,HealthLost," +
            "ShotsFired,ShotsHit,AccuracyPercent,EnemiesKilled,AverageEnemyTTK";

        File.WriteAllText(filePath, header + "\n");

        Debug.Log("CSVLogger created file: " + filePath);
    }

    public void LogSectionResult(SectionMetrics metrics, DifficultyAnalysisResult analysis)
    {
        if (metrics == null)
        {
            Debug.LogWarning("CSVLogger received null SectionMetrics.");
            return;
        }

        if (analysis == null)
        {
            Debug.LogWarning("CSVLogger received null DifficultyAnalysisResult.");
            return;
        }

        EnsureFileExistsWithHeader();

        StringBuilder row = new StringBuilder();

        row.Append(metrics.sectionIndex).Append(',');
        row.Append(analysis.difficultyBefore).Append(',');
        row.Append(analysis.difficultyAfter).Append(',');
        row.Append(analysis.flowScore).Append(',');
        row.Append(SanitizeCsvText(analysis.flowResult)).Append(',');

        row.Append(metrics.enemiesSpawned).Append(',');
        row.Append(metrics.shooterCount).Append(',');
        row.Append(metrics.chaserCount).Append(',');
        row.Append(metrics.coverCount).Append(',');

        row.Append(metrics.completionTime.ToString("F2")).Append(',');
        row.Append(metrics.playerHealthAtStart.ToString("F0")).Append(',');
        row.Append(metrics.playerHealthAtEnd.ToString("F0")).Append(',');
        row.Append(metrics.playerHealthLost.ToString("F0")).Append(',');

        row.Append(metrics.shotsFired).Append(',');
        row.Append(metrics.shotsHit).Append(',');
        row.Append(metrics.accuracyPercent.ToString("F1")).Append(',');
        row.Append(metrics.enemiesKilled).Append(',');
        row.Append(metrics.averageEnemyTimeToKill.ToString("F2"));

        File.AppendAllText(filePath, row + "\n");
    }

    private string SanitizeCsvText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value.Replace(",", " ");
    }
}
