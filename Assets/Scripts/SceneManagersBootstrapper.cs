using UnityEngine;
using UnityEngine.SceneManagement;

//==========================================
//           SceneManagersBootstrapper.cs
//==========================================
//what this handles:
// - Ensures critical persistent managers (e.g. SceneTransitionManager) exist at runtime by creating them when the first scene loads.
//why this is separate:
// - Avoids requiring a specific scene to author the manager GameObject; provides safe automatic bootstrap for runtime builds and editor play.
//what this interacts with:
// - SceneTransitionManager (creates/AddComponent if missing).
// - Unity runtime initialization (uses RuntimeInitializeOnLoadMethod with AfterSceneLoad).
// - Managers GameObject (created as DontDestroyOnLoad to persist across scenes).


public static class SceneManagersBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSceneManagers()
    {
        // If a SceneTransitionManager already exists (created in editor or by another bootstrap), nothing to do.
        if (SceneTransitionManager.Instance != null)
            return;

        // Optionally you can restrict automatic creation to the StartMenu scene only:
        // if (SceneManager.GetActiveScene().name != "StartMenu") return;

        // Create a GameObject to host persistent managers
        var go = new GameObject("Managers");
        // Prevent accidental duplication if something later creates the manager again before Awake executes.
        Object.DontDestroyOnLoad(go);

        // Add SceneTransitionManager (its Awake will set Instance and DontDestroyOnLoad)
        go.AddComponent<SceneTransitionManager>();

        Debug.Log("[Bootstrap] Created SceneTransitionManager (Managers GameObject).");
    }
}