using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

//==========================================
//           SceneTransitionManager.cs
//==========================================
//what this handles:
// - Centralized scene loading with optional fade in/out, realtime delays, async loading with allowSceneActivation.
// - Persistent singleton that survives scene changes and exposes OnTransitionStart / OnTransitionComplete events.
// - Fallback creation of a persistent CanvasGroup fader if none assigned in inspector.
//why this is separate:
// - Scene loading is global infrastructure; keeping it in a dedicated, persistent manager avoids coupling to scene‑specific objects (like a boss).
// - Provides a single place to handle transitions, progress hooks, and error handling for missing scenes.
//what this interacts with:
// - Scene enum (sceneID) or scene name resolution and Application.CanStreamedLevelBeLoaded validation.
// - CanvasGroup (fade UI) and any UI that listens to transition events.
// - SceneManagersBootstrapper (ensures the manager exists on startup).
// - Callers like Portal, UI, or GameManager that request scene changes.

[DisallowMultipleComponent]
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [SerializeField, Min(0f)] private float transitionDelay = 0.5f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [SerializeField] private CanvasGroup fadeCanvasGroup; // optional: assign a CanvasGroup that covers the screen

    public event Action OnTransitionStart;
    public event Action OnTransitionComplete;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If a CanvasGroup wasn't assigned in the inspector, create a persistent fullscreen fader.
        if (fadeCanvasGroup == null)
            CreatePersistentFader();
        else
        {
            // Ensure initial state
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
            DontDestroyOnLoad(fadeCanvasGroup.gameObject);
        }
    }

    private void CreatePersistentFader()
    {
        var go = new GameObject("SceneTransition_Fader");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var group = go.AddComponent<CanvasGroup>();
        // Optionally add a full-screen Image here if you want a visible background (left empty so projects can style it)
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        fadeCanvasGroup = group;

        DontDestroyOnLoad(go);
    }

    // Keep existing call site signature (sceneID appears to be a project enum)
    public void LoadScene(sceneID scene)
    {
        LoadScene(scene, onComplete: null, useAsync: true, additive: false);
    }

    // Overload with callback and options
    public void LoadScene(sceneID scene, Action onComplete = null, bool useAsync = true, bool additive = false)
    {
        if (IsTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: attempt to start a transition while one is in progress.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(scene, onComplete, useAsync, additive));
    }

    private IEnumerator LoadSceneRoutine(sceneID scene, Action onComplete, bool useAsync, bool additive)
    {
        IsTransitioning = true;
        OnTransitionStart?.Invoke();

        // optional fade out
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // use realtime so timescale doesn't block transitions
        yield return new WaitForSecondsRealtime(transitionDelay);

        string sceneName = scene.ToString();

        // Validate scene exists in build settings
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneTransitionManager: scene '{sceneName}' cannot be loaded (not in Build Settings). Aborting transition.");
            if (fadeCanvasGroup != null)
                yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
            IsTransitioning = false;
            yield break;
        }

        if (!useAsync)
        {
            SceneManager.LoadScene(sceneName, additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }
        else
        {
            var loadMode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, loadMode);
            if (op == null)
            {
                Debug.LogError($"SceneTransitionManager: failed to start async load for scene '{sceneName}'. Aborting transition.");
                if (fadeCanvasGroup != null)
                    yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
                IsTransitioning = false;
                yield break;
            }
            else
            {
                op.allowSceneActivation = false;

                while (op.progress < 0.9f)
                {
                    yield return null;
                }

                // ensure one frame for internal finalization
                yield return null;

                op.allowSceneActivation = true;

                while (!op.isDone)
                    yield return null;
            }
        }

        // optional fade in
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // short realtime delay to ensure scene has initialized
        yield return new WaitForSecondsRealtime(0.01f);

        onComplete?.Invoke();
        OnTransitionComplete?.Invoke();
        IsTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = true;

        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            if (fadeCanvasGroup == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.alpha = to;

        if (Mathf.Approximately(to, 0f))
        {
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }
}