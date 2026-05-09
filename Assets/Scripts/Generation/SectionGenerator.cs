using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;

/// <summary>
/// Generates endless combat sections using simple primitive prefabs.
/// 
/// Each section contains:
/// - one floor
/// - four boundary walls
/// - random cover cubes
/// - an invisible end trigger
/// 
/// When the player reaches the end trigger, the next section is spawned ahead.
/// Later, this generator will use values from the adaptive difficulty system.
/// </summary>
public class SectionGenerator : MonoBehaviour
{
    private enum GeneratedEnemyRole
    {
        StaticShooter,
        Patrol,
        Chaser
    }

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject coverPrefab;

    public GameObject enemyPrefab;

    [Header("References")]
    public Transform player;
    public PlayerHealth playerHealth;
    public PlayerShooter playerShooter;
    public DifficultyManager difficultyManager;
    public CSVLogger csvLogger;

    [Header("Section Size")]
    public float sectionWidth = 20f;
    public float sectionLength = 30f;

    [Header("Cover Settings")]
    public int coverCount = 8;

    public Vector2 coverSizeXRange = new Vector2(1.5f, 4f);
    public Vector2 coverSizeZRange = new Vector2(1.5f, 4f);
    public Vector2 coverHeightRange = new Vector2(1f, 2f);

    [Header("Side Cover Lane Generation")]
    public float sideCoverLaneOffset = 1.8f;
    public float sideCoverSegmentWidth = 1.0f;

    [Header("Enemy Settings")]
    public int enemyCount = 3;
    public float enemyMoveSpeed = 3f;
    public float enemySpawnClearRadius = 6f;

    [Tooltip("How much empty space is required around an enemy spawn point.")]
    public float enemySpawnCheckRadius = 1.2f;

    [Tooltip("Which layers should block enemy spawning.")]
    public LayerMask enemySpawnBlockingLayers = ~0;

    [Header("Spawn Safety")]
    public float playerSpawnClearRadius = 4f;
    public float edgePadding = 3f;

    [Header("Infinite Generation")]
    public int sectionsToSpawnAtStart = 1;
    public int maxSectionsKept = 4;

    private SectionInstance currentActiveSection;
    private int nextSectionIndex = 0;
    private DifficultyProfile currentProfile;

    private void Awake()
    {
        if (player != null)
        {
            if (playerHealth == null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }

            if (playerShooter == null)
            {
                playerShooter = player.GetComponent<PlayerShooter>();
            }
        }

        if (difficultyManager == null)
        {
            difficultyManager = GetComponent<DifficultyManager>();
        }

        if (csvLogger == null)
        {
            csvLogger = FindAnyObjectByType<CSVLogger>();
        }
    }

    private void Start()
    {
        for (int i = 0; i < sectionsToSpawnAtStart; i++)
        {
            SpawnNextSection();
        }
    }

    /// <summary>
    /// Spawns the next section in front of the previous one.
    /// 
    /// Example:
    /// Section 0 = Z 0
    /// Section 1 = Z 30
    /// Section 2 = Z 60
    /// </summary>
    public void SpawnNextSection()
    {
        if (difficultyManager != null)
        {
            currentProfile = difficultyManager.GetCurrentProfile();
        }
        else
        {
            currentProfile = new DifficultyProfile
            (
                3,
                enemyCount,
                50f,
                enemyMoveSpeed,
                false,
                true,
                7f,
                2f,
                30f,
                0.12f,
                0.35f,
                0.35f,
                coverCount,
                coverSizeXRange.x,
                coverSizeXRange.y,
                coverHeightRange.x,
                coverHeightRange.y,
                true,
                0.7f,
                1.0f,
                2.5f,
                1.4f,
                2.4f
            );
        }

        Vector3 sectionOrigin = new Vector3(
            0f,
            0f,
            nextSectionIndex * sectionLength
        );

        GenerateSection(sectionOrigin, nextSectionIndex);

        nextSectionIndex++;

        CleanupOldSections();
    }

    private void GenerateSection(Vector3 sectionOrigin, int sectionIndex)
    {
        GameObject sectionParent = new GameObject("Generated Section " + sectionIndex);
        sectionParent.transform.position = sectionOrigin;
        sectionParent.transform.parent = transform;

        SectionInstance sectionInstance = sectionParent.AddComponent<SectionInstance>();

        GenerateFloor(sectionOrigin, sectionParent.transform);
        GenerateWalls(sectionOrigin, sectionParent.transform);
        GenerateCover(sectionOrigin, sectionParent.transform);

        NavMeshSurface navMeshSurface = sectionParent.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navMeshSurface.BuildNavMesh();

        List<Vector3> patrolPoints = GeneratePatrolPoints(sectionOrigin);

        int shooterCount = 0;
        int chaserCount = 0;

        int spawnedEnemies = GenerateEnemies(sectionOrigin, sectionParent.transform, sectionInstance, patrolPoints, out shooterCount, out chaserCount);

        int generatedCoverCount = currentProfile != null ? currentProfile.coverCount : coverCount;

        sectionInstance.Setup(sectionIndex, spawnedEnemies, generatedCoverCount, shooterCount, chaserCount, playerHealth, difficultyManager, csvLogger);

        GenerateEndTrigger(sectionOrigin, sectionParent.transform, sectionInstance);

        // For now, the newly spawned section becomes the active metrics target.
        // This is fine because only one uncleared section exists at a time.
        if (playerShooter != null)
        {
            playerShooter.SetCurrentSection(sectionInstance);
        }

        currentActiveSection = sectionInstance;
    }

    private void GenerateFloor(Vector3 sectionOrigin, Transform parent)
    {
        GameObject floor = Instantiate(floorPrefab, sectionOrigin, Quaternion.identity, parent);

        floor.name = "Generated Floor";

        floor.transform.localScale = new Vector3(sectionWidth, 0.2f, sectionLength);

        floor.transform.position = new Vector3(
            sectionOrigin.x,
            -0.1f,
            sectionOrigin.z
        );
    }

    private void GenerateWalls(Vector3 sectionOrigin, Transform parent)
    {
        float halfWidth = sectionWidth / 2f;
        float halfLength = sectionLength / 2f;

        //CreateWall(
        //    "Back Wall",
        //    new Vector3(sectionOrigin.x, 1f, sectionOrigin.z - halfLength),
        //    new Vector3(sectionWidth, 2f, 1f),
        //    parent
        //);

        //CreateWall(
        //    "Front Wall",
        //    new Vector3(sectionOrigin.x, 1f, sectionOrigin.z + halfLength),
        //    new Vector3(sectionWidth, 2f, 1f),
        //    parent
        //);

        CreateWall(
            "Left Wall",
            new Vector3(sectionOrigin.x - halfWidth, 1f, sectionOrigin.z),
            new Vector3(1f, 2f, sectionLength),
            parent
        );

        CreateWall(
            "Right Wall",
            new Vector3(sectionOrigin.x + halfWidth, 1f, sectionOrigin.z),
            new Vector3(1f, 2f, sectionLength),
            parent
        );
    }

    private void CreateWall(string wallName, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, parent);

        wall.name = wallName;
        wall.transform.localScale = scale;
    }

    private void GenerateCover(Vector3 sectionOrigin, Transform parent)
    {
        int generatedCoverCount = currentProfile != null ? currentProfile.coverCount : coverCount;

        int spawned = 0;
        spawned += GenerateSideCoverLanes(sectionOrigin, parent);

        int entranceCount = Mathf.Max(1, Mathf.RoundToInt(generatedCoverCount * 0.3f));
        int midCount = Mathf.Max(1, Mathf.RoundToInt(generatedCoverCount * 0.35f));
        int sideCount = Mathf.Max(1, generatedCoverCount - entranceCount - midCount);

        spawned += SpawnCoverInZone(sectionOrigin, parent, entranceCount, "Entrance");
        spawned += SpawnCoverInZone(sectionOrigin, parent, midCount, "Mid");
        spawned += SpawnCoverInZone(sectionOrigin, parent, sideCount, "Side");

        while (spawned < generatedCoverCount)
        {
            if (TrySpawnCover(sectionOrigin, parent, "Mid"))
            {
                spawned++;
            }
            else
            {
                break;
            }
        }
    }


    private int GenerateSideCoverLanes(Vector3 sectionOrigin, Transform parent)
    {
        if (currentProfile == null || !currentProfile.sideCoverEnabled)
        {
            return 0;
        }

        int spawned = 0;
        spawned += GenerateSingleSideLane(sectionOrigin, parent, true);
        spawned += GenerateSingleSideLane(sectionOrigin, parent, false);
        return spawned;
    }

    private int GenerateSingleSideLane(Vector3 sectionOrigin, Transform parent, bool isLeft)
    {
        float halfWidth = sectionWidth / 2f;
        float halfLength = sectionLength / 2f;

        float laneX = isLeft
            ? sectionOrigin.x - halfWidth + edgePadding + sideCoverLaneOffset
            : sectionOrigin.x + halfWidth - edgePadding - sideCoverLaneOffset;

        float currentZ = sectionOrigin.z - halfLength + 3f;
        float zEnd = sectionOrigin.z + halfLength - 3f;

        int spawned = 0;

        while (currentZ < zEnd)
        {
            float segmentLength = Mathf.Max(0.5f, currentProfile.sideCoverSegmentLength);
            bool placeSegment = Random.value <= currentProfile.sideCoverContinuity;

            if (placeSegment)
            {
                Vector3 center = new Vector3(laneX, currentProfile.sideCoverHeight * 0.5f, currentZ + segmentLength * 0.5f);

                if (!Physics.CheckBox(center, new Vector3(sideCoverSegmentWidth * 0.5f, currentProfile.sideCoverHeight * 0.5f, segmentLength * 0.5f)))
                {
                    GameObject cover = Instantiate(coverPrefab, center, Quaternion.identity, parent);
                    cover.name = isLeft ? "Side Lane Cover Left" : "Side Lane Cover Right";
                    cover.transform.localScale = new Vector3(sideCoverSegmentWidth, currentProfile.sideCoverHeight, segmentLength);
                    ExcludeCoverFromNavMesh(cover);
                    spawned++;
                }
            }

            float gap = Random.Range(currentProfile.sideCoverGapMin, currentProfile.sideCoverGapMax);
            currentZ += segmentLength + gap;
        }

        return spawned;
    }
    private int SpawnCoverInZone(Vector3 sectionOrigin, Transform parent, int count, string zoneName)
    {
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            if (TrySpawnCover(sectionOrigin, parent, zoneName))
            {
                spawned++;
            }
        }

        return spawned;
    }

    private bool TrySpawnCover(Vector3 sectionOrigin, Transform parent, string zoneName)
    {
        Vector3 coverPosition;

        if (!TryGetCoverPositionForZone(sectionOrigin, zoneName, out coverPosition))
        {
            return false;
        }

        GameObject cover = Instantiate(coverPrefab, coverPosition, Quaternion.identity, parent);
        cover.name = "Generated Cover";

        float minSize = currentProfile != null ? currentProfile.coverMinSize : coverSizeXRange.x;
        float maxSize = currentProfile != null ? currentProfile.coverMaxSize : coverSizeXRange.y;
        float minHeight = currentProfile != null ? currentProfile.coverMinHeight : coverHeightRange.x;
        float maxHeight = currentProfile != null ? currentProfile.coverMaxHeight : coverHeightRange.y;

        float randomX = Random.Range(minSize, maxSize);
        float randomZ = Random.Range(minSize, maxSize);
        float randomHeight = Random.Range(minHeight, maxHeight);

        cover.transform.localScale = new Vector3(randomX, randomHeight, randomZ);
        cover.transform.position = new Vector3(coverPosition.x, randomHeight / 2f, coverPosition.z);
        ExcludeCoverFromNavMesh(cover);

        return true;
    }


    private void ExcludeCoverFromNavMesh(GameObject coverObject)
    {
        NavMeshModifier navMeshModifier = coverObject.GetComponent<NavMeshModifier>();

        if (navMeshModifier == null)
        {
            navMeshModifier = coverObject.AddComponent<NavMeshModifier>();
        }

        navMeshModifier.ignoreFromBuild = true;

        // Cover should block navigation at runtime so agents route around it.
        NavMeshObstacle navMeshObstacle = coverObject.GetComponent<NavMeshObstacle>();

        if (navMeshObstacle == null)
        {
            navMeshObstacle = coverObject.AddComponent<NavMeshObstacle>();
        }

        navMeshObstacle.carving = true;
        navMeshObstacle.carveOnlyStationary = true;
    }

    private bool TryGetCoverPositionForZone(Vector3 sectionOrigin, string zoneName, out Vector3 position)
    {
        float halfWidth = sectionWidth / 2f;
        float halfLength = sectionLength / 2f;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            float randomX = 0f;
            float randomZ = 0f;

            if (zoneName == "Entrance")
            {
                randomX = Random.Range(sectionOrigin.x - halfWidth + edgePadding + 1f, sectionOrigin.x + halfWidth - edgePadding - 1f);
                randomZ = Random.Range(sectionOrigin.z - halfLength + 4f, sectionOrigin.z - halfLength + 10f);
            }
            else if (zoneName == "Mid")
            {
                randomX = Random.Range(sectionOrigin.x - halfWidth + edgePadding + 1f, sectionOrigin.x + halfWidth - edgePadding - 1f);
                randomZ = Random.Range(sectionOrigin.z - 5f, sectionOrigin.z + 5f);
            }
            else
            {
                bool useLeftSide = Random.value < 0.5f;
                float sideX = useLeftSide ? sectionOrigin.x - halfWidth + edgePadding + 1.5f : sectionOrigin.x + halfWidth - edgePadding - 1.5f;
                randomX = Random.Range(sideX - 1.5f, sideX + 1.5f);
                randomZ = Random.Range(sectionOrigin.z - halfLength + 6f, sectionOrigin.z + halfLength - 6f);
            }

            Vector3 candidate = new Vector3(randomX, 0f, randomZ);

            // Keep a main path mostly open through the center of the section.
            if (Mathf.Abs(candidate.x - sectionOrigin.x) < 1.2f)
            {
                continue;
            }

            bool overlapsExisting = Physics.CheckBox(candidate + Vector3.up * 1f, new Vector3(1.2f, 1.2f, 1.2f));
            if (overlapsExisting)
            {
                continue;
            }

            position = candidate;
            return true;
        }

        position = sectionOrigin;
        return false;
    }

    private void GenerateEndTrigger(Vector3 sectionOrigin, Transform parent, SectionInstance sectionInstance)
    {
        float halfLength = sectionLength / 2f;

        GameObject triggerObject = new GameObject("Section End Trigger");

        triggerObject.transform.parent = parent;

        triggerObject.transform.position = new Vector3(
            sectionOrigin.x,
            1f,
            sectionOrigin.z + halfLength - 2f
        );

        triggerObject.transform.localScale = new Vector3(
            sectionWidth - 2f,
            2f,
            2f
        );

        BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        SectionEndTrigger triggerScript = triggerObject.AddComponent<SectionEndTrigger>();
        triggerScript.Setup(this, sectionInstance);
    }

    private int GenerateEnemies(Vector3 sectionOrigin, Transform parent, SectionInstance sectionInstance, List<Vector3> sectionPatrolPoints, out int shooterCount, out int chaserCount)
    {
        shooterCount = 0;
        chaserCount = 0;

        if (enemyPrefab == null || player == null)
        {
            Debug.LogWarning("Enemy prefab or player reference is missing on SectionGenerator.");
            return 0;
        }

        int spawnedEnemyCount = 0;

        int generatedEnemyCount = currentProfile != null ? currentProfile.enemyCount : enemyCount;

        for (int i = 0; i < generatedEnemyCount; i++)
        {
            GeneratedEnemyRole enemyRole = GetEnemyRoleForSpawn(i, generatedEnemyCount);

            if (enemyRole == GeneratedEnemyRole.StaticShooter || enemyRole == GeneratedEnemyRole.Patrol)
            {
                shooterCount++;
            }
            else
            {
                chaserCount++;
            }

            Vector3 enemyPosition = GetRandomEnemyPosition(sectionOrigin);

            GameObject enemy = Instantiate(enemyPrefab, enemyPosition, Quaternion.identity, parent);

            enemy.name = "Generated " + enemyRole;

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();

            if (enemyAI != null)
            {
                float generatedEnemySpeed = currentProfile != null ? currentProfile.enemyMoveSpeed : enemyMoveSpeed;

                EnemyAI.MovementMode movementMode = EnemyAI.MovementMode.Stationary;

                if (enemyRole == GeneratedEnemyRole.Chaser)
                {
                    movementMode = EnemyAI.MovementMode.ChasePlayer;
                }
                else if (enemyRole == GeneratedEnemyRole.Patrol)
                {
                    movementMode = EnemyAI.MovementMode.Patrol;
                }

                List<Vector3> patrolPointsForEnemy = movementMode == EnemyAI.MovementMode.Patrol ? sectionPatrolPoints : null;

                enemyAI.Setup(player, generatedEnemySpeed, movementMode, patrolPointsForEnemy);

                // Reuse the current difficulty damage value for melee/contact damage.
                if (currentProfile != null)
                {
                    enemyAI.contactDamage = currentProfile.enemyShotDamage;
                }
            }

            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

            if (enemyHealth != null)
            {
                float generatedEnemyHealth = currentProfile != null ? currentProfile.enemyHealth : enemyHealth.maxHealth;

                enemyHealth.Setup(sectionInstance, generatedEnemyHealth);
            }

            EnemyShooter enemyShooter = enemy.GetComponent<EnemyShooter>();

            if (enemyShooter != null)
            {
                bool generatedCanShoot = enemyRole != GeneratedEnemyRole.Chaser;

                float generatedShotDamage = currentProfile != null ? currentProfile.enemyShotDamage : 7f;
                float generatedFireCooldown = currentProfile != null ? currentProfile.enemyFireCooldown : 2f;
                float generatedShotRange = currentProfile != null ? currentProfile.enemyShotRange : 30f;
                float generatedShotRadius = currentProfile != null ? currentProfile.enemyShotRadius : 0.12f;
                float generatedShotSpread = currentProfile != null ? currentProfile.enemyShotSpread : 0.32f;

                float generatedPeekDamageChance = currentProfile != null ? currentProfile.peekDamageChance : 0.5f;

                enemyShooter.Setup(
                    player,
                    generatedShotDamage,
                    generatedFireCooldown,
                    generatedShotRange,
                    generatedShotRadius,
                    generatedShotSpread,
                    generatedCanShoot,
                    generatedPeekDamageChance
                );
            }

            spawnedEnemyCount++;
        }

        return spawnedEnemyCount;
    }

    private GeneratedEnemyRole GetEnemyRoleForSpawn(int enemyIndex, int totalEnemies)
    {
        int difficulty = currentProfile != null ? currentProfile.difficultyScore : 3;

        int chaserCount = 0;

        if (difficulty <= 2)
        {
            chaserCount = 0;
        }
        else if (difficulty <= 4)
        {
            chaserCount = 1;
        }
        else if (difficulty <= 6)
        {
            chaserCount = 1;
        }
        else if (difficulty <= 8)
        {
            chaserCount = 2;
        }
        else
        {
            chaserCount = 3;
        }

        chaserCount = Mathf.Clamp(chaserCount, 0, totalEnemies);

        int patrolCount = Mathf.Clamp(totalEnemies - chaserCount - 1, 0, totalEnemies);

        if (enemyIndex < chaserCount)
        {
            return GeneratedEnemyRole.Chaser;
        }

        if (enemyIndex < chaserCount + patrolCount)
        {
            return GeneratedEnemyRole.Patrol;
        }

        return GeneratedEnemyRole.StaticShooter;
    }

    private Vector3 GetRandomEnemyPosition(Vector3 sectionOrigin)
    {
        float halfWidth = sectionWidth / 2f;
        float halfLength = sectionLength / 2f;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            float randomX = Random.Range(
                sectionOrigin.x - halfWidth + edgePadding,
                sectionOrigin.x + halfWidth - edgePadding
            );

            float randomZ = Random.Range(
                sectionOrigin.z - halfLength + edgePadding,
                sectionOrigin.z + halfLength - edgePadding
            );

            Vector3 possiblePosition = new Vector3(randomX, 2f, randomZ);

            Vector3 sectionEntrance = new Vector3(
                sectionOrigin.x,
                2f,
                sectionOrigin.z - halfLength + 3f
            );

            float distanceFromEntrance = Vector3.Distance(sectionEntrance, possiblePosition);

            if (distanceFromEntrance < enemySpawnClearRadius)
            {
                continue;
            }

            bool spawnBlocked = Physics.CheckSphere(
                possiblePosition,
                enemySpawnCheckRadius,
                enemySpawnBlockingLayers
            );

            if (spawnBlocked)
            {
                continue;
            }

            return possiblePosition;
        }

        Debug.LogWarning("Could not find valid enemy spawn position. Falling back to section centre.");

        return new Vector3(sectionOrigin.x, 2f, sectionOrigin.z);
    }

    private List<Vector3> GeneratePatrolPoints(Vector3 sectionOrigin)
    {
        List<Vector3> points = new List<Vector3>();

        int pointCount = Random.Range(2, 5);

        for (int i = 0; i < pointCount; i++)
        {
            if (TryGetPatrolPoint(sectionOrigin, out Vector3 patrolPoint, i % 2 == 0))
            {
                points.Add(patrolPoint);
            }
        }

        return points;
    }

    private bool TryGetPatrolPoint(Vector3 sectionOrigin, out Vector3 patrolPoint, bool preferSidePoint)
    {
        float halfWidth = sectionWidth / 2f;
        float halfLength = sectionLength / 2f;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            float randomX;

            if (preferSidePoint)
            {
                bool left = Random.value < 0.5f;
                float sideX = left ? sectionOrigin.x - halfWidth + edgePadding + 2f : sectionOrigin.x + halfWidth - edgePadding - 2f;
                randomX = Random.Range(sideX - 1f, sideX + 1f);
            }
            else
            {
                randomX = Random.Range(sectionOrigin.x - halfWidth + edgePadding, sectionOrigin.x + halfWidth - edgePadding);
            }

            float randomZ = Random.Range(sectionOrigin.z - halfLength + edgePadding, sectionOrigin.z + halfLength - edgePadding);

            Vector3 candidate = new Vector3(randomX, 1f, randomZ);

            bool blocked = Physics.CheckSphere(candidate, 0.8f, enemySpawnBlockingLayers);
            if (blocked)
            {
                continue;
            }

            patrolPoint = candidate;
            return true;
        }

        patrolPoint = sectionOrigin + Vector3.up;
        return false;
    }

    private void CleanupOldSections()
    {
        if (transform.childCount <= maxSectionsKept)
        {
            return;
        }

        Transform oldestSection = transform.GetChild(0);

        Destroy(oldestSection.gameObject);
    }

    public SectionInstance GetCurrentActiveSection()
    {
        return currentActiveSection;
    }
}