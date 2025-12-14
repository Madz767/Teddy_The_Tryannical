using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

//==========================================
//           PlayerSpawner.cs
//==========================================
//what this handles:
// - Ensures a Player GameObject exists after a scene loads (instantiates or relocates player).
// - Accepts a pending destination id (set before load) and moves the player to the matching PortalDestination when the scene is ready.
//why this is separate:
// - Centralizes player instantiation/placement so portals and managers don't duplicate spawn logic.
// - Supports both persistent and per‑scene player workflows by controlling instantiation in one place.
//what this interacts with:
// - SceneManager.sceneLoaded (subscribes to handle placement).
// - Player prefab (assigned in inspector) and GameObject tagged "Player".
// - Portal (sets PendingDestinationId before requesting a load).
// - PortalDestination (used to find the spawn transform).


[DisallowMultipleComponent]
public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner Instance { get; private set; }

    [Tooltip("Prefab of the player to instantiate if none exists in the loaded scene.")]
    public GameObject playerPrefab;

    // set by Portal before loading the next scene
    public static string PendingDestinationId { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // DontDestroyOnLoad only works on root GameObjects
        // If this GameObject is not a root, move it to root first
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Call this before LoadScene to specify where the player should appear.
    /// </summary>
    public static void SetPendingDestination(string id)
    {
        PendingDestinationId = id;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If no pending destination set, nothing to do.
        if (string.IsNullOrEmpty(PendingDestinationId))
            return;

        // Try to find an existing player (tagged "Player")
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
        {
            // No player in the scene: instantiate the prefab if assigned
            if (playerPrefab != null)
            {
                playerObj = Instantiate(playerPrefab);
                // ensure tag set
                playerObj.tag = "Player";
            }
            else
            {
                Debug.LogWarning("[PlayerSpawner] playerPrefab not assigned and no Player found in scene.");
                PendingDestinationId = null;
                return;
            }
        }

        // Find destination by PortalDestination component (case-insensitive)
        var destinations = FindObjectsByType<PortalDestination>(FindObjectsSortMode.None);
        foreach (var dest in destinations)
        {
            // Case-insensitive comparison using ToLower()
            if (dest.id.ToLower() == PendingDestinationId.ToLower())
            {
                playerObj.transform.position = dest.transform.position;
                playerObj.transform.rotation = dest.transform.rotation;
                Debug.Log($"[PlayerSpawner] Successfully placed player at destination '{PendingDestinationId}' (position: {dest.transform.position}).");
                PendingDestinationId = null;
                
                // Set global portal cooldown to prevent immediate re-triggering if player is placed inside a portal trigger
                Portal.SetGlobalCooldown(1.5f);
                
                // Notify camera that player has been placed
                Cameramanager.NotifyPlayerPlaced();
                return;
            }
        }

        // Fallback: try to find a GameObject by name (case-insensitive)
        GameObject go = GameObject.Find(PendingDestinationId);
        if (go == null)
        {
            // Try case-insensitive search through all GameObjects
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                // Case-insensitive comparison using ToLower()
                if (obj.name.ToLower() == PendingDestinationId.ToLower())
                {
                    go = obj;
                    break;
                }
            }
        }
        
        if (go != null)
        {
            playerObj.transform.position = go.transform.position;
            playerObj.transform.rotation = go.transform.rotation;
            Debug.Log($"[PlayerSpawner] Placed player at GameObject '{PendingDestinationId}' (position: {go.transform.position}). " +
                     $"NOTE: Consider adding a PortalDestination component to '{go.name}' for better portal system consistency.");
            PendingDestinationId = null;
            
            // Set global portal cooldown to prevent immediate re-triggering if player is placed inside a portal trigger
            Portal.SetGlobalCooldown(1.5f);
            
            // Notify camera that player has been placed
            Cameramanager.NotifyPlayerPlaced();
            return;
        }

        // Log available destinations for debugging
        // Build list of available destination IDs
        string availableIds = "";
        for (int i = 0; i < destinations.Length; i++)
        {
            if (i > 0) availableIds += ", ";
            availableIds += destinations[i].id;
        }
        
        // Build list of potential GameObjects
        var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        System.Collections.Generic.List<string> potentialNames = new System.Collections.Generic.List<string>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Entrance") || obj.name.Contains("Exit") || obj.name.Contains("Portal"))
            {
                // Check if we already added this name
                bool alreadyAdded = false;
                foreach (string name in potentialNames)
                {
                    if (name == obj.name)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (!alreadyAdded)
                {
                    potentialNames.Add(obj.name);
                }
            }
        }
        
        string availableInfo = "";
        if (availableIds.Length > 0)
        {
            availableInfo = "PortalDestinations: " + availableIds;
        }
        if (potentialNames.Count > 0)
        {
            string potentialNamesStr = "";
            for (int i = 0; i < potentialNames.Count; i++)
            {
                if (i > 0) potentialNamesStr += ", ";
                potentialNamesStr += potentialNames[i];
            }
            if (availableInfo.Length > 0) availableInfo += " | ";
            availableInfo += "Potential GameObjects: " + potentialNamesStr;
        }
        
        Debug.LogWarning($"[PlayerSpawner] Destination '{PendingDestinationId}' not found in scene '{scene.name}'. " +
                        $"{(availableInfo.Length > 0 ? availableInfo : "No destinations found.")} " +
                        $"Make sure the destination has a PortalDestination component with matching id, or a GameObject with matching name.");
        PendingDestinationId = null;
    }
}

