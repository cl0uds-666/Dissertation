using System;
using System.Globalization;
using System.IO;
using UnityEngine;

public enum DifficultyExperimentMode
{
    Adaptive,
    FixedEasy,
    FixedMedium,
    FixedHard,
    FixedImpossible
}

public static class GameModeSelection
{
    private const string MetricsFileName = "section_metrics_log.csv";
    private const int SessionIdPadding = 3;

    public static DifficultyExperimentMode SelectedMode { get; private set; } = DifficultyExperimentMode.Adaptive;
    public static string SessionId { get; private set; } = "session_unset";

    public static void SetMode(DifficultyExperimentMode mode)
    {
        SelectedMode = mode;
    }

    public static void StartNewSession()
    {
        if (IsSessionIdAssigned())
        {
            Debug.Log("GameModeSelection: Reusing existing session ID for this Play session: " + SessionId);
            return;
        }

        SessionId = GetNextIncrementalSessionId();
        Debug.Log("GameModeSelection: Assigned new incremental session ID: " + SessionId);
    }

    public static bool IsAdaptive()
    {
        return SelectedMode == DifficultyExperimentMode.Adaptive;
    }

    public static float GetInitialDifficultyState()
    {
        switch (SelectedMode)
        {
            case DifficultyExperimentMode.FixedEasy: return -1.0f;
            case DifficultyExperimentMode.FixedMedium: return 0.0f;
            case DifficultyExperimentMode.FixedHard: return 1.2f;
            case DifficultyExperimentMode.FixedImpossible: return 2.4f;
            default: return 0.0f;
        }
    }

    private static bool IsSessionIdAssigned()
    {
        return !string.IsNullOrWhiteSpace(SessionId) && SessionId != "session_unset";
    }

    private static string GetNextIncrementalSessionId()
    {
        string csvPath = Path.Combine(Application.persistentDataPath, MetricsFileName);
        if (!File.Exists(csvPath))
        {
            Debug.Log("GameModeSelection: CSV not found, starting session IDs at 001. Path: " + csvPath);
            return FormatSessionId(1);
        }

        int maxSessionId = 0;
        int validSessionCount = 0;

        foreach (string line in File.ReadLines(csvPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length == 0) continue;

            string sessionColumn = columns[0].Trim();
            if (int.TryParse(sessionColumn, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedId) && parsedId > 0)
            {
                validSessionCount++;
                if (parsedId > maxSessionId) maxSessionId = parsedId;
            }
        }

        if (validSessionCount == 0)
        {
            Debug.Log("GameModeSelection: CSV has no valid numeric session IDs, starting at 001. Path: " + csvPath);
            return FormatSessionId(1);
        }

        return FormatSessionId(maxSessionId + 1);
    }

    private static string FormatSessionId(int sessionNumber)
    {
        return sessionNumber.ToString(sessionNumber >= 1000 ? "0" : "D" + SessionIdPadding, CultureInfo.InvariantCulture);
    }
}
