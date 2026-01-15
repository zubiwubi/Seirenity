using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class OverviewCameraController : MonoBehaviour
{
    [Header("Path")]
    public SplineContainer splineContainer;
    public Transform lookTarget;

    [Header("Motion")]
    public float speed = 0.02f;
    public bool loop = true;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float positionLerp = 0.6f;
    [Range(0f, 1f)] public float rotationSlerp = 0.6f;

    [Header("Transition")]
    public float transitionDuration = 3f;
    public Ease transitionEase = Ease.InOutSine;

    private float _t;
    private bool _isTransitioning;
    private Tween _positionTween;
    private Tween _rotationTween;

    private void OnEnable()
    {
        if (splineContainer != null && splineContainer.Spline != null)
        {
            StartSmoothTransition();
        }
    }

    private void OnDisable()
    {
        _positionTween?.Kill();
        _rotationTween?.Kill();
        _isTransitioning = false;
    }

    private void Update()
    {
        if (splineContainer == null || splineContainer.Spline == null || _isTransitioning) return;

        float dt = Time.deltaTime;
        _t += speed * dt;
        if (loop) _t = Mathf.Repeat(_t, 1f);
        else _t = Mathf.Clamp01(_t);

        Spline spline = splineContainer.Spline;
        float3 p = spline.EvaluatePosition(_t);
        Vector3 worldPos = splineContainer.transform.TransformPoint(new Vector3(p.x, p.y, p.z));

        transform.position = Vector3.Lerp(transform.position, worldPos, Mathf.Clamp01(positionLerp * dt * 10f));

        if (lookTarget != null)
        {
            Vector3 direction = (lookTarget.position - transform.position).normalized;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(rotationSlerp * dt * 10f));
            }
        }
    }

    private void StartSmoothTransition()
    {
        _positionTween?.Kill();
        _rotationTween?.Kill();
        _isTransitioning = true;

        Spline spline = splineContainer.Spline;
        float3 p = spline.EvaluatePosition(_t);
        Vector3 worldTargetPos = splineContainer.transform.TransformPoint(new Vector3(p.x, p.y, p.z));

        Quaternion targetRotation = transform.rotation;
        if (lookTarget != null)
        {
            Vector3 direction = (lookTarget.position - worldTargetPos).normalized;
            if (direction.sqrMagnitude > 0.0001f)
            {
                targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        _positionTween = transform.DOMove(worldTargetPos, transitionDuration).SetEase(transitionEase);
        _rotationTween = transform.DORotateQuaternion(targetRotation, transitionDuration).SetEase(transitionEase);

        _positionTween.OnComplete(() => _isTransitioning = false);
    }
}
