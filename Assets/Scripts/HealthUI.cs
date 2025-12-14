using UnityEngine;
using TMPro;


//==========================================
//           Health UI
//==========================================
//what this handles: displaying player health on screen
//why this is separate: keeps UI logic separate from gameplay logic, allows for modularity
//what this interacts with: PlayerHealth
//why this note is here: so you don't get lost in the sauce
//plus, UI needs love too


public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI healthText;
    
    [Header("Player Reference")]
    public PlayerHealth playerHealth;
    
    private void Start()
    {
        // Auto-find player health if not assigned
        if (playerHealth == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }
        
        // Subscribe to health changes
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthDisplay;
            UpdateHealthDisplay(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
    
    private void UpdateHealthDisplay(int current, int max)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {current} / {max}";
        }
    }
}

