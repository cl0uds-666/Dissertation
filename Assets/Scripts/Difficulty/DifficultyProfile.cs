using UnityEngine;

/// <summary>
/// Stores the actual gameplay values used to generate a section.
/// 
/// The DifficultyManager creates one of these profiles based on the current
/// difficulty score. The SectionGenerator then uses this profile when spawning
/// cover and enemies.
/// </summary>
[System.Serializable]
public class DifficultyProfile
{
    [Header("Difficulty")]
    public int difficultyScore;

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

    [Header("Cover Settings")]
    public int coverCount;
    public float coverMinSize;
    public float coverMaxSize;
    public float coverMinHeight;
    public float coverMaxHeight;

    public DifficultyProfile(
        int newDifficultyScore,

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

        int newCoverCount,
        float newCoverMinSize,
        float newCoverMaxSize,
        float newCoverMinHeight,
        float newCoverMaxHeight
    )
    {
        difficultyScore = newDifficultyScore;

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

        coverCount = newCoverCount;
        coverMinSize = newCoverMinSize;
        coverMaxSize = newCoverMaxSize;
        coverMinHeight = newCoverMinHeight;
        coverMaxHeight = newCoverMaxHeight;
    }
}