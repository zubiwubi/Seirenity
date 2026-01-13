using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayWindSfxOnSpace : MonoBehaviour
{
    // Name of the file (without extension) placed under a Resources folder
    private const string ClipName = "wind_sfx";

    private AudioSource _audioSource;

    private void Awake()
    {
        // Grab the AudioSource component
        _audioSource = GetComponent<AudioSource>();

        // Load the clip from Resources
        AudioClip windClip = Resources.Load<AudioClip>(ClipName);
        if (windClip == null)
        {
            Debug.LogError($"PlayWindSfxOnSpace: Could not find an AudioClip named \"{ClipName}\" in a Resources folder.");
            return;
        }

        // Assign the clip to the AudioSource
        _audioSource.clip = windClip;
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    private void Update()
    {
        // Play when Space is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_audioSource.clip != null)
                _audioSource.Play();
        }
    }
}