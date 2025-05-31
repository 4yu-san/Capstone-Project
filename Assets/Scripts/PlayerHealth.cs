using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    public float invincibilityTime = 1f;
    
    [Header("UI References")]
    public Image[] healthIcons; // Drag heart/life icons here
    public Text healthText; // Optional text display
    
    [Header("Effects")]
    public GameObject damageEffect; // Flash effect or particle system
    public AudioClip damageSound;
    public AudioClip deathSound;
    
    private int currentHealth;
    private bool isInvincible = false;
    private AudioSource audioSource;
    
    // Events
    public System.Action<int> OnHealthChanged;
    public System.Action OnPlayerDeath;
    
    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        
        UpdateHealthUI();
    }
    
    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Trigger effects
        PlayDamageEffects();
        
        // Start invincibility frames
        StartCoroutine(InvincibilityFrames());
        
        // Update UI
        UpdateHealthUI();
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth);
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    void PlayDamageEffects()
    {
        // Visual effect
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Screen flash effect
        StartCoroutine(DamageFlash());
        
        // Sound effect
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
    }
    
    IEnumerator DamageFlash()
    {
        // Simple screen flash - you can enhance this
        Camera.main.backgroundColor = Color.red;
        yield return new WaitForSeconds(0.1f);
        Camera.main.backgroundColor = Color.black;
    }
    
    IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        
        // Optional: Make player flash/transparent during invincibility
        Renderer playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            float flashDuration = invincibilityTime / 6f;
            for (int i = 0; i < 6; i++)
            {
                playerRenderer.enabled = !playerRenderer.enabled;
                yield return new WaitForSeconds(flashDuration);
            }
            playerRenderer.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(invincibilityTime);
        }
        
        isInvincible = false;
    }
    
    void UpdateHealthUI()
    {
        // Update health icons
        if (healthIcons != null)
        {
            for (int i = 0; i < healthIcons.Length; i++)
            {
                if (healthIcons[i] != null)
                {
                    healthIcons[i].enabled = i < currentHealth;
                }
            }
        }

        // Update health text
        if (healthText != null)
        {
            healthText.text = "Health: " + currentHealth + "/" + maxHealth;
            Debug.Log(currentHealth);
        }
    }
    
    void Die()
    {
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Trigger death event
        OnPlayerDeath?.Invoke();
        
        // You can add death logic here:
        // - Restart level
        // - Show game over screen
        // - Respawn player
        
        Debug.Log("Player died!");
        
        // Example: Restart scene after delay
        StartCoroutine(RestartAfterDelay(2f));
    }
    
    IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Restart current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    // Public getters
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsInvincible() => isInvincible;
    public bool IsAlive() => currentHealth > 0;
    
    // Debug method - remove for production
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(1); // Test damage
        }
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(1); // Test healing
        }
    }
}