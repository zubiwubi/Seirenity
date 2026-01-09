using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform zoomedOutEnvironmentTransform;
    [SerializeField] private Transform playerEnvironmentTransform;
    private Camera _mainCamera;
    
    private InputSystem_Actions _inputSystemActions;

    private void Start()
    {
        SetupInput();

        if (zoomedOutEnvironmentTransform == null) { Debug.LogError("Zoomed out environment transform is not set"); }
        
        _mainCamera = Camera.main;
        
        _mainCamera.gameObject.transform.position = zoomedOutEnvironmentTransform.transform.position;
    }

    private void SetupInput()
    {
        _inputSystemActions = new InputSystem_Actions();
        _inputSystemActions.Player.Enable();
        
        _inputSystemActions.Player.StartGame.performed += OnStartGame;
    }

    private void OnStartGame(InputAction.CallbackContext context)
    {
        Debug.Log("Start Game");
        _mainCamera.transform.DOMove(playerEnvironmentTransform.position + Vector3.up, 3f);
        
    }
}
