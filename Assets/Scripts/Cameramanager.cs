using System.Reflection;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cameramanager : MonoBehaviour
{
    private static Cameramanager instance;
    private CinemachineCamera vcam;
    private Transform playerTransform;
    private bool isSearchingForPlayer = false;
    
    /// <summary>
    /// Call this after placing the player to ensure camera finds it
    /// </summary>
    public static void NotifyPlayerPlaced()
    {
        // Try to find instance if it's null (might not be initialized yet)
        if (instance == null)
        {
            var cameraManager = FindFirstObjectByType<Cameramanager>();
            if (cameraManager != null)
            {
                instance = cameraManager;
                Debug.Log("[Cameramanager] Found Cameramanager instance via FindFirstObjectByType.");
            }
        }
        
        if (instance != null)
        {
            Debug.Log("[Cameramanager] Notified that player has been placed, searching for player...");
            instance.isSearchingForPlayer = false; // Reset flag to allow immediate search
            instance.FindAndAssignPlayer();
        }
        else
        {
            Debug.LogWarning("[Cameramanager] NotifyPlayerPlaced called but Cameramanager instance is null! " +
                           "Cameramanager script may not be attached to CinemachineCamera GameObject. " +
                           "Will retry finding player on next frame.");
            
            // Schedule a delayed retry to find the camera and player
            var cameraManager = FindFirstObjectByType<Cameramanager>();
            if (cameraManager != null)
            {
                cameraManager.StartCoroutine(DelayedNotifyPlayerPlaced(cameraManager));
            }
        }
    }
    
    private static System.Collections.IEnumerator DelayedNotifyPlayerPlaced(Cameramanager cameraManager)
    {
        yield return null; // Wait one frame
        
        if (cameraManager != null)
        {
            Debug.Log("[Cameramanager] Delayed notification: searching for player...");
            cameraManager.isSearchingForPlayer = false;
            cameraManager.FindAndAssignPlayer();
        }
    }
    
    private void Awake()
    {
        // Set instance for static access - always set this instance if null, or if this is the persistent one
        if (instance == null)
        {
            instance = this;
            Debug.Log($"[Cameramanager] Cameramanager instance set on {gameObject.name}");
        }
        else if (instance != this)
        {
            // If there's already an instance, check if this one should replace it
            // Keep the one that's under DontDestroyOnLoad (persistent)
            if (instance.gameObject.scene.name == "DontDestroyOnLoad" || 
                instance.transform.root == instance.transform)
            {
                Debug.LogWarning($"[Cameramanager] Multiple Cameramanager instances detected. Keeping persistent instance on {instance.gameObject.name}, destroying duplicate on {gameObject.name}.");
                Destroy(this);
                return;
            }
            else
            {
                // This one is persistent, replace the instance
                Debug.Log($"[Cameramanager] Replacing Cameramanager instance with persistent one on {gameObject.name}");
                instance = this;
            }
        }
        
        vcam = GetComponent<CinemachineCamera>();
        if (vcam == null)
        {
            Debug.LogError("[Cameramanager] CinemachineCamera component not found on this GameObject!");
        }
        else
        {
            Debug.Log($"[Cameramanager] Cameramanager initialized on {gameObject.name}, CinemachineCamera found.");
            
            // Ensure CinemachineCamera is enabled and properly configured
            if (!vcam.enabled)
            {
                vcam.enabled = true;
                Debug.Log("[Cameramanager] Enabled CinemachineCamera component.");
            }
            
            // Try to set priority if the property exists (higher priority cameras take precedence)
            try
            {
                var priorityProperty = vcam.GetType().GetProperty("Priority");
                if (priorityProperty != null && priorityProperty.CanWrite)
                {
                    priorityProperty.SetValue(vcam, 10); // Set high priority
                    Debug.Log("[Cameramanager] Set CinemachineCamera priority to 10.");
                }
            }
            catch
            {
                // Priority property might not exist, that's okay
            }
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void Start()
    {
        // Ensure camera is set up and active before finding player
        EnsureCinemachineCameraActive();
        FindAndAssignPlayer();
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[Cameramanager] Scene loaded: {scene.name}");
        
        // Ensure Cinemachine camera is active and rendering
        EnsureCinemachineCameraActive();
        
        // Disable any Main Camera in the newly loaded scene to avoid conflicts
        // BUT keep the Camera component that Cinemachine uses for rendering
        DisableSceneMainCamera();
        
        // Reset the searching flag and player transform reference
        isSearchingForPlayer = true;
        playerTransform = null;
        
        // Clear the follow target temporarily to force re-assignment
        if (vcam != null)
        {
            vcam.Follow = null;
        }
        
        // Wait longer to ensure player spawning/placement is complete
        // Portal placement happens after scene load, so we need to wait for that
        StartCoroutine(FindPlayerAfterDelay());
    }
    
    private void EnsureCinemachineCameraActive()
    {
        // Ensure our Cinemachine camera GameObject is active
        if (gameObject != null && !gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            Debug.Log("[Cameramanager] Activated CinemachineCamera GameObject.");
        }
        
        // Ensure CinemachineCamera component is enabled
        if (vcam != null && !vcam.enabled)
        {
            vcam.enabled = true;
            Debug.Log("[Cameramanager] Enabled CinemachineCamera component.");
        }
        
        // Ensure CinemachineBrain exists (needed for Cinemachine to work)
        // CinemachineBrain is usually on the Main Camera
        var brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain == null)
        {
            // Try to add CinemachineBrain to our Camera component
            Camera cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = GetComponentInChildren<Camera>();
            }
            
            if (cam != null)
            {
                brain = cam.gameObject.GetComponent<CinemachineBrain>();
                if (brain == null)
                {
                    brain = cam.gameObject.AddComponent<CinemachineBrain>();
                    Debug.Log("[Cameramanager] Added CinemachineBrain component to Camera.");
                }
            }
            else
            {
                Debug.LogWarning("[Cameramanager] No Camera component found to attach CinemachineBrain to!");
            }
        }
        
        // Check if there's a Camera component that Cinemachine uses for output
        // Cinemachine needs a Camera component to actually render
        Camera outputCamera = GetComponent<Camera>();
        if (outputCamera == null)
        {
            // Check children for Camera component
            outputCamera = GetComponentInChildren<Camera>();
        }
        
        if (outputCamera != null)
        {
            if (!outputCamera.enabled)
            {
                outputCamera.enabled = true;
                Debug.Log("[Cameramanager] Enabled Camera component for Cinemachine output.");
            }
            if (!outputCamera.gameObject.activeInHierarchy)
            {
                outputCamera.gameObject.SetActive(true);
                Debug.Log("[Cameramanager] Activated Camera GameObject for Cinemachine output.");
            }
        }
        else
        {
            // Try to add a Camera component if one doesn't exist
            Debug.LogWarning("[Cameramanager] No Camera component found! Adding Camera component to CinemachineCamera GameObject.");
            outputCamera = gameObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.orthographicSize = 5f;
            outputCamera.tag = "MainCamera";
            outputCamera.depth = -1;
            
            // Also add AudioListener if not present
            if (GetComponent<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
            
            Debug.Log("[Cameramanager] Added Camera component with orthographic settings. Camera should now render.");
        }
    }
    
    private void DisableSceneMainCamera()
    {
        // Find all cameras in the scene
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        
        foreach (Camera cam in cameras)
        {
            // Disable any Main Camera that's not part of our persistent Cinemachine setup
            // Keep cameras that are children of DontDestroyOnLoad objects or part of Cinemachine
            if (cam.CompareTag("MainCamera") && cam.transform.root != transform.root)
            {
                // Don't disable if it's the camera Cinemachine is using
                if (cam != GetComponent<Camera>() && cam != GetComponentInChildren<Camera>())
                {
                    cam.gameObject.SetActive(false);
                    Debug.Log($"[Cameramanager] Disabled scene Main Camera '{cam.name}' to avoid conflicts with Cinemachine.");
                }
            }
        }
    }
    
    private System.Collections.IEnumerator FindPlayerAfterDelay()
    {
        // Wait several frames to ensure player spawning/placement is complete
        // Portal placement happens in a coroutine after scene load
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.1f); // Additional delay for portal placement
        
        FindAndAssignPlayer();
        
        // Continue searching if not found
        if (vcam != null && vcam.Follow == null)
        {
            StartCoroutine(RetryFindPlayer());
        }
        else
        {
            isSearchingForPlayer = false;
        }
    }
    
    private void FindAndAssignPlayer()
    {
        // Ensure vcam is set
        if (vcam == null)
        {
            vcam = GetComponent<CinemachineCamera>();
            if (vcam == null)
            {
                Debug.LogWarning("[Cameramanager] CinemachineCamera component not found.");
                return;
            }
        }
        
        // Ensure GameObject is active
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            Debug.Log("[Cameramanager] Activated Cameramanager GameObject.");
        }
        
        // Try to find player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        // Fallback: try to find PlayerController
        if (player == null)
        {
            var playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.gameObject;
                Debug.Log("[Cameramanager] Found player via PlayerController component.");
            }
        }
        
        if (player != null && player.transform != null)
        {
            playerTransform = player.transform;
            
            // Ensure vcam is valid before assignment
            if (vcam == null)
            {
                Debug.LogError("[Cameramanager] vcam is null, cannot assign follow target!");
                return;
            }
            
            // Set Follow target
            vcam.Follow = playerTransform;
            
            // Also try to set LookAt if the property exists (some Cinemachine setups use LookAt)
            try
            {
                var lookAtProperty = vcam.GetType().GetProperty("LookAt");
                if (lookAtProperty != null && lookAtProperty.CanWrite)
                {
                    lookAtProperty.SetValue(vcam, playerTransform);
                    Debug.Log("[Cameramanager] Also set LookAt target to player.");
                }
            }
            catch
            {
                // LookAt property might not exist, that's okay
            }
            
            // Force update the camera immediately
            if (vcam.enabled)
            {
                vcam.enabled = false;
                vcam.enabled = true;
            }
            
            Debug.Log($"[Cameramanager] Successfully assigned camera follow target to player '{player.name}' at position {playerTransform.position}. " +
                     $"Camera Follow is now: {(vcam.Follow != null ? vcam.Follow.name : "NULL")}, " +
                     $"vcam enabled: {vcam.enabled}, GameObject active: {gameObject.activeInHierarchy}");
            
            // Verify the assignment worked
            if (vcam.Follow == null)
            {
                Debug.LogError("[Cameramanager] WARNING: vcam.Follow is still null after assignment! CinemachineCamera may not be properly initialized.");
            }
            else
            {
                // Double-check after a frame
                StartCoroutine(VerifyFollowTargetAfterFrame());
            }
            
            // Ensure camera is rendering
            EnsureCinemachineCameraActive();
            
            isSearchingForPlayer = false;
        }
        else
        {
            Debug.LogWarning($"[Cameramanager] Player not found. Searched by tag 'Player' and PlayerController component. " +
                           $"Active scene: {SceneManager.GetActiveScene().name}");
            
            // Try again after a short delay if not already searching
            if (!isSearchingForPlayer)
            {
                StartCoroutine(RetryFindPlayer());
            }
        }
    }
    
    private System.Collections.IEnumerator RetryFindPlayer()
    {
        int attempts = 0;
        const int maxAttempts = 10;
        
        while (attempts < maxAttempts && (vcam == null || vcam.Follow == null))
        {
            yield return new WaitForSeconds(0.2f);
            FindAndAssignPlayer();
            attempts++;
            
            if (vcam != null && vcam.Follow != null)
            {
                Debug.Log($"[Cameramanager] Successfully found player after {attempts} attempts.");
                isSearchingForPlayer = false;
                yield break;
            }
        }
        
        if (vcam != null && vcam.Follow == null)
        {
            Debug.LogError($"[Cameramanager] Failed to find player after {maxAttempts} attempts. Camera will not follow.");
            isSearchingForPlayer = false;
        }
    }
    
    private System.Collections.IEnumerator VerifyFollowTargetAfterFrame()
    {
        yield return null; // Wait one frame
        
        if (vcam != null && vcam.Follow == null && playerTransform != null)
        {
            Debug.LogWarning("[Cameramanager] Follow target was cleared! Re-assigning...");
            vcam.Follow = playerTransform;
        }
    }
    
    private void LateUpdate()
    {
        if (vcam == null)
        {
            vcam = GetComponent<CinemachineCamera>();
            if (vcam == null) return;
        }
        
        // Periodically check if player reference is still valid
        // Check more frequently to catch issues early
        if (Time.frameCount % 10 == 0) // Check every 10 frames (~0.17 seconds at 60fps)
        {
            // If follow target is null but we have a player transform, re-assign it
            if (vcam.Follow == null && playerTransform != null)
            {
                Debug.LogWarning("[Cameramanager] Follow target was cleared! Re-assigning immediately...");
                vcam.Follow = playerTransform;
            }
            // If follow target doesn't match our stored reference, update it
            else if (vcam.Follow != playerTransform && playerTransform != null)
            {
                Debug.LogWarning("[Cameramanager] Follow target changed unexpectedly. Re-assigning...");
                vcam.Follow = playerTransform;
            }
            // If player transform is null or destroyed, search for player
            else if (playerTransform == null || vcam.Follow == null)
            {
                // Only search if we're not already searching to avoid spam
                if (!isSearchingForPlayer)
                {
                    Debug.Log("[Cameramanager] Camera follow target is null or invalid, searching for player...");
                    FindAndAssignPlayer();
                }
            }
        }
    }
}
