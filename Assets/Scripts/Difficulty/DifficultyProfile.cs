using UnityEngine;

/// <summary>
/// Stores the gameplay values used to generate one section.
/// 
/// This profile is generated from the current continuous difficulty state,
/// not selected from fixed level buckets.
/// </summary>
[System.Serializable]
public class DifficultyProfile
{
    [Header("Difficulty State")]
    public float difficultyState;

    [Header("Enemy Movement Settings")]
    public int enemyCount;
    public float enemyHealth;
    public float enemyMoveSpeed;
    public bool enemiesCanMove;

    [Header("Enemy Shooting Settings")]
    public bool enemiesCanShoot;
    public float enemyShotDamage;
    public float enemyFireCooldown;
    public float enemyShotRange;
    public float enemyShotRadius;
    public float enemyShotSpread;
    public float peekDamageChance;

    [Header("Cover Settings")]
    public int coverCount;
    public float coverMinSize;
    public float coverMaxSize;
    public float coverMinHeight;
    public float coverMaxHeight;

    [Header("Side Cover Lane Settings")]
    public bool sideCoverEnabled;
    [Range(0f, 1f)] public float sideCoverContinuity;
    [Range(0f, 1f)] public float sideCoverContinuityCap;
    public float sideCoverHeight;
    public float sideCoverSegmentLength;
    public float sideCoverGapMin;
    public float sideCoverGapMax;

    [Header("Stealth Affordance Settings")]
    public float targetCoverHeightMin;
    public float targetCoverHeightMax;
    [Range(0f, 1f)] public float maxTallCoverFraction;
    public float enemyVisionRangeMultiplier;
    public float enemyVisionAngleMultiplier;

    public DifficultyProfile(
        float newDifficultyState,

        int newEnemyCount,
        float newEnemyHealth,
        float newEnemyMoveSpeed,
        bool newEnemiesCanMove,

        bool newEnemiesCanShoot,
        float newEnemyShotDamage,
        float newEnemyFireCooldown,
        float newEnemyShotRange,
        float newEnemyShotRadius,
        float newEnemyShotSpread,
        float newPeekDamageChance,

        int newCoverCount,
        float newCoverMinSize,
        float newCoverMaxSize,
        float newCoverMinHeight,
        float newCoverMaxHeight,

        bool newSideCoverEnabled,
        float newSideCoverContinuity,
        float newSideCoverContinuityCap,
        float newSideCoverHeight,
        float newSideCoverSegmentLength,
        float newSideCoverGapMin,
        float newSideCoverGapMax,

        float newTargetCoverHeightMin,
        float newTargetCoverHeightMax,
        float newMaxTallCoverFraction,
        float newEnemyVisionRangeMultiplier,
        float newEnemyVisionAngleMultiplier
    )
    {
        difficultyState = newDifficultyState;

        enemyCount = newEnemyCount;
        enemyHealth = newEnemyHealth;
        enemyMoveSpeed = newEnemyMoveSpeed;
        enemiesCanMove = newEnemiesCanMove;

        enemiesCanShoot = newEnemiesCanShoot;
        enemyShotDamage = newEnemyShotDamage;
        enemyFireCooldown = newEnemyFireCooldown;
        enemyShotRange = newEnemyShotRange;
        enemyShotRadius = newEnemyShotRadius;
        enemyShotSpread = newEnemyShotSpread;
        peekDamageChance = newPeekDamageChance;

        coverCount = newCoverCount;
        coverMinSize = newCoverMinSize;
        coverMaxSize = newCoverMaxSize;
        coverMinHeight = newCoverMinHeight;
        coverMaxHeight = newCoverMaxHeight;

        sideCoverEnabled = newSideCoverEnabled;
        sideCoverContinuity = newSideCoverContinuity;
        sideCoverContinuityCap = newSideCoverContinuityCap;
        sideCoverHeight = newSideCoverHeight;
        sideCoverSegmentLength = newSideCoverSegmentLength;
        sideCoverGapMin = newSideCoverGapMin;
        sideCoverGapMax = newSideCoverGapMax;

        targetCoverHeightMin = newTargetCoverHeightMin;
        targetCoverHeightMax = newTargetCoverHeightMax;
        maxTallCoverFraction = newMaxTallCoverFraction;
        enemyVisionRangeMultiplier = newEnemyVisionRangeMultiplier;
        enemyVisionAngleMultiplier = newEnemyVisionAngleMultiplier;
    }
}
