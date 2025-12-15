using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

//==========================================
//           Portal
//==========================================
//what this handles:
// - Triggered transitions between scenes when the player enters a portal trigger.
// - Initiates scene load via SceneTransitionManager and (optionally) moves the player to a named destination after load.
//why this is separate:
// - Encapsulates in‑scene trigger behavior and level‑to‑level navigation without coupling scene‑loading logic to player or managers.
//what this interacts with:
// - SceneTransitionManager (requests scene loads).
// - Player (detected by tag or component on trigger enter).
// - PortalDestination (destination lookup after load).
// - PlayerSpawner or direct placement code (to ensure the player exists / is moved).
// - Collider2D (expects a 2D trigger) and optional cooldown logic to avoid double triggers.
//
//SETUP INSTRUCTIONS FOR MULTI-SCENE PORTALS:
// 1. In each scene, create GameObjects at portal entry/exit points
// 2. Add PortalDestination component to each GameObject
// 3. Set the PortalDestination.id to match the destinationId in the Portal component (e.g., "HubWorld_Entrance", "Windowton_Exit")
// 4. In the Portal component, set targetScene to the scene you want to load
// 5. Set destinationId to match a PortalDestination.id in the target scene
// 6. The system will work across all scenes (HubWorld, Windowton, UnderBed, etc.) as long as destinations are set up correctly


[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Header("Portal Target")]
    [SerializeField] private sceneID targetScene;
    [Tooltip("Optional: match this to a PortalDestination.id in the target scene (or a GameObject name).")]
    [SerializeField] private string destinationId;

    [Header("Behavior")]
    [SerializeField] private bool requirePlayerTag = true; // set false if you use a different detection method
    [SerializeField, Min(0f)] private float triggerCooldown = 0.5f;

    private bool isTriggered;
    
    // Static cooldown to prevent portal loops after scene transitions
    // This prevents portals from triggering immediately when player is placed at a destination
    private static float globalCooldownUntil = 0f;
    private const float GLOBAL_COOLDOWN_DURATION = 10f; // Time in seconds to prevent portal triggers after scene load
    
    /// <summary>
    /// Sets a global cooldown for all portals to prevent immediate re-triggering.
    /// Call this after placing the player at a destination to prevent portal loops.
    /// </summary>
    public static void SetGlobalCooldown(float duration)
    {
        globalCooldownUntil = Time.unscaledTime + duration;
    }

    private void Reset()
    {
        // Ensure collider is a trigger (2D). This runs in editor when adding the component.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check global cooldown first (prevents portal loops after scene transitions)
        // Use unscaledTime because Time.time resets when scenes load
        if (Time.unscaledTime < globalCooldownUntil)
        {
            return;
        }
        
        if (isTriggered) return;

        if (requirePlayerTag && !other.CompareTag("Player"))
            return;

        // Optionally could check for a PlayerController component here instead of tag.
        TriggerPortal();
    }

    private void TriggerPortal()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[Portal] No SceneTransitionManager.Instance available. Aborting portal transition.");
            return;
        }

        isTriggered = true;

        // Set the pending destination for PlayerSpawner before loading the scene
        // This ensures the player will be spawned if it doesn't exist in the target scene
        if (!string.IsNullOrEmpty(destinationId))
        {
            if (PlayerSpawner.Instance != null)
            {
                PlayerSpawner.SetPendingDestination(destinationId);
            }
            else
            {
                Debug.LogWarning("[Portal] PlayerSpawner.Instance not found. Player may not spawn correctly.");
            }
        }

        // Store destination ID before scene load (since this Portal will be destroyed)
        string destId = destinationId;
        
        // Set global cooldown to prevent immediate re-triggering when player is placed
        // This will be active when the new scene loads and player is placed at destination
        // Use unscaledTime because Time.time resets when scenes load
        globalCooldownUntil = Time.unscaledTime + GLOBAL_COOLDOWN_DURATION;
        
        // Pass a callback to place player after scene load completes
        // This runs after PlayerSpawner's OnSceneLoaded, so we can verify placement worked
        // Note: We can't use StartCoroutine here because this Portal will be destroyed
        // Instead, we'll use SceneTransitionManager to run the coroutine
        SceneTransitionManager.Instance.LoadScene(targetScene, onComplete: () =>
        {
            // Use SceneTransitionManager (which persists) to run the placement coroutine
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.StartCoroutine(PlacePlayerAfterDelayStatic(destId));
            }
            else
            {
                Debug.LogError("[Portal] SceneTransitionManager.Instance is null after scene load. Cannot place player.");
            }
        }, useAsync: true, additive: false);

        // simple cooldown to avoid double-triggering if player arrives instantly
        // Note: This cooldown is per-portal instance and won't persist across scene loads
        // The global cooldown handles cross-scene protection
        if (triggerCooldown > 0f)
            StartCoroutine(ResetTriggerAfterDelay(triggerCooldown));
    }

    private System.Collections.IEnumerator ResetTriggerAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        isTriggered = false;
    }

    // Static version that can be called after the Portal object is destroyed
    private static System.Collections.IEnumerator PlacePlayerAfterDelayStatic(string id)
    {
        // Wait a frame to ensure PlayerSpawner.OnSceneLoaded has finished
        yield return null;
        
        // Check if PlayerSpawner handled it (PendingDestinationId should be cleared if successful)
        // If it's still set, PlayerSpawner didn't find the destination, so we'll try
        // Also check if player exists and is at a reasonable position
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerObj = pc.gameObject;
        }

        // If player doesn't exist, try to spawn it
        if (playerObj == null && PlayerSpawner.Instance != null && PlayerSpawner.Instance.playerPrefab != null)
        {
            playerObj = Instantiate(PlayerSpawner.Instance.playerPrefab);
            playerObj.tag = "Player";
            Debug.Log("[Portal] Spawned player using PlayerSpawner prefab.");
        }

        // If we still don't have a player, something is wrong
        if (playerObj == null)
        {
            Debug.LogError("[Portal] Could not find or spawn player after scene load!");
            yield break;
        }

        // If no destination ID, we're done
        if (string.IsNullOrEmpty(id))
        {
            Debug.Log("[Portal] No destination ID specified, leaving player at current position.");
            yield break;
        }

        // Check if PlayerSpawner already placed the player correctly
        // If PendingDestinationId is still set, PlayerSpawner didn't find the destination
        bool playerSpawnerHandled = string.IsNullOrEmpty(PlayerSpawner.PendingDestinationId);
        
        if (!playerSpawnerHandled)
        {
            Debug.Log($"[Portal] PlayerSpawner did not handle placement (destination '{id}' not found by PlayerSpawner), attempting fallback placement.");
        }
        else
        {
            // Verify the player is actually at the destination
            // Check both PortalDestinations and GameObjects by name
            bool foundAtDestination = false;
            
            // First check PortalDestinations
            var destinations = FindObjectsByType<PortalDestination>(FindObjectsSortMode.None);
            foreach (var dest in destinations)
            {
                if (dest.id.ToLower() == id.ToLower())
                {
                    float distance = Vector2.Distance(playerObj.transform.position, dest.transform.position);
                    if (distance < 0.5f) // Player is at the destination
                    {
                        foundAtDestination = true;
                        Debug.Log($"[Portal] Player successfully placed at PortalDestination '{id}' by PlayerSpawner.");
                        yield break;
                    }
                }
            }
            
            // Also check GameObjects by name (PlayerSpawner uses this fallback)
            GameObject targetGo = GameObject.Find(id);
            if (targetGo == null)
            {
                // Try case-insensitive search
                var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var obj in allObjects)
                {
                    if (obj.name.ToLower() == id.ToLower())
                    {
                        targetGo = obj;
                        break;
                    }
                }
            }
            
            if (targetGo != null)
            {
                float distance = Vector2.Distance(playerObj.transform.position, targetGo.transform.position);
                if (distance < 0.5f) // Player is at the destination
                {
                    foundAtDestination = true;
                    Debug.Log($"[Portal] Player successfully placed at GameObject '{id}' by PlayerSpawner.");
                    yield break;
                }
            }
            
            if (!foundAtDestination)
            {
                Debug.Log($"[Portal] PlayerSpawner cleared pending destination but player is not at destination '{id}', attempting placement.");
            }
        }

        // Try to place the player at the destination
        PlacePlayerAtDestinationStatic(id);
        
        // Set global cooldown after placing player to prevent immediate re-triggering
        // This ensures portals won't trigger if player is placed inside a portal trigger
        globalCooldownUntil = Time.unscaledTime + GLOBAL_COOLDOWN_DURATION;
        
        // Notify camera that player has been placed
        Cameramanager.NotifyPlayerPlaced();
    }

    private static void PlacePlayerAtDestinationStatic(string id)
    {
        // Find the player in the newly loaded scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            // fallback: try to find PlayerController (active only)
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj == null)
        {
            Debug.LogWarning("[Portal] Player not found after scene load; cannot move player to destination.");
            return;
        }

        // If no destination id provided, leave player where they are
        if (string.IsNullOrEmpty(id))
        {
            Debug.Log("[Portal] No destination ID provided.");
            return;
        }

        // Look for PortalDestination components with matching id (case-insensitive)
        var destinations = FindObjectsByType<PortalDestination>(FindObjectsSortMode.None);
        foreach (var dest in destinations)
        {
            // Case-insensitive comparison using ToLower()
            if (dest.id.ToLower() == id.ToLower())
            {
                playerObj.transform.position = dest.transform.position;
                playerObj.transform.rotation = dest.transform.rotation;
                Debug.Log($"[Portal] Successfully placed player at destination '{id}' (position: {dest.transform.position}).");
                return;
            }
        }

        // Fallback: try finding a GameObject by name (case-insensitive)
        GameObject go = GameObject.Find(id);
        if (go == null)
        {
            // Try case-insensitive search through all GameObjects
            var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                // Case-insensitive comparison using ToLower()
                if (obj.name.ToLower() == id.ToLower())
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
            Debug.Log($"[Portal] Placed player at GameObject '{id}' (position: {go.transform.position}).");
            return;
        }

        // Provide helpful error message with available destinations
        // Build list of available destination IDs
        string availableIds = "";
        for (int i = 0; i < destinations.Length; i++)
        {
            if (i > 0) availableIds += ", ";
            availableIds += destinations[i].id;
        }
        
        // Build list of potential GameObjects
        var sceneObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        System.Collections.Generic.List<string> potentialNames = new System.Collections.Generic.List<string>();
        foreach (var obj in sceneObjects)
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
        
        Debug.LogWarning($"[Portal] Destination '{id}' not found in scene '{SceneManager.GetActiveScene().name}'. " +
                        $"{(availableInfo.Length > 0 ? availableInfo : "No destinations found.")} " +
                        $"Make sure the destination has a PortalDestination component with matching id, or a GameObject with matching name.");
    }
}