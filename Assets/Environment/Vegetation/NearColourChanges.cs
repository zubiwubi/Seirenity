using UnityEngine;

/// <summary>
/// Makes this object copy the player's material colour when the player
/// comes within <see cref="detectRadius"/> units.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class CopyPlayerColourOnProximity : MonoBehaviour
{
    // Tag used on the player object – change if you use a different tag.
    [Tooltip("Tag assigned to the player GameObject.")]
    public string playerTag = "Player";

    // How far away the player can be before the colour is copied.
    [Tooltip("Distance at which the colour will be copied.")]
    public float detectRadius = 5f;

    // How often (in seconds) we check the distance. A small value feels responsive,
    // but larger intervals reduce per‑frame overhead.
    [Tooltip("Time between proximity checks.")]
    public float checkInterval = 0.1f;

    // Cached references for speed.
    private Renderer _renderer;
    private Transform _playerTransform;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        // Find the player once at start – assumes there is exactly one object with the tag.
        var playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            _playerTransform = playerObj.transform;
        else
            Debug.LogWarning($"CopyPlayerColourOnProximity: No object with tag '{playerTag}' found.");

        // Begin periodic checking.
        InvokeRepeating(nameof(CheckProximityAndUpdateColour), 0f, checkInterval);
    }

    private void CheckProximityAndUpdateColour()
    {
        if (_playerTransform == null) return;

        // Simple distance check.
        float sqrDist = (transform.position - _playerTransform.position).sqrMagnitude;
        if (sqrDist <= detectRadius * detectRadius)
        {
            // Grab the player's colour from its renderer.
            var playerRend = _playerTransform.GetComponent<Renderer>();
            if (playerRend != null && playerRend.sharedMaterial != null && playerRend.sharedMaterial.HasProperty("_BaseColor"))
            {
                Color playerColour = playerRend.sharedMaterial.GetColor("_BaseColor");

                // Apply the colour to this object's material using a property block
                // (keeps the original material asset untouched, which is safe for URP).
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", playerColour);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }
    }

    // Optional: visualise the detection radius in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectRadius);
    }
}