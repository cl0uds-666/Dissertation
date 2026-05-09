using UnityEngine;

[System.Serializable]
public class DifficultyProfile
{
    public int difficultyScore;
    public int enemyCount;
    public float enemyHealth;
    public float enemyShotDamage;
    public float enemyFireCooldown;
    public float enemyShotRange;
    public float enemyShotRadius;
    public float enemyShotSpread;
    public float peekDamageChance;
    public int coverCount;
    public float coverMinSize, coverMaxSize, coverMinHeight, coverMaxHeight;
    public float enemyVisionRange, enemyVisionAngle, enemyReactionTime;
    public float enemyPatrolSpeed, enemyChaseSpeed, enemyRushSpeed;
    public float enemyPeekDuration, enemyHideDuration;
    public float blockerAheadDistance;
    public float suppressorFireCooldownMultiplier;

    public static DifficultyProfile CreateDefault(){ return new DifficultyProfile{ difficultyScore=3, enemyCount=3, enemyHealth=50f, enemyShotDamage=7f, enemyFireCooldown=1.8f, enemyShotRange=30f, enemyShotRadius=0.12f, enemyShotSpread=0.3f, peekDamageChance=0.35f, coverCount=8, coverMinSize=2f, coverMaxSize=4f, coverMinHeight=1f, coverMaxHeight=2f, enemyVisionRange=18f, enemyVisionAngle=80f, enemyReactionTime=0.3f, enemyPatrolSpeed=2.5f, enemyChaseSpeed=3.5f, enemyRushSpeed=5f, enemyPeekDuration=1.2f, enemyHideDuration=1.6f, blockerAheadDistance=6f, suppressorFireCooldownMultiplier=0.7f}; }
}
