using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class OverviewCameraController : MonoBehaviour
{
    [Header("Path")]
    public SplineContainer splineContainer; 
    public Transform lookTarget;

    [Header("Motion")]
    public float speed = 0.02f; 
    public bool loop = true;

    [Header("Smoothing & Noise")]
    [Range(0f, 1f)] public float positionLerp = 0.5f;
    [Range(0f, 1f)] public float rotationSlerp = 0.5f;

    private float _t;

    private void Reset()
    {
        speed = 0.02f;
        positionLerp = 0.6f;
        rotationSlerp = 0.6f;
    }

    private void OnEnable() => _t = 0f;

    private void Update()
    {
        float dt = Time.deltaTime;
        _t += speed * dt;
        if (!loop) _t = Mathf.Clamp01(_t);

        Vector3 targetPos;

        
        Spline spline = splineContainer.Spline;
        float sampleT = loop ? Mathf.Repeat(_t, 1f) : Mathf.Clamp01(_t);
        float3 p = spline.EvaluatePosition(sampleT);
        Vector3 worldPos = splineContainer.transform.TransformPoint(new Vector3(p.x, p.y, p.z));

        targetPos = worldPos;

        transform.position = Vector3.Lerp(transform.position, targetPos, Mathf.Clamp01(positionLerp * dt * 10f));

        Vector3 look = lookTarget.position;
        Quaternion targetRot = Quaternion.LookRotation((look - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(rotationSlerp * dt * 10f));
    }
}
