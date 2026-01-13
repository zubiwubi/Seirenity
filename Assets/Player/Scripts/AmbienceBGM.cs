using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayAmbienceOnStart : MonoBehaviour
{
    // Drag the “ambience_bgm” file onto this field in the Inspector
    [SerializeField] private AudioClip ambienceBgm;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;   // we’ll start it ourselves
        _audioSource.loop = true;           // usually background music loops
    }

    private void Start()
    {
        if (ambienceBgm != null)
        {
            _audioSource.clip = ambienceBgm;
            _audioSource.Play();
        }
        else
        {
            Debug.LogWarning(
                "PlayAmbienceOnStart: No AudioClip assigned. " +
                "Drag the ‘ambience_bgm’ file onto the script’s field."
            );
        }
    }
}