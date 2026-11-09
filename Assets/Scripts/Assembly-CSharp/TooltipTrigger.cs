using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public TooltipSettings settings;

	private bool tooltipActive;

	public void OnPointerEnter(PointerEventData eventData)
	{
		tooltipActive = true;
		Singleton<TooltipSystem>.Instance.Show(settings);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		tooltipActive = false;
		Singleton<TooltipSystem>.Instance.Hide();
	}

	private void OnEnable()
	{
		tooltipActive = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)base.transform, MouseInput.GetMousePosition());
		if (tooltipActive)
		{
			Singleton<TooltipSystem>.Instance.Show(settings);
		}
	}

	private void Update()
	{
		if (tooltipActive && !PointerInRect())
		{
			Singleton<TooltipSystem>.Instance.Hide();
			tooltipActive = false;
		}
	}

	private void OnDisable()
	{
		if (tooltipActive)
		{
			Singleton<TooltipSystem>.Instance.Hide();
			tooltipActive = false;
		}
	}

	private bool PointerInRect()
	{
		return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)base.transform, Input.mousePosition);
	}
}
