using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private OverviewCameraController overviewController;
    [SerializeField] private PlayerLife playerLife;

    [Header("Transition")]
    [SerializeField] private float cameraTransitionDuration = 3f;
    [SerializeField] private Ease cameraTransitionEase = Ease.InOutSine;

    public bool IsOverviewMode => overviewCamera != null && overviewCamera.gameObject.activeInHierarchy;
    public bool IsInputIgnored => Time.time < _inputIgnoreUntil;

    private InputSystem_Actions _inputSystemActions;
    private float _inputIgnoreUntil;
    private bool _hasStartedGameOnce;

    private void Start()
    {
        SetupInput();

        if (overviewCamera != null) overviewCamera.gameObject.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.gameObject.SetActive(false);
        if (overviewController != null) overviewController.enabled = true;
    }

    private void SetupInput()
    {
        if (_inputSystemActions == null)
            _inputSystemActions = new InputSystem_Actions();

        _inputSystemActions.Player.StartGame.performed += OnStartGame;
        _inputSystemActions.Player.ZoomOut.performed += OnZoomOut;
        _inputSystemActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (_inputSystemActions != null)
        {
            _inputSystemActions.Player.StartGame.performed -= OnStartGame;
            _inputSystemActions.Player.ZoomOut.performed -= OnZoomOut;
            _inputSystemActions.Player.Disable();
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

        if (IsOverviewMode)
            StartGame();
        else
            EnterOverviewMode();
    }

    public void StartGame()
    {
        _inputIgnoreUntil = Time.time + cameraTransitionDuration + 0.1f;

        if (!_hasStartedGameOnce)
        {
            _hasStartedGameOnce = true;
            _inputSystemActions.Player.StartGame.performed -= OnStartGame;
        }

        TransitionToGameplay();
    }

    public void EnterOverviewMode()
    {
        _inputIgnoreUntil = Time.time + cameraTransitionDuration + 0.1f;

        ColourPickerController picker = FindAnyObjectByType<ColourPickerController>();
        if (picker != null && picker.IsOpen)
            picker.HidePicker();

        DestroyCurrentPlayer();

        if (playerLife != null)
        {
            playerLife.BlockSpawning = true;
            playerLife.isTicking = false;
        }

        TransitionToOverview();
    }

    private void TransitionToGameplay()
    {
        Vector3 startPos = overviewCamera.transform.position;
        Quaternion startRot = overviewCamera.transform.rotation;
        float startFOV = overviewCamera.fieldOfView;

        Vector3 finalPos = gameplayCamera.transform.position;
        Quaternion finalRot = gameplayCamera.transform.rotation;
        float finalFOV = gameplayCamera.fieldOfView;
        float finalDepth = gameplayCamera.depth;

        gameplayCamera.transform.position = startPos;
        gameplayCamera.transform.rotation = startRot;
        gameplayCamera.fieldOfView = startFOV;
        gameplayCamera.depth = overviewCamera.depth + 1;
        gameplayCamera.gameObject.SetActive(true);

        if (overviewController != null)
            overviewController.enabled = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(gameplayCamera.transform.DOMove(finalPos, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(gameplayCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => gameplayCamera.fieldOfView, x => gameplayCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(overviewCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => overviewCamera.fieldOfView, x => overviewCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));

        seq.OnComplete(() =>
        {
            if (overviewCamera != null)
                overviewCamera.gameObject.SetActive(false);

            gameplayCamera.depth = finalDepth;

            if (playerLife != null)
            {
                playerLife.BlockSpawning = false;
                playerLife.BeginGame();
            }
        });
    }

    private void TransitionToOverview()
    {
        Vector3 finalPos = overviewCamera.transform.position;
        Quaternion finalRot = overviewCamera.transform.rotation;
        float finalFOV = overviewCamera.fieldOfView;
        float origDepth = overviewCamera.depth;

        overviewCamera.transform.position = gameplayCamera.transform.position;
        overviewCamera.transform.rotation = gameplayCamera.transform.rotation;
        overviewCamera.fieldOfView = gameplayCamera.fieldOfView;
        overviewCamera.depth = gameplayCamera.depth + 1;
        overviewCamera.gameObject.SetActive(true);

        if (overviewController != null)
            overviewController.enabled = false;

        Sequence seq = DOTween.Sequence();
        seq.Join(overviewCamera.transform.DOMove(finalPos, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(overviewCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => overviewCamera.fieldOfView, x => overviewCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));

        seq.OnComplete(() =>
        {
            if (gameplayCamera != null)
                gameplayCamera.gameObject.SetActive(false);

            overviewCamera.depth = origDepth;

            if (overviewController != null)
                overviewController.enabled = true;
        });
    }

    private void DestroyCurrentPlayer()
    {
        if (playerLife == null || playerLife.CurrentPlayerInstance == null)
            return;

        GameObject playerInstance = playerLife.CurrentPlayerInstance;

        PlayerController controller = playerInstance.GetComponentInChildren<PlayerController>();
        if (controller != null)
        {
            controller.RequestStopLaunch();
            controller.DisableInput();
        }
        
        FindFirstObjectByType<PlayerLife>().StartDeath();
    }
}
