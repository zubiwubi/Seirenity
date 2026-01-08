using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Follow")]
    public float distance = 6f;
    public float heightOffset = 1.5f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.08f;
    public float rotationLerp = 12f;

    Vector3 positionVelocity;

    void Start()
    {
        if (target == null)
        {
            if (transform.parent != null) target = transform.parent;
            else
            {
                var t = GameObject.FindWithTag("Player");
                if (t != null) target = t.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position - target.forward * distance + Vector3.up * heightOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref positionVelocity, positionSmoothTime);

        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(toTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 1f - Mathf.Exp(-rotationLerp * Time.deltaTime));
        }
    }
}
