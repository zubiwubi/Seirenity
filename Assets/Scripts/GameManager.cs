using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera gameplayCamera;
    
    [SerializeField] private PlayerLife playerLife;

    [Header("Transition")]
    [SerializeField] private float cameraTransitionDuration = 3f;
    [SerializeField] private Ease cameraTransitionEase = Ease.InOutSine;

    private InputSystem_Actions _inputSystemActions;

    private void Start()
    {
        SetupInput();

        if (overviewCamera == null) { Debug.LogError("Overview camera is not assigned on GameManager"); }
        if (gameplayCamera == null) { Debug.LogError("Gameplay camera is not assigned on GameManager"); }
        
        // Start with the overview (menu) camera active and gameplay camera disabled
        if (overviewCamera != null) overviewCamera.gameObject.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.gameObject.SetActive(false);
        
    }

    private void SetupInput()
    {
        _inputSystemActions = new InputSystem_Actions();
        _inputSystemActions.Player.Enable();
        
        _inputSystemActions.Player.StartGame.performed += OnStartGame;
    }

    private void OnDisable()
    {
        DisableInput();
    }
    
    private void DisableInput()
    {
        if (_inputSystemActions != null)
        {
            _inputSystemActions.Player.StartGame.performed -= OnStartGame;
            _inputSystemActions.Player.Disable();
        }
    }

    private void OnStartGame(InputAction.CallbackContext context)
    {
        StartGame();
    }
    
    public void StartGame()
    {
        if (overviewCamera == null || gameplayCamera == null)
        {
            Debug.LogWarning("Cameras not configured on GameManager. Aborting smooth transition.");
            if (playerLife != null) playerLife.BeginGame();
            DisableInput();
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
        
        Sequence seq = DOTween.Sequence();
        seq.Join(gameplayCamera.transform.DOMove(finalPos, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(gameplayCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => gameplayCamera.fieldOfView, x => gameplayCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));
        
        seq.Join(overviewCamera.transform.DORotateQuaternion(finalRot, cameraTransitionDuration).SetEase(cameraTransitionEase));
        seq.Join(DOTween.To(() => overviewCamera.fieldOfView, x => overviewCamera.fieldOfView = x, finalFOV, cameraTransitionDuration).SetEase(cameraTransitionEase));

        seq.OnComplete(() =>
        {
            if (overviewCamera != null) overviewCamera.gameObject.SetActive(false);
            gameplayCamera.depth = finalDepth; // restore original depth
            
            if (playerLife != null)
            {
                playerLife.BeginGame();
            }
            DisableInput();
        });
    }
}
