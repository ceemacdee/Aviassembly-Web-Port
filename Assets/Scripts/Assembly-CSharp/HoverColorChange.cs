using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverColorChange : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public MaskableGraphic targetGraphic;

	public Color hoverColor;

	private Color originalColor;

	private void Awake()
	{
		originalColor = targetGraphic.color;
	}

	private void OnDisable()
	{
		targetGraphic.color = originalColor;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		targetGraphic.color = hoverColor;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		targetGraphic.color = originalColor;
	}
}
