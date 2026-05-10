# Dissertation

## Project Aim
This project implements and evaluates a **flow-aware adaptive difficulty system** for a Unity-based FPS prototype. The central aim is to determine whether real-time adjustment of encounter parameters (enemy composition, cover density, and combat lethality) can keep players inside a target challenge band while preserving performance and pacing.

## Research Hypothesis
- **Primary hypothesis (H1):** Adaptive difficulty will keep player performance closer to the intended flow zone than static difficulty.
- **Secondary hypothesis (H2):** Adaptive sessions will reduce extreme outcomes (very easy or very hard sections), evidenced by fewer threshold violations and more “Flow Zone” classifications.
- **Null hypothesis (H0):** There will be no measurable difference between adaptive and control conditions in flow classifications or core gameplay metrics.

## System Architecture (Diagram Description)
The architecture can be represented as a closed-loop pipeline:

1. **Section Generation Layer**
   - `SectionGenerator` creates each combat section from a `DifficultyProfile`.
   - Profile values include enemy count, health/mobility/shooting settings, and cover generation parameters.
2. **Gameplay Runtime Layer**
   - Player and enemy systems execute combat.
   - Runtime events (shots, hits, kills, detection state, health changes, completion timing) occur during each section.
3. **Metrics Collection Layer**
   - `SectionMetrics` accumulates section-level telemetry.
   - Metrics are reset at section start and finalized at section end.
4. **Difficulty Analysis Layer**
   - `DifficultyManager.AnalyseSectionPerformance(...)` evaluates metrics against configured flow thresholds.
   - A weighted `flowScore` and categorical result (`Too Easy`, `Flow Zone`, `Too Hard`) are produced.
5. **Adaptation Layer**
   - If adaptive mode is enabled, difficulty score is incremented/decremented within min/max bounds.
   - Updated score selects the next `DifficultyProfile`.
6. **Persistence Layer**
   - `CSVLogger` writes one row per completed section to CSV for offline analysis.

This forms a repeated **measure -> analyse -> adapt -> generate** loop for subsequent sections.

## Adaptation Algorithm Summary
The algorithm is score-based and threshold-driven:

1. Start from current integer difficulty score (`1..10`).
2. For each completed section, evaluate:
   - Health lost
   - Completion time
   - Accuracy
   - Average enemy time-to-kill (TTK)
3. Each metric contributes weighted points to a shared `flowScore`:
   - “Too Easy” pushes score negative.
   - “Too Hard” pushes score positive.
   - “Flow Zone” contributes no shift.
4. Apply global thresholds:
   - If `flowScore <= tooEasyScoreThreshold`: increase difficulty by 1.
   - If `flowScore >= tooHardScoreThreshold`: decrease difficulty by 1.
   - Otherwise: keep difficulty unchanged.
5. Clamp to [`minDifficultyScore`, `maxDifficultyScore`].
6. If adaptive mode is disabled, analysis still runs and logs, but score remains fixed.

## Tracked Metrics (Definitions)
All section-level CSV columns and meanings:

### Identification and Adaptation Outcome
- `SectionIndex`: Sequential section number in the run.
- `DifficultyBefore`: Difficulty score used for the completed section.
- `DifficultyAfter`: Difficulty score selected for the next section (or unchanged in control mode).
- `FlowScore`: Weighted aggregate score from per-metric evaluation.
- `FlowResult`: Overall categorical outcome (`Too Easy`, `Flow Zone`, `Too Hard`).

### Section Composition
- `EnemyCount`: Number of enemies spawned in the section.
- `ShooterCount`: Number of shooter-type enemies.
- `ChaserCount`: Number of chaser-type enemies.
- `CoverCount`: Number of generated cover objects.

### Time and Survivability
- `CompletionTime`: Time in seconds from section start to section end.
- `HealthStart`: Player health at section start.
- `HealthEnd`: Player health at section end.
- `HealthLost`: `HealthStart - HealthEnd`.

### Combat Performance
- `ShotsFired`: Total player shots fired in the section.
- `ShotsHit`: Total player shots that hit.
- `AccuracyPercent`: `(ShotsHit / ShotsFired) * 100` (0 if no shots fired).
- `EnemiesKilled`: Enemies eliminated in the section.
- `AverageEnemyTTK`: Mean time-to-kill across killed enemies.

### Stealth / Detection Behavior
- `TimeDetected`: Total time player was in detected state.
- `TimeUndetected`: Total time player remained undetected.
- `TimesDetected`: Number of detection state entries/events.
- `StealthKills`: Kills registered while not detected.
- `DetectedKills`: Kills registered while detected.

## Experimental Design

### Independent Variables
- **Difficulty condition** (primary IV):
  - `adaptiveDifficultyEnabled = false` (control/static)
  - `adaptiveDifficultyEnabled = true` (adaptive)
- **Optional IVs for extension studies:** initial difficulty score, metric weights, and flow-zone thresholds.

### Dependent Variables
- Primary DVs:
  - Flow outcome frequency (`Too Easy`, `Flow Zone`, `Too Hard`)
  - Absolute/relative difficulty changes over time
- Secondary DVs:
  - Completion time
  - Health lost
  - Accuracy
  - Average enemy TTK
  - Detection metrics and kill-mode split

### Control vs Adaptive Conditions
- **Control condition:** Keep `adaptiveDifficultyEnabled` off. Analysis and logging remain active, but difficulty score does not change between sections.
- **Adaptive condition:** Enable `adaptiveDifficultyEnabled`. The same analyser updates difficulty each section using the weighted flow score.

## Participant / Session Procedure
Recommended study procedure:

1. Assign participant ID and condition order (counterbalanced for within-subject studies).
2. Run a short familiarization segment (not included in analysis).
3. Execute fixed-length gameplay sessions per condition (e.g., N sections or T minutes).
4. Preserve raw CSV output after each session with participant and condition labels.
5. Repeat for second condition if within-subject.
6. Optionally collect post-session subjective ratings (perceived challenge, frustration, enjoyment, flow).

Suggested data hygiene:
- Exclude aborted runs.
- Mark warm-up sections if they should be removed from inferential analysis.
- Keep configuration snapshots (weights/thresholds/initial difficulty) with each dataset.

## Expected Analysis Methods
- **Descriptive statistics:** mean, median, SD for all continuous metrics per condition.
- **Distribution checks:** normality and outlier inspection (especially for TTK and completion time).
- **Inferential comparisons:**
  - Within-subject: paired t-tests or Wilcoxon signed-rank tests.
  - Between-subject: independent t-tests or Mann–Whitney U tests.
- **Categorical outcome analysis:** chi-square or mixed-effects logistic models for flow category frequencies.
- **Time-series / progression analysis:** section-index trends, transition probabilities, and adaptation stability.
- **Effect sizes:** Cohen’s d / rank-biserial / odds ratios as appropriate.

## Reproducibility

### Unity Version
- Unity Editor: `6000.0.58f2`.

### Required Scene(s)
- Main playable scene in build settings:
  - `Assets/Scenes/SampleScene.unity`

### Runtime Steps
1. Open project in Unity `6000.0.58f2`.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Ensure one active `DifficultyManager` and one active `CSVLogger` in scene.
4. Set `adaptiveDifficultyEnabled` according to test condition.
5. Enter Play mode and complete sections.
6. Stop Play mode and retrieve the generated CSV.

### CSV Output Location
- File name: `section_metrics_log.csv`
- Path: `Application.persistentDataPath/section_metrics_log.csv`
- One row is appended per completed section.

## Key Implementation Files
- Adaptive controller: `Assets/Scripts/Difficulty/DifficultyManager.cs`
- Difficulty profile model: `Assets/Scripts/Difficulty/DifficultyProfile.cs`
- Section telemetry model: `Assets/Scripts/Metrics/SectionMetrics.cs`
- CSV persistence: `Assets/Scripts/Metrics/CSVLogger.cs`
- Unity version reference: `ProjectSettings/ProjectVersion.txt`
- Build scene list: `ProjectSettings/EditorBuildSettings.asset`
