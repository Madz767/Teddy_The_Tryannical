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

        // Find destination by PortalDestination component
        var destinations = FindObjectsOfType<PortalDestination>();
        foreach (var dest in destinations)
        {
            if (string.Equals(dest.id, PendingDestinationId, System.StringComparison.Ordinal))
            {
                playerObj.transform.position = dest.transform.position;
                playerObj.transform.rotation = dest.transform.rotation;
                PendingDestinationId = null;
                return;
            }
        }

        // Fallback: try to find a GameObject by name
        var go = GameObject.Find(PendingDestinationId);
        if (go != null)
        {
            playerObj.transform.position = go.transform.position;
            playerObj.transform.rotation = go.transform.rotation;
            PendingDestinationId = null;
            return;
        }

        Debug.LogWarning($"[PlayerSpawner] Destination '{PendingDestinationId}' not found in scene.");
        PendingDestinationId = null;
    }
}

