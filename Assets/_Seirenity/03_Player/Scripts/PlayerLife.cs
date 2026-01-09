// csharp
using System;
using System.Collections;
using UnityEngine;

public class PlayerLife : MonoBehaviour
{
    public Action onDeath;

    public GameObject playerPrefab;

    private GameObject playerInstance;

    [SerializeField]
    private float lifeTime = 10f;
    private float _currentLife;
    public bool isTicking = false;

    [Tooltip("Seconds to wait after playing death animation before respawning")]
    [SerializeField]
    private float respawnDelay = 2f;

    [Tooltip("Seconds to wait before spawning the player (used for initial spawn and respawn)")]
    [SerializeField]
    private float spawnDelay = 0.5f;

    [Tooltip("Seconds to wait after spawning before opening the color picker")]
    [SerializeField]
    private float colorPickerDelay = 0.5f;

    public void ResetLife()
    {
        _currentLife = lifeTime;
    }

    private void Start()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerLife: playerPrefab is not assigned.");
            return;
        }

        // Start initial spawn sequence (delayed spawn + delayed color picker)
        StartCoroutine(SpawnSequenceCoroutine());
    }

    private GameObject SpawnNewPlayer()
    {
        var go = Instantiate(playerPrefab, transform);
        if (go == null) return null;

        go.name = playerPrefab.name;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.SetActive(true);
        return go;
    }

    private void Update()
    {
        if (!isTicking) { return; }

        _currentLife -= Time.deltaTime;

        if (_currentLife <= 0)
        {
            isTicking = false;
            StartDeath();
            onDeath?.Invoke();
        }
    }

    private Coroutine deathCoroutine;
    private Coroutine spawnCoroutine;

    private void StartDeath()
    {
        // Stop any pending spawn to avoid race
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (deathCoroutine != null) StopCoroutine(deathCoroutine);
        deathCoroutine = StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        // play death animation (no null checks per earlier behavior)
        playerInstance.GetComponentInChildren<Animator>().Play("PlayerDeath");

        // wait for the configured delay
        yield return new WaitForSeconds(respawnDelay);

        // remove old instance and start spawn coroutine for fresh one
        Destroy(playerInstance);
        playerInstance = null;

        deathCoroutine = null;

        spawnCoroutine = StartCoroutine(SpawnSequenceCoroutine());
    }

    private IEnumerator SpawnSequenceCoroutine()
    {
        // wait before spawning
        if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);

        playerInstance = SpawnNewPlayer();

        // reset life for the new instance
        ResetLife();

        // after spawn, wait then show/rehook color picker
        if (colorPickerDelay > 0f) yield return new WaitForSeconds(colorPickerDelay);

        var colourPickerController = FindObjectOfType<ColourPickerController>();
        if (colourPickerController != null && playerInstance != null)
        {
            var applier = playerInstance.GetComponentInChildren<PlayerColourApplier>();
            if (applier != null) colourPickerController.SetPlayerApplier(applier);
            colourPickerController.ShowPicker();
        }

        spawnCoroutine = null;
    }
}
