using System;
using UnityEngine;

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

    private void Reset()
    {
        // Ensure collider is a trigger (2D). This runs in editor when adding the component.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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

        // Pass a callback to place player after scene load completes.
        SceneTransitionManager.Instance.LoadScene(targetScene, onComplete: () =>
        {
            try
            {
                PlacePlayerAtDestination(destinationId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Portal] Exception while placing player at destination: {ex}");
            }
        }, useAsync: true, additive: false);

        // simple cooldown to avoid double-triggering if player arrives instantly
        if (triggerCooldown > 0f)
            StartCoroutine(ResetTriggerAfterDelay(triggerCooldown));
    }

    private System.Collections.IEnumerator ResetTriggerAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        isTriggered = false;
    }

    private void PlacePlayerAtDestination(string id)
    {
        // Find the player in the newly loaded scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            // fallback: try to find PlayerController (active only)
            var pc = FindObjectOfType<PlayerController>(false);
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj == null)
        {
            Debug.LogWarning("[Portal] Player not found after scene load; cannot move player to destination.");
            return;
        }

        // If no destination id provided, leave player where they are (or you could set to portal position)
        if (string.IsNullOrEmpty(id))
            return;

        // Look for PortalDestination components with matching id
        var destinations = FindObjectsOfType<PortalDestination>();
        foreach (var dest in destinations)
        {
            if (string.Equals(dest.id, id, StringComparison.Ordinal))
            {
                playerObj.transform.position = dest.transform.position;
                playerObj.transform.rotation = dest.transform.rotation;
                return;
            }
        }

        // Fallback: try finding a GameObject by name
        var go = GameObject.Find(id);
        if (go != null)
        {
            playerObj.transform.position = go.transform.position;
            playerObj.transform.rotation = go.transform.rotation;
            return;
        }

        Debug.LogWarning($"[Portal] Destination '{id}' not found in scene.");
    }
}