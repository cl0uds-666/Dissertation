using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SectionMetrics
{
    public int sectionIndex,coverCount,coverShooterCount,sideShooterCount,suppressorCount,chaserCount,rusherCount,blockerCount;
    public float sectionStartTime,sectionEndTime,completionTime,playerHealthAtStart,playerHealthAtEnd,playerHealthLost,accuracyPercent,averageEnemyTimeToKill;
    public int shotsFired,shotsHit,enemiesSpawned,enemiesKilled; public List<float> enemyTimeToKillValues = new List<float>();
    public void StartSection(int idx,float hp,int e,int cover,int coverShooter,int side,int sup,int chaser,int rusher,int blocker){ sectionIndex=idx; playerHealthAtStart=hp; playerHealthAtEnd=hp; enemiesSpawned=e; enemiesKilled=0; coverCount=cover; coverShooterCount=coverShooter; sideShooterCount=side; suppressorCount=sup; chaserCount=chaser; rusherCount=rusher; blockerCount=blocker; sectionStartTime=Time.time; shotsFired=0; shotsHit=0; enemyTimeToKillValues.Clear(); }
    public void RecordShotFired(){shotsFired++;RecalculateAccuracy();} public void RecordShotHit(){shotsHit++;RecalculateAccuracy();} public void RecordEnemyKilled(float ttk){enemiesKilled++;enemyTimeToKillValues.Add(ttk);RecalculateAverageTTK();}
    public void EndSection(float hp){ sectionEndTime=Time.time; completionTime=sectionEndTime-sectionStartTime; playerHealthAtEnd=hp; playerHealthLost=playerHealthAtStart-playerHealthAtEnd; RecalculateAccuracy(); RecalculateAverageTTK(); }
    private void RecalculateAccuracy(){ accuracyPercent=shotsFired<=0?0f:((float)shotsHit/shotsFired)*100f; }
    private void RecalculateAverageTTK(){ if(enemyTimeToKillValues.Count==0){averageEnemyTimeToKill=0f;return;} float t=0; foreach(float v in enemyTimeToKillValues)t+=v; averageEnemyTimeToKill=t/enemyTimeToKillValues.Count; }
}
