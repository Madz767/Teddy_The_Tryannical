using UnityEngine;

//==========================================
//           PortalDestination.cs
//==========================================
//what this handles:
// - Marks a Transform as a named spawn/arrival point for portals in a scene.
// - Provides an id (defaults to GameObject name) used to locate where the player should appear.
//why this is separate:
// - Keeps spawn point metadata separate from portal/manager logic so designers can place and name destinations in scenes.
//what this interacts with:
// - Portal (reads destinationId to move the player).
// - PlayerSpawner or Portal placement logic (searches for PortalDestination.id on scene load).
// - Scene view (draws gizmo / label to aid level editing).


[DisallowMultipleComponent]
public class PortalDestination : MonoBehaviour
{
    [Tooltip("Unique id used by Portal.destinationId to locate this spawn point.")]
    public string id;

    private void Reset()
    {
        // When the component is first added in the editor, default the id to the GameObject name.
        if (string.IsNullOrEmpty(id))
            id = gameObject.name;
    }

    private void OnValidate()
    {
        // Keep the id meaningful in the inspector; default to the GameObject name when empty.
        if (string.IsNullOrEmpty(id))
            id = gameObject.name;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);

        #if UNITY_EDITOR
        // Editor-only label so you can see the id in the scene view.
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, $"PortalDest: {id}");
        #endif
    }
}