using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Third‑person movement that re‑uses the speed / running logic from the original
/// FirstPersonMovement script, but moves relative to a camera and rotates the
/// character to face the movement direction.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThirdPersonMovement : MonoBehaviour
{
    // --------------------------------------------------------------------
    // ★ Public configuration – tweak these in the Inspector ----------------
    // --------------------------------------------------------------------
    [Header("Basic speeds")]
    public float speed = 5f;               // walk speed
    [Header("Running")]
    public bool canRun = true;
    public float runSpeed = 9f;
    public KeyCode runningKey = KeyCode.LeftShift;

    [Header("Camera")]
    [Tooltip("Camera that defines forward direction for movement.")]
    public Camera followCamera;            // assign in inspector

    [Header("Rotation")]
    [Tooltip("How fast the character turns to face movement direction.")]
    public float rotationSmoothTime = 0.1f;

    [Header("Ground check (optional)")]
    [Tooltip("Layer(s) considered ground for the grounded check.")]
    public LayerMask groundLayers = Physics.DefaultRaycastLayers;
    public float groundCheckDistance = 0.2f; // distance below the collider

    // --------------------------------------------------------------------
    // ★ Runtime state -------------------------------------------------------
    // --------------------------------------------------------------------
    public bool IsRunning { get; private set; }
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    private Rigidbody _rb;
    private Vector3 _velocitySmooth;      // used by SmoothDamp for rotation
    private float _currentYVelocity;      // preserve vertical velocity (gravity)

    // --------------------------------------------------------------------
    // Unity callbacks -------------------------------------------------------
    // --------------------------------------------------------------------
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // If no camera was assigned, fall back to the main camera.
        if (followCamera == null)
            followCamera = Camera.main;
    }

    private void FixedUpdate()
    {
        // ----- 1️⃣ Determine whether we’re running -----
        IsRunning = canRun && Input.GetKey(runningKey);

        // ----- 2️⃣ Resolve the base movement speed (including overrides) -----
        float targetSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
            targetSpeed = speedOverrides[speedOverrides.Count - 1]();

        // ----- 3️⃣ Build input vector (X = horizontal, Z = vertical) -----
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(inputX, 0f, inputZ).normalized; // normalize to prevent diagonal boost

        // If there is no input, we still want gravity to act, so skip the rest.
        if (input.sqrMagnitude < 0.001f)
        {
            // Preserve existing vertical velocity (gravity, jumps, etc.).
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            return;
        }

        // ----- 4️⃣ Convert input to world space relative to the camera -----
        // We only care about the camera’s Y rotation (horizontal plane).
        float camYaw = followCamera.transform.eulerAngles.y;
        Quaternion camRot = Quaternion.Euler(0f, camYaw, 0f);
        Vector3 moveDir = camRot * input; // direction in world space

        // ----- 5️⃣ Apply horizontal velocity -----
        Vector3 horizVelocity = moveDir * targetSpeed;
        // Keep the current Y velocity (gravity / jumping) untouched.
        _rb.linearVelocity = new Vector3(horizVelocity.x, _rb.linearVelocity.y, horizVelocity.z);

        // ----- 6️⃣ Smoothly rotate the character to face movement direction -----
        // Desired forward direction is the same as moveDir (projected on XZ plane).
        Vector3 desiredForward = new Vector3(moveDir.x, 0f, moveDir.z);
        if (desiredForward.sqrMagnitude > 0.001f)
        {
            // Compute the target rotation.
            Quaternion targetRot = Quaternion.LookRotation(desiredForward, Vector3.up);
            // Smoothly interpolate using SmoothDampAngle for a natural feel.
            float smoothY = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetRot.eulerAngles.y,
                ref _currentYVelocity,
                rotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, smoothY, 0f);
        }
    }

    // --------------------------------------------------------------------
    // Optional helper – ground check (useful if you later add jumping) ---
    // --------------------------------------------------------------------
    /// <summary>
    /// Returns true if the character is standing on something considered ground.
    /// </summary>
    public bool IsGrounded()
    {
        // Cast a short ray downward from the centre of the collider.
        // Assumes the object has a CapsuleCollider; adapt if you use a different shape.
        Collider col = GetComponent<Collider>();
        if (col == null) return false;

        Vector3 origin = col.bounds.center;
        float radius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        // Slightly shrink the radius to avoid hitting adjacent walls.
        radius *= 0.9f;

        return Physics.SphereCast(origin, radius, Vector3.down,
                                  out _, groundCheckDistance, groundLayers,
                                  QueryTriggerInteraction.Ignore);
    }
}