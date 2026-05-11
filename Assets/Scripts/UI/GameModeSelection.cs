using UnityEngine;

public enum DifficultyExperimentMode
{
    Adaptive,
    FixedEasy,
    FixedMedium,
    FixedHard
}

public static class GameModeSelection
{
    public static DifficultyExperimentMode SelectedMode { get; private set; } = DifficultyExperimentMode.Adaptive;
    public static string SessionId { get; private set; } = "session_unset";

    public static void SetMode(DifficultyExperimentMode mode)
    {
        SelectedMode = mode;
    }

    public static void StartNewSession()
    {
        SessionId = System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
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
            default: return 0.0f;
        }
    }
}
