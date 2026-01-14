using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private OverviewCameraController overviewController;
    
    [SerializeField] private PlayerLife playerLife;
    
    public bool IsOverviewMode => overviewCamera != null && overviewCamera.gameObject.activeInHierarchy;
    
    public bool IsInputIgnored => Time.time < _inputIgnoreUntil;

    [Header("Transition")]
    [SerializeField] private float cameraTransitionDuration = 3f;
    [SerializeField] private Ease cameraTransitionEase = Ease.InOutSine;

    private InputSystem_Actions _inputSystemActions;
    
    private float _inputIgnoreUntil;
    
    private Coroutine _postStartRoutine;

    private void Start()
    {
        SetupInput();

        if (overviewCamera == null) { Debug.LogError("Overview camera is not assigned on GameManager"); }
        if (gameplayCamera == null) { Debug.LogError("Gameplay camera is not assigned on GameManager"); }
        
        if (overviewCamera != null) overviewCamera.gameObject.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.gameObject.SetActive(false);

        if (overviewController != null) overviewController.enabled = true;
    }

    private void SetupInput()
    {
        SubscribeInputs();
    }

    private void SubscribeInputs()
    {
        if (_inputSystemActions == null)
            _inputSystemActions = new InputSystem_Actions();

        _inputSystemActions.Player.StartGame.performed -= OnStartGame;
        _inputSystemActions.Player.StartGame.performed += OnStartGame;

        _inputSystemActions.Player.ZoomOut.performed -= OnZoomOut;
        _inputSystemActions.Player.ZoomOut.performed += OnZoomOut;

        _inputSystemActions.Player.Enable();
    }

    private void UnsubscribeInputs()
    {
        if (_inputSystemActions == null) return;

        _inputSystemActions.Player.StartGame.performed -= OnStartGame;
        _inputSystemActions.Player.ZoomOut.performed -= OnZoomOut;
        _inputSystemActions.Player.Disable();
    }

    private IEnumerator DelayedSubscribe(float delay)
    {
        UnsubscribeInputs();

        yield return new WaitForSeconds(delay);

        SubscribeInputs();
    }

    private void OnDisable()
    {
        UnsubscribeInputs();
    }

    private void RemoveStartGameSubscription()
    {
        if (_inputSystemActions != null)
        {
            _inputSystemActions.Player.StartGame.performed -= OnStartGame;
        }
    }

    private void OnStartGame(InputAction.CallbackContext context)
    {
        if (Time.time < _inputIgnoreUntil) return; 
        StartGame();
    }

    private void OnZoomOut(InputAction.CallbackContext context)
    {
        if (Time.time < _inputIgnoreUntil) return; 
        
        ColourPickerController picker = FindAnyObjectByType<ColourPickerController>();
        if (picker != null && picker.IsOpen) return;

        if (context.performed)
        {
            if (overviewCamera != null && overviewCamera.gameObject.activeInHierarchy)
            {
                StartGame();
            }
            else
            {
                EnterOverviewMode();
            }
        }
    }
    
    public void StartGame()
    {
        _inputIgnoreUntil = Time.time + cameraTransitionDuration + 0.1f;

        if (overviewCamera == null || gameplayCamera == null)
        {
            Debug.LogWarning("Cameras not configured on GameManager. Aborting smooth transition.");
            if (playerLife != null) playerLife.BeginGame();
            RemoveStartGameSubscription();
            return;
        }
        
        Vector3 finalPos = gameplayCamera.transform.position;
        Quaternion finalRot = gameplayCamera.transform.rotation;
        float finalFOV = gameplayCamera.fieldOfView;
        float finalDepth = gameplayCamera.depth;
        
        gameplayCamera.transform.position = overviewCamera.transform.position;
        gameplayCamera.transform.rotation = overviewCamera.transform.rotation;
        gameplayCamera.fieldOfView = overviewCamera.fieldOfView;
        gameplayCamera.depth = overviewCamera.depth + 1;

        gameplayCamera.gameObject.SetActive(true);
        if (overviewController != null) overviewController.enabled = false;
        
        Sequence seq = DOTween.Sequence();
        seq.Join(gameplayCamera.transform.DOMove(finalPos, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(gameplayCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => gameplayCamera.fieldOfView, x => gameplayCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));
        
        seq.Join(overviewCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => overviewCamera.fieldOfView, x => overviewCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));

        seq.OnComplete(() =>
        {
            if (overviewCamera != null) overviewCamera.gameObject.SetActive(false);
            gameplayCamera.depth = finalDepth; 
            
            if (playerLife != null)
            {
                playerLife.BeginGame();
                
                GameObject existing = playerLife.CurrentPlayerInstance;
                if (existing != null)
                {
                    PlayerColourApplier ap = existing.GetComponentInChildren<PlayerColourApplier>();
                    PlayerController pc = existing.GetComponentInChildren<PlayerController>();
                    if (ap != null && ap.IsColourLocked)
                    {
                        playerLife.StartLifeTicking();
                        if (pc != null) pc.EnableInput();
                    }
                    else if (ap == null)
                    {
                        // no applier and no picker: resume life
                        ColourPickerController pickerTmp = FindAnyObjectByType<ColourPickerController>();
                        if (pickerTmp == null)
                        {
                            playerLife.StartLifeTicking();
                            if (pc != null) pc.EnableInput();
                        }
                    }
                }

                if (_postStartRoutine != null) StopCoroutine(_postStartRoutine);
                _postStartRoutine = StartCoroutine(PostStartHandlePlayer());
            }

            RemoveStartGameSubscription();
        });
    }
    
    public void EnterOverviewMode()
    {
        _inputIgnoreUntil = Time.time + cameraTransitionDuration + 0.1f;

        if (overviewCamera == null || gameplayCamera == null)
        {
            if (overviewCamera != null) overviewCamera.gameObject.SetActive(true);
            if (gameplayCamera != null) gameplayCamera.gameObject.SetActive(false);

            if (overviewController != null) overviewController.enabled = true;
            ColourPickerController pickerErr = FindAnyObjectByType<ColourPickerController>();
            if (pickerErr != null && pickerErr.IsOpen) pickerErr.HidePicker();

            if (playerLife != null)
            {
                playerLife.BlockSpawning = true;
                playerLife.CancelInitialSpawn();
                playerLife.isTicking = false;

                GameObject inst = playerLife.CurrentPlayerInstance;
                if (inst != null)
                {
                    PlayerController playerController = inst.GetComponentInChildren<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.RequestStopLaunch();
                        playerController.DisableInput();
                    }
                }
            }

            StartCoroutine(DelayedSubscribe(0.25f));
            return;
        }

        Vector3 finalPos = overviewCamera.transform.position;
        Quaternion finalRot = overviewCamera.transform.rotation;
        float finalFOV = overviewCamera.fieldOfView;

        ColourPickerController picker = FindAnyObjectByType<ColourPickerController>();
        if (picker != null && picker.IsOpen) picker.HidePicker();

        if (playerLife != null)
        {
            playerLife.BlockSpawning = true;
            playerLife.CancelInitialSpawn();
            playerLife.isTicking = false;

            GameObject inst = playerLife.CurrentPlayerInstance;
            if (inst != null)
            {
                PlayerController playerController = inst.GetComponentInChildren<PlayerController>();
                if (playerController != null)
                {
                    playerController.RequestStopLaunch();
                    playerController.DisableInput();
                }
            }
        }

        if (overviewController != null) overviewController.enabled = false;

        float origOverviewDepth = overviewCamera.depth;

        overviewCamera.transform.position = gameplayCamera.transform.position;
        overviewCamera.transform.rotation = gameplayCamera.transform.rotation;
        overviewCamera.fieldOfView = gameplayCamera.fieldOfView;
        overviewCamera.depth = gameplayCamera.depth + 1;
        overviewCamera.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.Join(overviewCamera.transform.DOMove(finalPos, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(overviewCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => overviewCamera.fieldOfView, x => overviewCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));

        seq.OnComplete(() =>
        {
            if (gameplayCamera != null) gameplayCamera.gameObject.SetActive(false);
            overviewCamera.depth = origOverviewDepth;

            if (overviewController != null) overviewController.enabled = true;

            StartCoroutine(DelayedSubscribe(0.25f));
        });
    }

    private IEnumerator PostStartHandlePlayer()
    {
        float timeout = 5f;
        float start = Time.time;
        while ((playerLife == null || playerLife.CurrentPlayerInstance == null) && Time.time - start < timeout)
        {
            yield return null;
        }

        GameObject inst = playerLife != null ? playerLife.CurrentPlayerInstance : null;
        PlayerColourApplier applier = inst != null ? inst.GetComponentInChildren<PlayerColourApplier>() : null;
        ColourPickerController picker = FindAnyObjectByType<ColourPickerController>();

        if (applier != null && picker != null && !applier.IsColourLocked)
        {
            picker.ShowPicker(applier);
        }
        else
        {
            if (inst != null)
            {
                PlayerController pc = inst.GetComponentInChildren<PlayerController>();
                if (pc != null) pc.EnableInput();
            }

            if (playerLife != null)
            {
                playerLife.StartLifeTicking();
            }
        }

        _postStartRoutine = null;
    }
}
