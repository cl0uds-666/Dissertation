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

    [Header("Section Size")]
    public float sectionWidth = 20f;
    public float sectionLength = 30f;

    [Header("Cover Settings")]
    public int coverCount = 8;

    public Vector2 coverSizeXRange = new Vector2(1.5f, 4f);
    public Vector2 coverSizeZRange = new Vector2(1.5f, 4f);
    public Vector2 coverHeightRange = new Vector2(1f, 2f);

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
                0.32f,
                coverCount,
                coverSizeXRange.x,
                coverSizeXRange.y,
                coverHeightRange.x,
                coverHeightRange.y
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

        int spawnedEnemies = GenerateEnemies(sectionOrigin, sectionParent.transform, sectionInstance);

        sectionInstance.Setup(sectionIndex, spawnedEnemies, playerHealth, difficultyManager);

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

        for (int i = 0; i < generatedCoverCount; i++)
        {
            Vector3 coverPosition = GetRandomCoverPosition(sectionOrigin);

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

            cover.transform.position = new Vector3(
                coverPosition.x,
                randomHeight / 2f,
                coverPosition.z
            );
        }
    }

    private Vector3 GetRandomCoverPosition(Vector3 sectionOrigin)
    {
        float halfWidth = sectionWidth / 2f;
        float halfLength = sectionLength / 2f;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            float randomX = Random.Range(
                sectionOrigin.x - halfWidth + edgePadding,
                sectionOrigin.x + halfWidth - edgePadding
            );

            float randomZ = Random.Range(
                sectionOrigin.z - halfLength + edgePadding,
                sectionOrigin.z + halfLength - edgePadding
            );

            Vector3 possiblePosition = new Vector3(randomX, 0f, randomZ);

            // Keeps cover away from the section entrance.
            Vector3 sectionEntrance = new Vector3(
                sectionOrigin.x,
                0f,
                sectionOrigin.z - halfLength + 3f
            );

            float distanceFromSpawn = Vector3.Distance(sectionEntrance, possiblePosition);

            if (distanceFromSpawn >= playerSpawnClearRadius)
            {
                return possiblePosition;
            }
        }

        return sectionOrigin;
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

    private int GenerateEnemies(Vector3 sectionOrigin, Transform parent, SectionInstance sectionInstance)
    {
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

            Vector3 enemyPosition = GetRandomEnemyPosition(sectionOrigin);

            GameObject enemy = Instantiate(enemyPrefab, enemyPosition, Quaternion.identity, parent);

            enemy.name = "Generated " + enemyRole;

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();

            if (enemyAI != null)
            {
                float generatedEnemySpeed = currentProfile != null ? currentProfile.enemyMoveSpeed : enemyMoveSpeed;

                bool generatedCanMove = enemyRole == GeneratedEnemyRole.Chaser;

                enemyAI.Setup(player, generatedEnemySpeed, generatedCanMove);

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
                bool generatedCanShoot = enemyRole == GeneratedEnemyRole.StaticShooter;

                float generatedShotDamage = currentProfile != null ? currentProfile.enemyShotDamage : 7f;
                float generatedFireCooldown = currentProfile != null ? currentProfile.enemyFireCooldown : 2f;
                float generatedShotRange = currentProfile != null ? currentProfile.enemyShotRange : 30f;
                float generatedShotRadius = currentProfile != null ? currentProfile.enemyShotRadius : 0.12f;
                float generatedShotSpread = currentProfile != null ? currentProfile.enemyShotSpread : 0.32f;

                enemyShooter.Setup(
                    player,
                    generatedShotDamage,
                    generatedFireCooldown,
                    generatedShotRange,
                    generatedShotRadius,
                    generatedShotSpread,
                    generatedCanShoot
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

        if (enemyIndex < chaserCount)
        {
            return GeneratedEnemyRole.Chaser;
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

            Vector3 possiblePosition = new Vector3(randomX, 1f, randomZ);

            Vector3 sectionEntrance = new Vector3(
                sectionOrigin.x,
                1f,
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

        return new Vector3(sectionOrigin.x, 1f, sectionOrigin.z);
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