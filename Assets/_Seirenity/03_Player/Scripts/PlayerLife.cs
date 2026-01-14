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
    
    private bool hasGameBegun = false;

    public bool BlockSpawning { get; set; } = false;
    public GameObject CurrentPlayerInstance => playerInstance;

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
        if (colourPickerController == null)
        {
            var all = Resources.FindObjectsOfTypeAll<ColourPickerController>();
            if (all != null && all.Length > 0) colourPickerController = all[0];
        }

        if (colourPickerController != null)
            colourPickerController.HidePicker();

    }

    public void BeginGame()
    {
        if (!hasGameBegun)
        {
            hasGameBegun = true;

            IsSpawnPending = true;
            spawnCoroutine = StartCoroutine(SpawnSequenceCoroutine());
            return;
        }


        if (playerInstance == null && !BlockSpawning && spawnCoroutine == null)
        {
            IsSpawnPending = true;
            spawnCoroutine = StartCoroutine(SpawnSequenceCoroutine());
        }
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
        playerInstance.GetComponentInChildren<Animator>().Play("PlayerDeath");
        
        playerInstance.GetComponentInChildren<TrailRenderer>().gameObject.transform.SetParent(trailCollectorObject.transform);
        
        IsSpawnPending = true;
        yield return new WaitForSeconds(respawnDelay);
        
        Destroy(playerInstance);
        playerInstance = null;

        deathCoroutine = null;

        spawnCoroutine = StartCoroutine(SpawnSequenceCoroutine());
    }

    private IEnumerator SpawnSequenceCoroutine()
    {
        if (spawnDelay > 0f) yield return new WaitForSeconds(spawnDelay);

        // wait while spawning is blocked (overview mode)
        while (BlockSpawning)
        {
            IsSpawnPending = true;
            yield return null;
        }

        playerInstance = SpawnNewPlayer();
        
        ResetLife();

        IsSpawnPending = false;
        
        if (colorPickerDelay > 0f) yield return new WaitForSeconds(colorPickerDelay);

        PlayerColourApplier applier = null;
        if (playerInstance != null)
            applier = playerInstance.GetComponentInChildren<PlayerColourApplier>();

        if (colourPickerController != null && applier != null)
        {
            colourPickerController.ShowPicker(applier);
        }
        else
        {
            StartLifeTicking();
        }

        spawnCoroutine = null;
    }
    
    public void StartLifeTicking()
    {
        ResetLife();
        isTicking = true;
    }
    
    public void OnPlayerColorConfirmed()
    {
        // When player confirms their color, enable player input if a controller exists
        if (playerInstance != null)
        {
            var pc = playerInstance.GetComponentInChildren<PlayerController>();
            if (pc != null) pc.EnableInput();
        }

        StartLifeTicking();
    }
}
