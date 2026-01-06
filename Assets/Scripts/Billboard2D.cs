using UnityEngine;

/// <summary>
/// Makes the attached GameObject (usually a quad or sprite) always face a chosen camera.
/// If no camera is assigned, it defaults to Camera.main.
/// Supports optional Y‑axis locking and a rotation offset.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class Billboard2D : MonoBehaviour
{
    /// <summary>
    /// The camera the billboard should look at.
    /// Leave null to use Camera.main automatically.
    /// </summary>
    [Tooltip("Assign a specific camera for the billboard to face. If left empty, the script uses Camera.main.")]
    public Camera targetCamera;

    /// <summary>
    /// Lock rotation to the Y axis only (useful for top‑down / isometric views).
    /// </summary>
    [Tooltip("Lock rotation to the Y axis only (common for top‑down or isometric games).")]
    public bool lockYAxis = false;

    /// <summary>
    /// Additional rotation offset (in degrees) applied after the billboard calculation.
    /// </summary>
    [Tooltip("Extra rotation offset (degrees) applied after billboard calculation.")]
    public Vector3 rotationOffset = Vector3.zero;

    private Camera _cachedCamera;

    void Awake()
    {
        // Cache the camera we’ll use. If the user supplied one, keep it;
        // otherwise grab Camera.main now (it may change later, handled in LateUpdate).
        _cachedCamera = targetCamera ? targetCamera : Camera.main;
    }

    void LateUpdate()
    {
        // Resolve the camera each frame in case the user clears the reference
        // or the scene’s Main Camera changes at runtime.
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return; // No camera to look at.

        // Store the resolved camera for the next frame (avoids repeated look‑ups).
        _cachedCamera = cam;

        // Direction from the object to the camera.
        Vector3 direction = cam.transform.position - transform.position;

        // Optionally lock rotation around the Y axis.
        if (lockYAxis) direction.y = 0f;

        // Guard against a zero‑length direction vector.
        if (direction.sqrMagnitude < 0.0001f) return;

        // Compute the rotation that faces the camera.
        Quaternion targetRot = Quaternion.LookRotation(direction);

        // Apply any user‑specified offset.
        targetRot *= Quaternion.Euler(rotationOffset);

        // Assign the rotation.
        transform.rotation = targetRot;
    }

#if UNITY_EDITOR
    // Helpful editor validation – warns if you forget to assign a camera.
    private void OnValidate()
    {
        if (targetCamera == null && Camera.main == null)
        {
            Debug.LogWarning($"{nameof(Billboard2D)} on '{gameObject.name}': No camera assigned and no Main Camera found in the scene.", this);
        }
    }
#endif
}