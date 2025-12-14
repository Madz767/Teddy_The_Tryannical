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
            // Try exact match first (case-insensitive)
            if (string.Equals(dest.id, PendingDestinationId, System.StringComparison.OrdinalIgnoreCase))
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
            
            // Try matching with underscores normalized (e.g., "Hub_World_Enter" matches "HubWorld_Enter")
            string normalizedSearchId = PendingDestinationId.Replace("_", "");
            string normalizedDestId = dest.id.Replace("_", "");
            if (string.Equals(normalizedDestId, normalizedSearchId, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[PlayerSpawner] Found destination using normalized match: '{dest.id}' matches search '{PendingDestinationId}'");
                playerObj.transform.position = dest.transform.position;
                playerObj.transform.rotation = dest.transform.rotation;
                Debug.Log($"[PlayerSpawner] Successfully placed player at destination '{dest.id}' (position: {dest.transform.position}).");
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
                // Try exact match
                if (string.Equals(obj.name, PendingDestinationId, System.StringComparison.OrdinalIgnoreCase))
                {
                    go = obj;
                    break;
                }
                
                // Try normalized match (underscores removed)
                string normalizedSearchId = PendingDestinationId.Replace("_", "");
                string normalizedObjName = obj.name.Replace("_", "");
                if (string.Equals(normalizedObjName, normalizedSearchId, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[PlayerSpawner] Found GameObject using normalized match: '{obj.name}' matches search '{PendingDestinationId}'");
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
        var availableIds = destinations.Select(d => d.id).ToArray();
        var availableGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(obj => obj.name.Contains("Entrance") || obj.name.Contains("Exit") || obj.name.Contains("Portal"))
            .Select(obj => obj.name)
            .Distinct()
            .ToArray();
        
        string availableInfo = "";
        if (availableIds.Length > 0)
        {
            availableInfo = $"PortalDestinations: {string.Join(", ", availableIds)}";
        }
        if (availableGameObjects.Length > 0)
        {
            if (availableInfo.Length > 0) availableInfo += " | ";
            availableInfo += $"Potential GameObjects: {string.Join(", ", availableGameObjects)}";
        }
        
        Debug.LogWarning($"[PlayerSpawner] Destination '{PendingDestinationId}' not found in scene '{scene.name}'. " +
                        $"{(availableInfo.Length > 0 ? availableInfo : "No destinations found.")} " +
                        $"Make sure the destination has a PortalDestination component with matching id, or a GameObject with matching name.");
        PendingDestinationId = null;
    }
}

