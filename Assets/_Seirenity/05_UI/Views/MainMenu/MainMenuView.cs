using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuView : UIView
{
    protected override void HandleShow()
    {
        _inputSystemActions.Player.StartGame.performed += OnStartGame;
    }
    
    protected override void HandleHide()
    {
        _inputSystemActions.Player.StartGame.performed -= OnStartGame;
    }

    private void OnStartGame(InputAction.CallbackContext obj)
    {
        
    }


}
