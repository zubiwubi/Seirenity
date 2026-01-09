using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIView : MonoBehaviour
{
	public bool DefaultView;

	protected Selectable primarySelectable = null;
	private CanvasGroup canvasGroup;
	private bool interactable;
	private bool blocksRaycasts;
	
	protected InputSystem_Actions _inputSystemActions;
	
	protected CanvasGroup CanvasGroup
	{
		get
		{
			if (canvasGroup == null)
			{
				canvasGroup = GetComponent<CanvasGroup>();
				interactable = canvasGroup.interactable;
				blocksRaycasts = canvasGroup.blocksRaycasts;
			}
			return canvasGroup;
		}
	}

	public virtual void Initialize() { }

	public void Hide()
	{
		EventSystem.current.SetSelectedGameObject(null);
		CanvasGroup.alpha = 0;
		CanvasGroup.interactable = false;
		CanvasGroup.blocksRaycasts = false;

		HandleHide();
	}

	public void Show()
	{
		if (gameObject == null) { return; }
		
		CanvasGroup.alpha = 1;
		CanvasGroup.interactable = interactable;
		CanvasGroup.blocksRaycasts = blocksRaycasts;
		
		SetupInput();
		
		HandleShow();
	}

	private void SetupInput()
	{
		_inputSystemActions = new InputSystem_Actions();
		_inputSystemActions.Player.Enable();
	}

	public void OnDestroy()
	{
		HandleDestroy();
	}

	protected virtual void HandleHide() { }
	protected virtual void HandleShow() { }
	protected virtual void HandleDestroy() { }
}
