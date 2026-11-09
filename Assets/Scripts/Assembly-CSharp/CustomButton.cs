using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CustomButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[Serializable]
	public class ButtonClickedEvent : UnityEvent
	{
	}

	public bool hover;

	public bool interactable;

	[SerializeField]
	private ButtonClickedEvent onClick = new ButtonClickedEvent();

	private void Update()
	{
		if (hover && MouseInput.GetMouseButtonDown(0))
		{
			Click();
		}
	}

	private void Click()
	{
		if (interactable)
		{
			onClick.Invoke();
		}
	}

	public void OnPointerEnter(PointerEventData data)
	{
		hover = true;
	}

	public void OnPointerExit(PointerEventData data)
	{
		hover = false;
	}

	private void OnDisable()
	{
		hover = false;
	}
}
