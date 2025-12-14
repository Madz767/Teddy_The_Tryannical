using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


//==========================================
//           Game Manager
//==========================================
//what this handles: overall game state management, scene loading, persistent data
//why this is separate: centralizes game state logic, keeps other scripts cleaner
//what this interacts with: PlayerController, UIManager, SceneManager
//
//SETUP INSTRUCTIONS:
// 1. Create a GameObject named "GameManager" (or add to your "Managers" GameObject)
// 2. Add the GameManager component to it
// 3. The GameObject will persist across scenes automatically (DontDestroyOnLoad)
//
//what still needs to be made: inventory system (needs more tweeking)
//and don't forget boss fights... only three don't worry, Tutorial level, underBed level. PlayTown level <--(fianal boss)
//Who put me in charge here... can i sleep now? No? alright then...

//what the plan is next: finish the basic gameplay loop, make sure player can move, attack, collect items
//and sadly, the rest of the entire game... and by that i mean the rest of the game... so yeah... :,)



public enum sceneID
{
    //these are the different scenes in the game
    //used for scene management and loading
    MainMenu,
    Tutorial_Chest,
    HubWorld,
    UnderBed,
    Windowton,
    PlayTown
}



public enum GameState
{
    //these states help us manage the game flow
    //each of these states should encompass all different states of the game
    Playing,
    Paused,
    GameOver
}




public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    

    //this will help us track collected chests across scenes
    //also, using a HashSet helps avoid duplicates
    //as well as provides faster lookups
    public HashSet<string> collectedChests = new HashSet<string>();

    public GameState currentState = GameState.Playing;

    private void Awake()
    {

        //hehe singleton, more like simpleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetGameState(GameState newState)
    {
        currentState = newState;

        switch (newState)
        {

            //so i studied these case statements a bit
            //they are pretty nice, however they will require
            //more studieng to fully comprehend
            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }
        
        // Notify UIManager of state change
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnGameStateChanged(newState);
        }
    }

    public void LoadNextLevel(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void PlayerDied()
    {
        SetGameState(GameState.GameOver);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        
        // Reset game state before loading scene
        currentState = GameState.Playing;
        
        // Find and respawn player if it exists
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Respawn();
            }
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}