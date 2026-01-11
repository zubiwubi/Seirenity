// csharp
using System;
using System.Collections;
using UnityEngine;

public class PlayerLife : MonoBehaviour
{
    public Action onDeath;

    public GameObject playerPrefab;
    public GameObject trailCollectorObject;
    
    private GameObject playerInstance;
    private ColourPickerController colourPickerController;
    
    public bool IsSpawnPending { get; private set; } = false;

    [SerializeField]
    private float lifeTime = 10f;
    private float _currentLife;
    
    [HideInInspector]
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

    // Track whether the game has begun (so Start doesn't auto spawn)
    private bool hasGameBegun = false;

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
        
        colourPickerController = FindAnyObjectByType<ColourPickerController>();
        colourPickerController.HidePicker();
        
    }
    
    public void BeginGame()
    {
        if (hasGameBegun) return;
        hasGameBegun = true;

        
        IsSpawnPending = true;
        spawnCoroutine = StartCoroutine(SpawnSequenceCoroutine());
    }
    
    public void CancelInitialSpawn()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
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
        
        // move the trail to the collector object so it doesn't get destroyed with the player
        playerInstance.GetComponentInChildren<TrailRenderer>().gameObject.transform.SetParent(trailCollectorObject.transform);

        // wait for the configured delay
        IsSpawnPending = true;
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
        
        IsSpawnPending = false;

        // reset life for the new instance
        ResetLife();

        // enable life ticking now that the player exists
        isTicking = true;

        // after spawn, wait then show/rehook color picker
        if (colorPickerDelay > 0f) yield return new WaitForSeconds(colorPickerDelay);

        PlayerColourApplier applier = null;
        if (playerInstance != null)
            applier = playerInstance.GetComponentInChildren<PlayerColourApplier>();
        
        if (colourPickerController != null && applier != null)
        {
            colourPickerController.ShowPicker(applier);
        }

        spawnCoroutine = null;
    }
}
