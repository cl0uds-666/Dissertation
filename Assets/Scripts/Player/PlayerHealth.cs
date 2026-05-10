using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Regeneration")]
    [Tooltip("If true, player health regenerates after not taking damage for regenDelay seconds.")]
    public bool regenEnabled = true;

    [Tooltip("Seconds after last damage before regeneration can start.")]
    public float regenDelay = 4f;

    [Tooltip("How much health is restored per second during regeneration.")]
    public float regenPerSecond = 5f;

    [Header("Death")]
    [Tooltip("Scene name to load after death.")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Delay before returning to the main menu after death.")]
    public float deathToMenuDelay = 1.5f;

    [Header("Optional Control Scripts To Disable On Death")]
    [Tooltip("Optional direct references to scripts that should be disabled when the player dies.")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnDeath;

    public float CurrentHealth { get; private set; }

    public bool IsDead => isDead;

    private bool isDead;
    private float lastDamageTime;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        lastDamageTime = Time.time;
    }

    private void Update()
    {
        HandleHealthRegen();
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead)
        {
            return;
        }

        CurrentHealth -= damageAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

        // Reset regen timer whenever damage is taken.
        lastDamageTime = Time.time;

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

    private void HandleHealthRegen()
    {
        if (!regenEnabled || isDead)
        {
            return;
        }

        if (CurrentHealth >= maxHealth)
        {
            return;
        }

        if (Time.time < lastDamageTime + regenDelay)
        {
            return;
        }

        // Regenerate gradually and never exceed max health.
        CurrentHealth += regenPerSecond * Time.deltaTime;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
    }

    private void Die()
    {
        // Prevent repeated death logic.
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log("Player died.");

        DisablePlayerControlScripts();

        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    private void DisablePlayerControlScripts()
    {
        // Prefer explicit references from the Inspector.
        if (scriptsToDisableOnDeath != null)
        {
            for (int i = 0; i < scriptsToDisableOnDeath.Length; i++)
            {
                MonoBehaviour script = scriptsToDisableOnDeath[i];
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }

        // Fallback by common script names so setup is low-friction.
        DisableScriptByName("PlayerController");
        DisableScriptByName("PlayerShooter");
        DisableScriptByName("MouseLook");
        DisableScriptByName("CameraLook");
    }

    private void DisableScriptByName(string scriptTypeName)
    {
        Component foundComponent = GetComponent(scriptTypeName);
        if (foundComponent is MonoBehaviour behaviour)
        {
            behaviour.enabled = false;
        }
    }

    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        if (deathToMenuDelay > 0f)
        {
            yield return new WaitForSeconds(deathToMenuDelay);
        }

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("PlayerHealth: mainMenuSceneName is empty. Cannot load main menu.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError(
                "PlayerHealth: Scene '" + mainMenuSceneName + "' is not in Build Settings or is invalid. Cannot load main menu.");
            yield break;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
