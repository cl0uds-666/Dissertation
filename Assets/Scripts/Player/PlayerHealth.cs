using UnityEngine;

/// <summary>
/// Tracks player health for the prototype.
/// 
/// Player health is one of the key metrics used to judge how difficult
/// each generated section was.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        CurrentHealth -= damageAmount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

        Debug.Log("Player Health: " + CurrentHealth);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public float GetCurrentHealth()
    {
        return CurrentHealth;
    }

    private void Die()
    {
        Debug.Log("Player died.");

        // Temporary reset behaviour.
        // Later, this will become part of the metrics/adaptation system.
        CurrentHealth = maxHealth;
        transform.position = new Vector3(0f, 1f, 0f);
    }
}