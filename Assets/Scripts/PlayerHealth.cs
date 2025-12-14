using UnityEngine;


//==========================================
//           Player Health
//==========================================
//what this handles: tracking player health, taking damage, death handling
//why this is separate: keeps health logic separate from movement/combat, allows for modularity
//what this interacts with: GameManager (for death), MeleeEnemy, RangedEnemy, ProjectileController
//why this note is here: so you don't get lost in the sauce
//plus, cant let everyone else have fun with notes...


public class PlayerHealth : MonoBehaviour
{
    [Header("Health Stats")]
    public int maxHealth = 100;
    
    private int currentHealth;
    private bool isDead = false;
    
    // Event for health changes (for UI updates)
    public System.Action<int, int> OnHealthChanged;
    
    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void TakeDamage(int amount)
    {
        // Don't take damage if already dead
        if (isDead)
            return;
        
        currentHealth -= amount;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void Die()
    {
        // Prevent multiple death calls
        if (isDead)
            return;
        
        isDead = true;
        
        // Disable player movement and combat
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable player attack controller
        PlayerAttackController attackController = GetComponent<PlayerAttackController>();
        if (attackController != null)
        {
            attackController.enabled = false;
        }
        
        // Disable Rigidbody2D to stop movement
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        
        // Notify GameManager to trigger game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] GameManager.Instance is null! Cannot trigger game over.");
        }
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
    
    /// <summary>
    /// Resets the player's health and death state (useful for respawning)
    /// </summary>
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        // Re-enable components
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        PlayerAttackController attackController = GetComponent<PlayerAttackController>();
        if (attackController != null)
        {
            attackController.enabled = true;
        }
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}

