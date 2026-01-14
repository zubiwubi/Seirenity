using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using Unity.Mathematics;

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions _inputSystemActions;
    
    [Header("Very important particle reference")]
    [SerializeField] private ParticleSystem launchParticles;
    
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private bool loop = true;
    [Range(0f, 1f)]
    [SerializeField] private float launchSpeedNormalized = 0.05f;
    [SerializeField] private float playerSmoothTime = 0.5f;
    [SerializeField] private float playerRotationSmoothTime = 0.6f;
    [SerializeField] private float homingDuration = 0.25f;
    [SerializeField] private float launchRampUp = 3.0f;
    [SerializeField] private float launchRampDown = 3.0f;
    [Range(0f, 1f)]
    [SerializeField] private float minLaunchSpeedFactor = 0.02f;

    [Header("Launch")]
    [SerializeField] private float launchDuration = 1.5f;
    [SerializeField] private float launchCooldown = 0.5f;

    private float _t;
    private float _launchTimer;
    private float _cooldownTimer;
    private bool _isLaunching;

    private Rigidbody rb;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2.5f, -6f); 
    [SerializeField] private float cameraSmoothTime = 0.3f;
    [SerializeField] private bool cameraLookAtPlayer = true;
    [SerializeField] private float cameraRotationSmoothTime = 0.6f;

    private Vector3 _cameraVelocity;
    private Vector3 _playerVelocity;
    private bool _isHoming = false;
    private float _homingTimer = 0f;
    
    private bool _isStopping = false;
    private float _stopRampTimer = 0f;
    
    private PlayerLife _playerLife;
    private ColourPickerController _colourPickerController;

    [Header("Debug")]
    [SerializeField] private bool debugHoming = false;
    [SerializeField] private float snapThreshold = 0f;
    
    
    [Header("Spline Offset")]
    [SerializeField] private bool enableSplineOffset = true;
    [SerializeField] private float maxSplineOffset = 0.3f;
    [SerializeField] private float noiseScale = 4f;
    [SerializeField] private float noiseSpeed = 0.5f;
    [SerializeField] private float offsetSmoothness = 8f;
    [SerializeField] private int noiseSeed = 0;

    private Vector3 _currentWorldOffset = Vector3.zero;

    private bool _hasLockedColour = false;

    private float _nextLaunchAllowedAt = 0f;

    private void SetupInput()
    {
        _inputSystemActions = new InputSystem_Actions();
        _inputSystemActions.Player.Enable();
        _inputSystemActions.Player.LaunchPlayer.performed += OnLaunch;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        SetupInput();
        
        if (enableSplineOffset && noiseSeed == 0)
        {
            noiseSeed = UnityEngine.Random.Range(1, 100000);
        }
 
        // subscribe to PlayerLife death to request early stop
        _playerLife = GetComponentInParent<PlayerLife>();
        if (_playerLife != null)
        {
            _playerLife.onDeath += OnPlayerLifeDeath;
        }

        
        _colourPickerController = FindAnyObjectByType<ColourPickerController>();
        if (_colourPickerController == null)
        {
            
            _colourPickerController = null;
        }
        
        if (splineContainer != null && splineContainer.Spline != null)
        {
            var spline = splineContainer.Spline;
            int samples = 256;
            float bestT = 0f;
            float bestSqr = float.MaxValue;
            Vector3 refP = transform.position;
            for (int i = 0; i < samples; i++)
            {
                float tt = i / (float)(samples - 1);
                float3 lp = spline.EvaluatePosition(tt);
                Vector3 worldP = splineContainer.transform.TransformPoint(new Vector3(lp.x, lp.y, lp.z));
                float d = (worldP - refP).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; bestT = tt; }
            }
            _t = bestT;
            
            float sampleT = loop ? Mathf.Repeat(_t, 1f) : Mathf.Clamp01(_t);
            float3 tTan = spline.EvaluateTangent(sampleT);
            Vector3 worldTan = splineContainer.transform.TransformDirection(new Vector3(tTan.x, tTan.y, tTan.z));
            if (worldTan.sqrMagnitude > 0.0001f) transform.rotation = ForwardLookRotation(worldTan);
         }
     }

    private void OnDestroy()
    {
        if (_playerLife != null) _playerLife.onDeath -= OnPlayerLifeDeath;

        if (_inputSystemActions != null)
        {
            _inputSystemActions.Player.LaunchPlayer.performed -= OnLaunch;
            _inputSystemActions.Player.Disable();
            _inputSystemActions.Dispose();
            _inputSystemActions = null;
        }
    }

    private void OnLaunch(InputAction.CallbackContext context)
    {
        
        if (Time.time < _nextLaunchAllowedAt || _isLaunching || _isStopping)
        {
            if (debugHoming)
            {
                if (Time.time < _nextLaunchAllowedAt)
                    Debug.Log($"Launch blocked: cooldown active for {(_nextLaunchAllowedAt - Time.time):F2}s");
                else if (_isLaunching) Debug.Log("Launch blocked: already launching");
                else Debug.Log("Launch blocked: stopping in progress");
            }
            return;
        }

        
        if (_playerLife != null && _playerLife.IsSpawnPending)
        {
            if (debugHoming) Debug.Log("Launch blocked: player spawn pending");
            return;
        }
        
        if (_colourPickerController != null && _colourPickerController.IsOpen)
        {
            if (debugHoming) Debug.Log("Launch blocked: colour picker open");
            return;
        }
        
        if (!CanLaunch())
        {
            if (debugHoming) Debug.Log("Launch blocked: CanLaunch() returned false");
            return;
        }

        BeginLaunch();
    }
    
    private bool CanLaunch()
    {
        var applier = GetComponentInChildren<PlayerColourApplier>();
        if (applier != null && applier.IsColourLocked)
        {
            _hasLockedColour = true;
        }

        if (!(_hasLockedColour || (applier != null && applier.IsColourLocked)))
        {
            if (debugHoming) Debug.Log("CanLaunch: neither cached lock nor instance lock is present");
            return false;
        }

        if (!PlayerPrefs.HasKey("PlayerColour"))
        {
            if (debugHoming) Debug.Log("CanLaunch: PlayerPrefs does not contain PlayerColour key");
            return false;
        }

        if (debugHoming) Debug.Log("CanLaunch: OK (lock present)");
        return true;
    }

    
    private void OnPlayerLifeDeath()
    {
        // request a graceful stop of any homing/launch in progress
        RequestStopLaunch();
        
        _hasLockedColour = false;
    }

   
    public void RequestStopLaunch()
    {
        if (_isStopping) return;
        _isStopping = true;
        _stopRampTimer = Mathf.Max(0f, launchRampDown);

        
        _isHoming = false;
        _isLaunching = true;

        
        _launchTimer = Mathf.Max(_launchTimer, _stopRampTimer);

       
        _playerVelocity = Vector3.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.fixedDeltaTime;
            if (_cooldownTimer < 0f) _cooldownTimer = 0f;
        }

        if (splineContainer == null || splineContainer.Spline == null) return;

        // idle: not homing or launching 
        if (!_isLaunching && !_isHoming)
        {
            var splineIdle = splineContainer.Spline;
            float sampleTIdle = loop ? Mathf.Repeat(_t, 1f) : Mathf.Clamp01(_t);
            float3 tanIdle = splineIdle.EvaluateTangent(sampleTIdle);
            Vector3 worldTanIdle = splineContainer.transform.TransformDirection(new Vector3(tanIdle.x, tanIdle.y, tanIdle.z));
            if (worldTanIdle.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotIdle = ForwardLookRotation(worldTanIdle);
                float rotLerp = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.0001f, playerRotationSmoothTime));
                if (rb != null) rb.MoveRotation(Quaternion.Slerp(rb.rotation, desiredRotIdle, rotLerp));
                else transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotIdle, rotLerp);
            }
            return;
        }

        if (_isHoming)
        {
            var splineH = splineContainer.Spline;
            float sampleTH = loop ? Mathf.Repeat(_t, 1f) : Mathf.Clamp01(_t);
            float3 pH = splineH.EvaluatePosition(sampleTH);
            float3 tanH = math.normalize(splineH.EvaluateTangent(sampleTH));
            Vector3 worldPosH = splineContainer.transform.TransformPoint(new Vector3(pH.x, pH.y, pH.z));
            Vector3 worldTanH = splineContainer.transform.TransformDirection(new Vector3(tanH.x, tanH.y, tanH.z));

            
            Vector3 worldOffsetH = ComputeSplineOffset(sampleTH, worldTanH);
            Vector3 targetWorldPosH = worldPosH + worldOffsetH;

            Vector3 smoothedPosH = Vector3.SmoothDamp(rb != null ? rb.position : transform.position, targetWorldPosH, ref _playerVelocity, playerSmoothTime, float.MaxValue, Time.fixedDeltaTime);
            Quaternion desiredRotH = ForwardLookRotation(worldTanH);
            if (rb != null)
            {
                rb.MovePosition(smoothedPosH);
                float rotLerp = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.0001f, playerRotationSmoothTime));
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, desiredRotH, rotLerp));
            }
            else
            {
                transform.position = smoothedPosH;
                float rotLerp = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.0001f, playerRotationSmoothTime));
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotH, rotLerp);
            }

             _homingTimer -= Time.fixedDeltaTime;
             if (_homingTimer <= 0f)
             {
                 _isHoming = false;
                 _isLaunching = true;
                 _launchTimer = launchDuration;
             }

             return;
         }

        // Launching (or stopping) behaviour
        float speed = launchSpeedNormalized;
        float elapsed = launchDuration - _launchTimer;
        float remaining = _launchTimer;
        float upFactor = (launchRampUp > 0f) ? Mathf.Lerp(minLaunchSpeedFactor, 1f, Mathf.Clamp01(elapsed / launchRampUp)) : 1f;
        float downFactor = (launchRampDown > 0f) ? Mathf.Lerp(minLaunchSpeedFactor, 1f, Mathf.Clamp01(remaining / launchRampDown)) : 1f;

        float factor;
        if (_isStopping)
        {
            float stopRemaining = Mathf.Max(0f, _stopRampTimer);
            float stopDownFactor = (launchRampDown > 0f) ? Mathf.Lerp(minLaunchSpeedFactor, 1f, Mathf.Clamp01(stopRemaining / launchRampDown)) : minLaunchSpeedFactor;
            factor = Mathf.Min(upFactor, stopDownFactor);
        }
        else
        {
            factor = Mathf.Min(upFactor, downFactor);
        }

        float deltaT = speed * factor * Time.fixedDeltaTime;
        float candidateT = _t - deltaT; // counter-clockwise

        var spline = splineContainer.Spline;
        
        float sampleTAdvance = candidateT;
        if (loop) sampleTAdvance = Mathf.Repeat(sampleTAdvance, 1f);
        else sampleTAdvance = Mathf.Clamp01(sampleTAdvance);

        float3 p = spline.EvaluatePosition(sampleTAdvance);
        float3 tan = math.normalize(spline.EvaluateTangent(sampleTAdvance));
        Vector3 localPos = new Vector3(p.x, p.y, p.z);
        Vector3 localTan = new Vector3(tan.x, tan.y, tan.z);

        Vector3 worldPos = splineContainer.transform.TransformPoint(localPos);
        Vector3 worldTan = splineContainer.transform.TransformDirection(localTan);
        
        Vector3 worldOffset = ComputeSplineOffset(sampleTAdvance, worldTan);
        
        Vector3 desiredPos = worldPos + worldOffset;
        Vector3 smoothedPos = Vector3.SmoothDamp(rb != null ? rb.position : transform.position, desiredPos, ref _playerVelocity, playerSmoothTime, float.MaxValue, Time.fixedDeltaTime);
 
        Quaternion desiredRot = ForwardLookRotation(worldTan);

        if (rb != null)
        {
            rb.MovePosition(smoothedPos);
            float rotLerp = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.0001f, playerRotationSmoothTime));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, desiredRot, rotLerp));
        }
        else
        {
            transform.position = smoothedPos;
            float rotLerp = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.0001f, playerRotationSmoothTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotLerp);
        }
        
        _t = candidateT;
        if (loop) _t = Mathf.Repeat(_t, 1f);
        else _t = Mathf.Clamp01(_t);

        // decrement timers
        if (_isStopping)
        {
            _stopRampTimer -= Time.fixedDeltaTime;
            // when stop ramp completes, end the launch
            if (_stopRampTimer <= 0f)
            {
                EndLaunch();
                return;
            }
        }

        _launchTimer -= Time.fixedDeltaTime;
        if (_launchTimer <= 0f)
        {
            EndLaunch();
        }
    }

    private void BeginLaunch()
    {
        if (splineContainer == null || splineContainer.Spline == null) return;
        
        if (launchParticles != null)
        {
            launchParticles.Play();
        }
        
        var spline = splineContainer.Spline;

        Vector3 referencePoint = rb != null ? rb.worldCenterOfMass : transform.position;

        // coarse sampling pass
        int samples = 1024;
        float bestT = 0f;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < samples; i++)
        {
            float tt = i / (float)(samples - 1);
            float3 lp = spline.EvaluatePosition(tt);
            Vector3 worldP = splineContainer.transform.TransformPoint(new Vector3(lp.x, lp.y, lp.z));
            float d = (worldP - referencePoint).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                bestT = tt;
            }
        }

        // local ternary refinement around bestT
        float radius = 1f / samples * 4f;
        float lo = Mathf.Max(0f, bestT - radius);
        float hi = Mathf.Min(1f, bestT + radius);
        for (int it = 0; it < 16; it++)
        {
            float t1 = lo + (hi - lo) / 3f;
            float t2 = hi - (hi - lo) / 3f;
            float3 p1 = spline.EvaluatePosition(t1);
            float3 p2 = spline.EvaluatePosition(t2);
            Vector3 w1 = splineContainer.transform.TransformPoint(new Vector3(p1.x, p1.y, p1.z));
            Vector3 w2 = splineContainer.transform.TransformPoint(new Vector3(p2.x, p2.y, p2.z));
            float d1 = (w1 - referencePoint).sqrMagnitude;
            float d2 = (w2 - referencePoint).sqrMagnitude;
            if (d1 < d2) hi = t2; else lo = t1;
        }
        float refinedT = (lo + hi) * 0.5f;
        _t = Mathf.Repeat(refinedT, 1f);
        Vector3 worldRefined = splineContainer.transform.TransformPoint(new Vector3(spline.EvaluatePosition(refinedT).x, spline.EvaluatePosition(refinedT).y, spline.EvaluatePosition(refinedT).z));
        if (debugHoming) Debug.Log($"BeginLaunch: refinedT={refinedT:F6}, worldPos={worldRefined}");

        float dist = Vector3.Distance(referencePoint, worldRefined);
        if (snapThreshold > 0f && dist > snapThreshold)
        {
            if (rb != null) rb.MovePosition(worldRefined);
            else transform.position = worldRefined;
             _playerVelocity = Vector3.zero;
             if (rb != null)
             {
                 rb.linearVelocity = Vector3.zero;
                 rb.angularVelocity = Vector3.zero;
             }
             _isHoming = false;
             _isLaunching = true;
             _launchTimer = launchDuration;
         }
         else
         {
             _homingTimer = homingDuration;
             _isHoming = true;
             _playerVelocity = Vector3.zero;
             if (rb != null)
             {
                 rb.linearVelocity = Vector3.zero;
                 rb.angularVelocity = Vector3.zero;
             }
         }
     }

     private void EndLaunch()
     {
         _isLaunching = false;
         _isStopping = false;
         _stopRampTimer = 0f;
         _launchTimer = 0f;
         _cooldownTimer = launchCooldown; // keep the existing timer for compatibility
         _nextLaunchAllowedAt = Time.time + launchCooldown; // robust timestamp-based cooldown
         if (rb != null)
         {
             rb.linearVelocity = Vector3.zero;
             rb.angularVelocity = Vector3.zero;
         }
     }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            Vector3 desired = transform.position + transform.rotation * cameraOffset;

            // Smooth position
            cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, desired, ref _cameraVelocity, cameraSmoothTime);

            // Smooth rotation
            if (cameraLookAtPlayer)
            {
                Quaternion lookRot = Quaternion.LookRotation(transform.position - cameraTransform.position, Vector3.up);

                // align yaw with player to keep camera positioned behind
                Vector3 lookEuler = lookRot.eulerAngles;
                float playerYaw = transform.eulerAngles.y;
                Quaternion yawAligned = Quaternion.Euler(lookEuler.x, playerYaw, 0f);

                float rotLerp = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, cameraRotationSmoothTime));
                cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, yawAligned, rotLerp);
            }
            else
            {
                float rotLerp = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, cameraRotationSmoothTime));
                cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, transform.rotation, rotLerp);
            }
        }
    }

    private Vector3 ComputeSplineOffset(float sampleT, Vector3 worldTan)
    {
        if (!enableSplineOffset || worldTan.sqrMagnitude <= 0.0001f) return Vector3.zero;

        Vector3 sideWorld = Vector3.Cross(worldTan, Vector3.up);
        if (sideWorld.sqrMagnitude < 1e-6f)
            sideWorld = Vector3.Cross(worldTan, Vector3.forward);
        sideWorld.Normalize();

        float tNoise = sampleT * noiseScale;
        float timeNoise = Time.time * noiseSpeed;
        float seedOffset = noiseSeed * 0.001f;
        float n1 = Mathf.PerlinNoise(tNoise + seedOffset, timeNoise);

        float lateral = (n1 * 2f - 1f) * maxSplineOffset;

        Vector3 targetOffset = sideWorld * lateral;

        float k = 1f - Mathf.Exp(-Mathf.Max(0f, offsetSmoothness) * Time.fixedDeltaTime);
        _currentWorldOffset = Vector3.Lerp(_currentWorldOffset, targetOffset, k);
        return _currentWorldOffset;
    }
 
     private void OnDrawGizmosSelected()
     {
         if (!debugHoming || splineContainer == null || splineContainer.Spline == null) return;

        var spline = splineContainer.Spline;
        float sampleT = loop ? Mathf.Repeat(_t, 1f) : Mathf.Clamp01(_t);
        float3 p = spline.EvaluatePosition(sampleT);
        float3 tan = spline.EvaluateTangent(sampleT);
        Vector3 worldPos = splineContainer.transform.TransformPoint(new Vector3(p.x, p.y, p.z));
        Vector3 worldTan = splineContainer.transform.TransformDirection(new Vector3(tan.x, tan.y, tan.z));
        Vector3 offset = ComputeSplineOffset(sampleT, worldTan);

        Gizmos.color = Color.yellow; Gizmos.DrawSphere(worldPos, 0.05f);
        Gizmos.color = Color.cyan; Gizmos.DrawSphere(worldPos + offset, 0.06f);
        Gizmos.color = Color.green; Gizmos.DrawLine(worldPos, worldPos + offset);
     }

    private Quaternion ForwardLookRotation(Vector3 worldTan)
    {
        Vector3 f = new Vector3(-worldTan.x, 0f, -worldTan.z);
        if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
        float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, yaw, 0f);
    }

    public void DisableInput()
    {
        if (_inputSystemActions != null)
        {
            _inputSystemActions.Player.LaunchPlayer.performed -= OnLaunch;
            _inputSystemActions.Player.Disable();
        }
    }

    public void EnableInput()
    {
        if (_inputSystemActions == null)
        {
            SetupInput();
            return;
        }
        _inputSystemActions.Player.LaunchPlayer.performed -= OnLaunch;
        _inputSystemActions.Player.LaunchPlayer.performed += OnLaunch;
        _inputSystemActions.Player.Enable();
    }

}
