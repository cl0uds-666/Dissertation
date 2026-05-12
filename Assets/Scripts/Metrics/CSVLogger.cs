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
    private bool headerValidated;

    public string GetFilePath() { return filePath; }

    private void Awake()
    {
        CSVLogger[] loggers = FindObjectsByType<CSVLogger>(FindObjectsSortMode.None);

        if (loggers.Length > 1)
        {
            Debug.LogWarning("Multiple CSVLogger instances detected. Destroying duplicate on: " + gameObject.name);
            Destroy(this);
            return;
        }

        filePath = Path.Combine(Application.persistentDataPath, FileName);
        EnsureFileExistsWithHeader();
        ValidateHeaderOnce();
    }

    private void EnsureFileExistsWithHeader()
    {
        if (File.Exists(filePath)) return;

        string header = GetCsvHeader();
        lock (FileWriteLock)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, header + "\n");
                Debug.Log("CSVLogger created file: " + filePath);
            }
        }
    }

    private string GetCsvHeader()
    {
        return "SessionId,SelectedMode,AdaptiveEnabled,SectionIndex,DifficultyStateBefore,DifficultyStateAfter,FlowScore,FlowResult," +
               "EnemyCount,ShooterCount,ChaserCount,CoverCount," +
               "CompletionTime,HealthStart,HealthEnd,HealthLost," +
               "ShotsFired,ShotsHit,AccuracyPercent,EnemiesKilled,AverageEnemyTTK," +
               "TimeDetected,TimeUndetected,TimesDetected,StealthKills,DetectedKills,RunDeterminer";
    }

    private void ValidateHeaderOnce(bool forceMigration = false)
    {
        if (headerValidated && !forceMigration) return;
        MigrateHeaderIfNeeded();
        headerValidated = true;
    }

    private void MigrateHeaderIfNeeded()
    {
        string expectedHeader = GetCsvHeader();
        if (!File.Exists(filePath)) return;

        string[] lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
        {
            File.WriteAllText(filePath, expectedHeader + "\n");
            return;
        }

        if (lines[0] == expectedHeader) return;
        lines[0] = expectedHeader;
        File.WriteAllLines(filePath, lines);
    }

    public void LogSectionResult(SectionMetrics metrics, DifficultyAnalysisResult analysis)
    {
        if (metrics == null || analysis == null)
        {
            Debug.LogWarning("CSVLogger received null data.");
            return;
        }

        EnsureFileExistsWithHeader();

        StringBuilder row = new StringBuilder();
        row.Append(SanitizeCsvText(GameModeSelection.SessionId)).Append(',');
        row.Append(SanitizeCsvText(GameModeSelection.SelectedMode.ToString())).Append(',');
        row.Append(GameModeSelection.IsAdaptive() ? "1" : "0").Append(',');
        row.Append(metrics.sectionIndex).Append(',');
        row.Append(analysis.difficultyStateBefore.ToString("F3")).Append(',');
        row.Append(analysis.difficultyStateAfter.ToString("F3")).Append(',');
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
        row.Append(metrics.averageEnemyTimeToKill.ToString("F2")).Append(',');
        row.Append(metrics.timeDetected.ToString("F2")).Append(',');
        row.Append(metrics.timeUndetected.ToString("F2")).Append(',');
        row.Append(metrics.timesDetected).Append(',');
        row.Append(metrics.stealthKills).Append(',');
        row.Append(metrics.detectedKills).Append(',');
        row.Append(SanitizeCsvText(GetRunDeterminer(metrics)));

        AppendWithRetry(row + "\n");
    }

    private string GetRunDeterminer(SectionMetrics metrics)
    {
        // A run is considered stealth when the player was never detected.
        return metrics.timesDetected <= 0 ? "Stealth" : "Aggressor";
    }

    private void AppendWithRetry(string content)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                lock (FileWriteLock) { File.AppendAllText(filePath, content); }
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
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace(",", " ");
    }

    [ContextMenu("Clear CSV Log")]
    public void ClearCsvLog()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Path.Combine(Application.persistentDataPath, FileName);
        }

        lock (FileWriteLock)
        {
            File.WriteAllText(filePath, GetCsvHeader() + "\n");
        }

        headerValidated = false;
        ValidateHeaderOnce(forceMigration: true);
        Debug.Log("CSVLogger cleared log file: " + filePath);
    }
}
