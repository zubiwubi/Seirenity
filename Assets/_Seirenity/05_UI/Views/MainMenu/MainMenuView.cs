using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuView : UIView
{
    public TextMeshProUGUI title;
    public TextMeshProUGUI credits;

    [SerializeField] private float transitionDuration = 1.5f;

    private bool _isTransitioning;
    
    protected override void HandleShow()
    {
        _inputSystemActions.Player.StartGame.performed += OnStartGame;
    }
    
    protected override void HandleHide()
    {
        if (_inputSystemActions == null) { return; }
        _inputSystemActions.Player.StartGame.performed -= OnStartGame;
    }

    private void OnStartGame(InputAction.CallbackContext obj)
    {
        if (_isTransitioning) return;
        
        if (_inputSystemActions != null)
        {
            _inputSystemActions.Player.StartGame.performed -= OnStartGame;
        }

        StartCoroutine(TransitionToGame());
    }

    private IEnumerator TransitionToGame()
    {
        _isTransitioning = true;
        
        CanvasGroup.interactable = false;
        CanvasGroup.blocksRaycasts = false;

        float startAlpha = CanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            CanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        CanvasGroup.alpha = 0f;
        
        var viewService = FindFirstObjectByType<UIViewService>();
        if (viewService != null)
        {
            viewService.Show<GameView>();
        }
        else
        {
            Debug.LogWarning("MainMenuView: UIViewService not found. Cannot switch to GameView.");
        }

        _isTransitioning = false;
    }


}
