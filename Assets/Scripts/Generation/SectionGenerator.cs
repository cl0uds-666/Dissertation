using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class SectionGenerator : MonoBehaviour
{
    private enum GeneratedEnemyRole { CoverShooter, SideShooter, Suppressor, Chaser, Rusher, Blocker }
    public GameObject floorPrefab, wallPrefab, coverPrefab, enemyPrefab;
    public Transform player;
    public PlayerHealth playerHealth; public PlayerShooter playerShooter; public DifficultyManager difficultyManager; public CSVLogger csvLogger;
    public float sectionWidth = 20f, sectionLength = 30f; public int coverCount = 8; public Vector2 coverSizeXRange = new Vector2(1.5f, 4f), coverHeightRange = new Vector2(1f, 2f);
    public int enemyCount = 3; public float edgePadding = 3f; public int sectionsToSpawnAtStart = 1; public int maxSectionsKept = 4;
    private int nextSectionIndex; private DifficultyProfile currentProfile;
    private SectionInstance currentActiveSection;

    void Start(){ for(int i=0;i<sectionsToSpawnAtStart;i++) SpawnNextSection(); }
    public void SpawnNextSection(){ currentProfile = difficultyManager != null ? difficultyManager.GetCurrentProfile() : DifficultyProfile.CreateDefault(); Vector3 o=new Vector3(0,0,nextSectionIndex*sectionLength); GenerateSection(o,nextSectionIndex++); CleanupOldSections(); }

    private void GenerateSection(Vector3 origin,int index)
    {
        GameObject parent = new GameObject("Generated Section " + index); parent.transform.SetParent(transform); parent.transform.position = origin;
        SectionInstance instance = parent.AddComponent<SectionInstance>(); SectionData data = parent.AddComponent<SectionData>();
        GenerateFloor(origin,parent.transform); GenerateWalls(origin,parent.transform); GenerateCover(origin,parent.transform,data); BuildSupportPoints(origin,data);
        var navSurface = parent.AddComponent<NavMeshSurface>(); navSurface.collectObjects = CollectObjects.Children; navSurface.BuildNavMesh();
        int coverShooter=0, side=0, sup=0, chase=0, rush=0, block=0;
        int spawned = GenerateEnemies(origin,parent.transform,instance,data, ref coverShooter,ref side,ref sup,ref chase,ref rush,ref block);
        instance.Setup(index, spawned, currentProfile.coverCount, coverShooter, side, sup, chase, rush, block, playerHealth, difficultyManager, csvLogger);
        GenerateEndTrigger(origin,parent.transform,instance); if (playerShooter!=null) playerShooter.SetCurrentSection(instance);
        currentActiveSection = instance;
    }

    private int GenerateEnemies(Vector3 origin, Transform parent, SectionInstance section, SectionData data, ref int coverShooterCount, ref int sideShooterCount, ref int suppressorCount, ref int chaserCount, ref int rusherCount, ref int blockerCount)
    {
        int total = currentProfile.enemyCount;
        for(int i=0;i<total;i++){
            GeneratedEnemyRole role = GetEnemyRoleForSpawn(i,total);
            Vector3 pos = GetRandomPoint(origin);
            GameObject e = Instantiate(enemyPrefab,pos,Quaternion.identity,parent); e.name = "Generated " + role;
            EnemyAI ai=e.GetComponent<EnemyAI>(); EnemyShooter sh=e.GetComponent<EnemyShooter>(); EnemyHealth hp=e.GetComponent<EnemyHealth>(); EnemyVision v=e.GetComponent<EnemyVision>();
            Vector3[] patrol = data.sidePatrolPoints.Count>=2 ? new[]{data.sidePatrolPoints[0],data.sidePatrolPoints[1]} : null;
            Vector3 target = data.coverPoints.Count>0 ? data.coverPoints[Random.Range(0,data.coverPoints.Count)] : data.blockerControlPoint;
            EnemyAI.EnemyMovementMode mode = EnemyAI.EnemyMovementMode.None; bool useCoverPeek=false;
            switch(role){ case GeneratedEnemyRole.CoverShooter: mode=EnemyAI.EnemyMovementMode.MoveToCover; coverShooterCount++; useCoverPeek=true; break; case GeneratedEnemyRole.SideShooter: mode=EnemyAI.EnemyMovementMode.PatrolSide; sideShooterCount++; break; case GeneratedEnemyRole.Suppressor: mode=EnemyAI.EnemyMovementMode.SuppressAndMove; suppressorCount++; break; case GeneratedEnemyRole.Chaser: mode=EnemyAI.EnemyMovementMode.ChasePlayer; chaserCount++; break; case GeneratedEnemyRole.Rusher: mode=EnemyAI.EnemyMovementMode.RushPlayer; rusherCount++; break; case GeneratedEnemyRole.Blocker: mode=EnemyAI.EnemyMovementMode.BlockPath; blockerCount++; target=data.blockerControlPoint; break; }
            if(ai!=null) ai.Setup(player,mode,currentProfile.enemyPatrolSpeed,currentProfile.enemyChaseSpeed,currentProfile.enemyRushSpeed,patrol,target);
            if(v!=null){ v.player=player; v.visionRange=currentProfile.enemyVisionRange; v.visionAngle=currentProfile.enemyVisionAngle; v.reactionTime=currentProfile.enemyReactionTime; }
            if(sh!=null){ float cd = currentProfile.enemyFireCooldown; if(role==GeneratedEnemyRole.Suppressor) cd*=currentProfile.suppressorFireCooldownMultiplier; sh.Setup(player,currentProfile.enemyShotDamage,cd,currentProfile.enemyShotRange,currentProfile.enemyShotRadius,currentProfile.enemyShotSpread,true,currentProfile.peekDamageChance,currentProfile.enemyHideDuration,currentProfile.enemyPeekDuration,useCoverPeek); }
            if(hp!=null){ float h=currentProfile.enemyHealth; if(role==GeneratedEnemyRole.Rusher) h*=0.7f; if(role==GeneratedEnemyRole.Blocker) h*=1.3f; hp.Setup(section,h); }
        }
        return total;
    }
    private GeneratedEnemyRole GetEnemyRoleForSpawn(int idx,int total){ List<GeneratedEnemyRole> bag=new List<GeneratedEnemyRole>(); int d=currentProfile.difficultyScore; if(d<=2){bag.AddRange(new[]{GeneratedEnemyRole.CoverShooter,GeneratedEnemyRole.CoverShooter,GeneratedEnemyRole.SideShooter});} else if(d<=4){bag.AddRange(new[]{GeneratedEnemyRole.CoverShooter,GeneratedEnemyRole.SideShooter});} else if(d<=6){bag.AddRange(new[]{GeneratedEnemyRole.CoverShooter,GeneratedEnemyRole.SideShooter,GeneratedEnemyRole.Suppressor});} else if(d<=8){bag.AddRange(new[]{GeneratedEnemyRole.Suppressor,GeneratedEnemyRole.Rusher,GeneratedEnemyRole.SideShooter,GeneratedEnemyRole.CoverShooter});} else {bag.AddRange(new[]{GeneratedEnemyRole.Blocker,GeneratedEnemyRole.Rusher,GeneratedEnemyRole.Suppressor,GeneratedEnemyRole.SideShooter});} return bag[idx%bag.Count]; }

    private void BuildSupportPoints(Vector3 o, SectionData d){ float hw=sectionWidth/2f; float hl=sectionLength/2f; d.sidePatrolPoints.Add(new Vector3(o.x-hw+2f,1f,o.z-hl+5f)); d.sidePatrolPoints.Add(new Vector3(o.x-hw+2f,1f,o.z+hl-5f)); d.sidePatrolPoints.Add(new Vector3(o.x+hw-2f,1f,o.z-hl+5f)); d.sidePatrolPoints.Add(new Vector3(o.x+hw-2f,1f,o.z+hl-5f)); d.blockerControlPoint = new Vector3(o.x,1f,o.z+hl-currentProfile.blockerAheadDistance); d.sectionFrontPoint = new Vector3(o.x,1f,o.z+hl-1f); }
    private void GenerateFloor(Vector3 o,Transform p){ var f=Instantiate(floorPrefab,o,Quaternion.identity,p); f.transform.localScale=new Vector3(sectionWidth,0.2f,sectionLength); f.transform.position=new Vector3(o.x,-0.1f,o.z); }
    private void GenerateWalls(Vector3 o,Transform p){ float hw=sectionWidth/2f; Instantiate(wallPrefab,new Vector3(o.x-hw,1f,o.z),Quaternion.identity,p).transform.localScale=new Vector3(1f,2f,sectionLength); Instantiate(wallPrefab,new Vector3(o.x+hw,1f,o.z),Quaternion.identity,p).transform.localScale=new Vector3(1f,2f,sectionLength); }
    private void GenerateCover(Vector3 o,Transform p,SectionData d){ for(int i=0;i<currentProfile.coverCount;i++){ Vector3 pos=GetRandomPoint(o); var c=Instantiate(coverPrefab,pos,Quaternion.identity,p); float s=Random.Range(currentProfile.coverMinSize,currentProfile.coverMaxSize), h=Random.Range(currentProfile.coverMinHeight,currentProfile.coverMaxHeight); c.transform.localScale=new Vector3(s,h,s); c.transform.position=new Vector3(pos.x,h/2f,pos.z); d.coverPoints.Add(new Vector3(pos.x,1f,pos.z)); } }
    private Vector3 GetRandomPoint(Vector3 o){ float hw=sectionWidth/2f; float hl=sectionLength/2f; return new Vector3(Random.Range(o.x-hw+edgePadding,o.x+hw-edgePadding),1f,Random.Range(o.z-hl+edgePadding,o.z+hl-edgePadding)); }
    private void GenerateEndTrigger(Vector3 o,Transform p,SectionInstance i){ var t=new GameObject("Section End Trigger"); t.transform.SetParent(p); t.transform.position=new Vector3(o.x,1f,o.z+sectionLength/2f-2f); t.transform.localScale=new Vector3(sectionWidth-2f,2f,2f); var bc=t.AddComponent<BoxCollider>(); bc.isTrigger=true; t.AddComponent<SectionEndTrigger>().Setup(this,i); }
    private void CleanupOldSections(){ while(transform.childCount>maxSectionsKept) Destroy(transform.GetChild(0).gameObject); }

    public SectionInstance GetCurrentActiveSection()
    {
        return currentActiveSection;
    }
}

