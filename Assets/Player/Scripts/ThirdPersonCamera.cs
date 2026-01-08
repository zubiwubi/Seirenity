
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    // --------------------------------------------------------------------
    // ★ Public configuration – tweak these in the Inspector ----------
    // --------------------------------------------------------------------
    [Header("Target")]
    public Transform target;                     // assign your player here

    [Header("Orbit")]
    public float sensitivity = 2f;
    public float smoothing = 1.5f;

    [Header("Follow")]
    // X = horizontal offset of the camera (negative = left of player)
    public Vector3 followOffset = new Vector3(-2f, 2f, -4f);
    public float positionSmoothTime = 0.12f;

    [Header("Screen Bias")]
    // X = how far left of the player the camera looks (positive = left)
    public Vector2 screenBias = new Vector2(1f, 0f); // only horizontal bias needed

    [Header("Collision")]
    public LayerMask collisionLayers = ~0;
    public float minDistance = 0.5f;

    // --------------------------------------------------------------------
    // Private state -------------------------------------------------------
    // --------------------------------------------------------------------
    private Vector2 _velocity;
    private Vector2 _frameVelocity;
    private Vector3 _smoothPosVelocity;

    // --------------------------------------------------------------------
    // Unity callbacks ------------------------------------------------------
    // --------------------------------------------------------------------
    private void Awake()
    {
        if (!target) Debug.LogWarning("ThirdPersonCamera: No target assigned.", this);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (!target) return;

        // ----------- 1️⃣ INPUT (unchanged) -----------
        Vector2 mouseDelta = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        );

        Vector2 rawFrameVel = mouseDelta * sensitivity;
        _frameVelocity = Vector2.Lerp(_frameVelocity, rawFrameVel, 1f / smoothing);
        _velocity += _frameVelocity;
        _velocity.y = Mathf.Clamp(_velocity.y, -80f, 80f);

        // ----------- 2️⃣ ROTATION --------------------
        Quaternion camRot = Quaternion.Euler(-_velocity.y, _velocity.x, 0f);

        // ----------- 3️⃣ DESIRED CAMERA POSITION -----
        Vector3 desiredPos = target.position + camRot * followOffset;

        // ----------- 4️⃣ COLLISION (unchanged) -------
        RaycastHit hit;
        Vector3 dir = desiredPos - target.position;
        float dist = dir.magnitude;
        if (Physics.SphereCast(
                target.position,
                0.2f,
                dir.normalized,
                out hit,
                dist,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
        {
            dist = Mathf.Max(hit.distance - minDistance, minDistance);
            desiredPos = target.position + dir.normalized * dist;
        }

        // ----------- 5️⃣ SMOOTH FOLLOW ---------------
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref _smoothPosVelocity,
            positionSmoothTime
        );

        // ----------- 6️⃣ LOOK AT POINT WITH BIAS ------
        // Compute a point a little left of the player (screenBias.x positive = left)
        Vector3 biasWorld = target.right * screenBias.x + target.up * screenBias.y;
        Vector3 lookAtPoint = target.position + biasWorld + Vector3.up * followOffset.y * 0.5f;

        transform.LookAt(lookAtPoint);
    }
}