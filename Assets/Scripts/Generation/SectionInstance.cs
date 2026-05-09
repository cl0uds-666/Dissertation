using UnityEngine;

public class SectionInstance : MonoBehaviour
{
    public int sectionIndex,totalEnemies,enemiesAlive; public bool sectionCleared; public SectionMetrics metrics = new SectionMetrics();
    private PlayerHealth playerHealth; private DifficultyManager difficultyManager; private CSVLogger csvLogger;
    public void Setup(int idx,int enemyCount,int coverCount,int coverShooterCount,int sideShooterCount,int suppressorCount,int chaserCount,int rusherCount,int blockerCount,PlayerHealth player,DifficultyManager diff,CSVLogger logger)
    { difficultyManager=diff; csvLogger=logger; sectionIndex=idx; totalEnemies=enemyCount; enemiesAlive=enemyCount; sectionCleared=enemyCount<=0; playerHealth=player; float hp=playerHealth!=null?playerHealth.GetCurrentHealth():100f; metrics.StartSection(sectionIndex,hp,enemyCount,coverCount,coverShooterCount,sideShooterCount,suppressorCount,chaserCount,rusherCount,blockerCount); }
    public void RegisterShotFired(){ if(!sectionCleared) metrics.RecordShotFired(); } public void RegisterShotHit(){ if(!sectionCleared) metrics.RecordShotHit(); }
    public void RegisterEnemyDeath(float ttk){ enemiesAlive=Mathf.Max(0,enemiesAlive-1); metrics.RecordEnemyKilled(ttk); if(enemiesAlive<=0) CompleteSection(); }
    private void CompleteSection(){ if(sectionCleared)return; sectionCleared=true; float hp=playerHealth!=null?playerHealth.GetCurrentHealth():100f; metrics.EndSection(hp); var result=difficultyManager!=null?difficultyManager.AnalyseSectionPerformance(metrics):null; if(csvLogger!=null&&result!=null) csvLogger.LogSectionResult(metrics,result); }
    public bool CanProgress(){ return sectionCleared; }
}
