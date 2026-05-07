using UnityEngine;

/// <summary>
/// Stores enemy health and handles enemy death.
/// 
/// Enemy health can be configured by the current difficulty profile.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 50f;

    private float currentHealth;

    private SectionInstance owningSection;

    private bool isDead;

    private float spawnTime;

    private void Awake()
    {
        currentHealth = maxHealth;
        spawnTime = Time.time;
    }

    /// <summary>
    /// Called by the SectionGenerator after spawning the enemy.
    /// </summary>
    public void Setup(SectionInstance section, float newMaxHealth)
    {
        owningSection = section;

        maxHealth = newMaxHealth;
        currentHealth = maxHealth;

        spawnTime = Time.time;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damageAmount;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        float timeToKill = Time.time - spawnTime;

        if (owningSection != null)
        {
            owningSection.RegisterEnemyDeath(timeToKill);
        }

        Destroy(gameObject);
    }
}