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
    
    // Event for health changes (for UI updates)
    public System.Action<int, int> OnHealthChanged;
    
    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void TakeDamage(int amount)
    {
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
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
}

