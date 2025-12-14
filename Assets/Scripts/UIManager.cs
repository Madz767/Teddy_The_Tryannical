using UnityEngine;
using UnityEngine.SceneManagement;

//==========================================
//           UI Manager
//==========================================
//what this handles: centralized UI management, panel visibility, pause menu, game state UI coordination
//why this is separate: keeps UI logic centralized and separate from gameplay, allows for easy UI updates
//what this interacts with: GameManager (game state changes), HealthUI (health display), PlayerController (pause input)
//why this note is here: UI management is important, don't get lost in the sauce
//
//SETUP INSTRUCTIONS:
// 1. Create a GameObject named "Managers" (or "UIManager") in your first/main scene
// 2. Add the UIManager component to this GameObject
// 3. In the Inspector, assign your UI panel GameObjects:
//    - HUD Panel (your health UI, etc.)
//    - Pause Menu Panel
//    - Inventory Panel
//    - Settings Panel
//    - Game Over Panel
// 4. The GameObject will persist across scenes automatically (DontDestroyOnLoad)
// 5. Customize the input keys if needed (default: Escape, Tab, F1)

public enum UIPanelType
{
    HUD,           // Main gameplay HUD (health, etc.)
    PauseMenu,     // Pause menu panel
    Inventory,     // Inventory panel
    Settings,      // Settings menu panel
    GameOver       // Game over screen
}

[DisallowMultipleComponent]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("Main HUD panel (health, etc.) - typically always visible during gameplay")]
    [SerializeField] private GameObject hudPanel;
    
    [Tooltip("Pause menu panel")]
    [SerializeField] private GameObject pauseMenuPanel;
    
    [Tooltip("Inventory panel")]
    [SerializeField] private GameObject inventoryPanel;
    
    [Tooltip("Settings menu panel")]
    [SerializeField] private GameObject settingsPanel;
    
    [Tooltip("Game over screen panel")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Settings")]
    [Tooltip("Key to toggle pause menu (default: Escape)")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    
    [Tooltip("Key to toggle inventory (default: Tab)")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.Tab;
    
    [Tooltip("Key to toggle settings (default: F1)")]
    [SerializeField] private KeyCode settingsKey = KeyCode.F1;

    private bool isPaused = false;
    private bool isInventoryOpen = false;
    private bool isSettingsOpen = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Ensure this is a root GameObject for DontDestroyOnLoad
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);

        // Subscribe to game state changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Initialize UI state
        InitializeUI();
        
        // Subscribe to GameManager state changes if available
        if (GameManager.Instance != null)
        {
            // We'll handle game state changes through our own methods
        }
    }

    private void Update()
    {
        // Handle input for UI toggles
        HandleUIInput();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh UI references after scene load
        // This ensures UI panels are found if they're scene-specific
        RefreshUIPanels();
        
        // Initialize UI based on the loaded scene
        InitializeUI();
        
        // Ensure HUD is hidden on MainMenu
        if (scene.name == "MainMenu")
        {
            ShowHUD(false);
        }
    }

    private void HandleUIInput()
    {
        // Don't process input if game is over
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.GameOver)
        {
            return;
        }

        // Pause menu toggle
        if (Input.GetKeyDown(pauseKey))
        {
            if (isInventoryOpen)
            {
                CloseInventory();
            }
            else if (isSettingsOpen)
            {
                CloseSettings();
            }
            else
            {
                TogglePauseMenu();
            }
        }

        // Inventory toggle (only if not paused)
        if (!isPaused && Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }

        // Settings toggle (only if not paused)
        if (!isPaused && Input.GetKeyDown(settingsKey))
        {
            ToggleSettings();
        }
    }

    private void InitializeUI()
    {
        // Get current scene name
        string currentScene = SceneManager.GetActiveScene().name;
        
        // Hide HUD on MainMenu, show it on gameplay scenes
        bool shouldShowHUD = currentScene != "MainMenu";
        ShowHUD(shouldShowHUD);
        
        // Set initial UI state
        ShowPauseMenu(false);
        ShowInventory(false);
        ShowSettings(false);
        ShowGameOver(false);
        
        isPaused = false;
        isInventoryOpen = false;
        isSettingsOpen = false;
    }

    private void RefreshUIPanels()
    {
        // If panels are scene-specific, you can search for them here
        // For now, we'll rely on inspector-assigned references
        // This method can be extended to auto-find panels by tag or name if needed
    }

    #region Panel Visibility Methods

    public void ShowHUD(bool show)
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(show);
        }
    }

    public void ShowPauseMenu(bool show)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(show);
        }
        
        isPaused = show;
        
        // Note: Do NOT call SetGameState here to avoid circular dependency
        // Game state is managed by TogglePauseMenu() or external calls
    }

    public void ShowInventory(bool show)
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(show);
        }
        
        isInventoryOpen = show;
    }

    public void ShowSettings(bool show)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(show);
        }
        
        isSettingsOpen = show;
    }

    public void ShowGameOver(bool show)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(show);
        }
        
        // Hide HUD when game over is shown
        if (show)
        {
            ShowHUD(false);
        }
    }

    #endregion

    #region Toggle Methods

    public void TogglePauseMenu()
    {
        bool newPausedState = !isPaused;
        ShowPauseMenu(newPausedState);
        
        // Update game state after showing/hiding the menu
        // This is safe because ShowPauseMenu no longer calls SetGameState
        if (GameManager.Instance != null)
        {
            if (newPausedState)
            {
                GameManager.Instance.SetGameState(GameState.Paused);
            }
            else
            {
                GameManager.Instance.SetGameState(GameState.Playing);
            }
        }
    }

    public void ToggleInventory()
    {
        ShowInventory(!isInventoryOpen);
    }

    public void ToggleSettings()
    {
        ShowSettings(!isSettingsOpen);
    }

    #endregion

    #region Public Getters

    public bool IsPaused()
    {
        return isPaused;
    }

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    public bool IsSettingsOpen()
    {
        return isSettingsOpen;
    }

    public GameObject GetPanel(UIPanelType panelType)
    {
        return panelType switch
        {
            UIPanelType.HUD => hudPanel,
            UIPanelType.PauseMenu => pauseMenuPanel,
            UIPanelType.Inventory => inventoryPanel,
            UIPanelType.Settings => settingsPanel,
            UIPanelType.GameOver => gameOverPanel,
            _ => null
        };
    }

    #endregion

    #region Button Callbacks (for UI buttons)

    public void OnResumeButtonClicked()
    {
        ShowPauseMenu(false);
        
        // Update game state to resume
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Playing);
        }
    }

    public void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        
        InitializeUI();
    }

    public void OnMainMenuButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Playing);
            GameManager.Instance.LoadNextLevel("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
        
        InitializeUI();
    }

    public void OnQuitButtonClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Starts the game from the main menu - loads the first game scene (Tutorial_Chest)
    /// Call this from your "Start Game" button in the main menu
    /// </summary>
    public void OnStartGameButtonClicked()
    {
        // Set game state to playing
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Playing);
        }
        
        // Use SceneTransitionManager if available (for fade transitions)
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(sceneID.Tutorial_Chest, useAsync: true, additive: false);
        }
        else
        {
            // Fallback to direct scene loading
            SceneManager.LoadScene("Tutorial_Chest");
        }
        
        // Initialize UI state for gameplay
        InitializeUI();
    }

    public void OnCloseInventoryButtonClicked()
    {
        CloseInventory();
    }

    public void OnCloseSettingsButtonClicked()
    {
        CloseSettings();
    }

    private void CloseInventory()
    {
        ShowInventory(false);
    }

    private void CloseSettings()
    {
        ShowSettings(false);
    }

    #endregion

    #region Game State Integration

    /// <summary>
    /// Called by GameManager when game state changes
    /// </summary>
    public void OnGameStateChanged(GameState newState)
    {
        // Get current scene to check if we should show HUD
        string currentScene = SceneManager.GetActiveScene().name;
        bool isMainMenu = currentScene == "MainMenu";
        
        switch (newState)
        {
            case GameState.Playing:
                // Only show HUD if not in MainMenu
                ShowHUD(!isMainMenu);
                ShowPauseMenu(false);
                ShowGameOver(false);
                break;
                
            case GameState.Paused:
                ShowPauseMenu(true);
                break;
                
            case GameState.GameOver:
                ShowHUD(false);
                ShowPauseMenu(false);
                ShowGameOver(true);
                break;
        }
    }

    #endregion
}

