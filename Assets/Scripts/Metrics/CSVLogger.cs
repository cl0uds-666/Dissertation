using System.IO;
using System.Text;
using System.Threading;
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

    // Shared lock in case multiple logger instances exist.
    private static readonly object FileWriteLock = new object();

    private string filePath;

    private void Awake()
    {
        // Keep only one logger instance to avoid concurrent file writes.
        CSVLogger[] loggers = FindObjectsByType<CSVLogger>(FindObjectsSortMode.None);

        if (loggers.Length > 1)
        {
            Debug.LogWarning("Multiple CSVLogger instances detected. Destroying duplicate on: " + gameObject.name);
            Destroy(this);
            return;
        }

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
            "EnemyCount,CoverShooterCount,SideShooterCount,SuppressorCount,ChaserCount,RusherCount,BlockerCount,CoverCount," +
            "CompletionTime,HealthStart,HealthEnd,HealthLost," +
            "ShotsFired,ShotsHit,AccuracyPercent,EnemiesKilled,AverageEnemyTTK";

        lock (FileWriteLock)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, header + "\n");
                Debug.Log("CSVLogger created file: " + filePath);
            }
        }
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
        row.Append(metrics.coverShooterCount).Append(',');
        row.Append(metrics.sideShooterCount).Append(',');
        row.Append(metrics.suppressorCount).Append(',');
        row.Append(metrics.chaserCount).Append(',');
        row.Append(metrics.rusherCount).Append(',');
        row.Append(metrics.blockerCount).Append(',');
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

        AppendWithRetry(row + "\n");
    }

    private void AppendWithRetry(string content)
    {
        const int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                lock (FileWriteLock)
                {
                    File.AppendAllText(filePath, content);
                }

                return;
            }
            catch (IOException ex)
            {
                Debug.LogWarning("CSVLogger write attempt " + attempt + " failed due to file lock. " + ex.Message);

                if (attempt == maxAttempts)
                {
                    Debug.LogError("CSVLogger failed to write after retries. Path: " + filePath);
                    return;
                }

                Thread.Sleep(20 * attempt);
            }
        }
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
