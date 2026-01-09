using UnityEngine;

public class AxisLocker : MonoBehaviour
{
    public float lockedY;
    
    private void Update()
    {
        Vector3 position = transform.position;
        position.y = lockedY;
        transform.position = position;
    }
}
