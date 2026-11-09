using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OrangeHoverButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private Color orangeHoverColor;

	private Color whiteColor;

	private Color blackColor;

	private Button button;

	private Graphic[] childGraphics;

	private void Start()
	{
		orangeHoverColor = new Color(0.7843137f, 0.4823529f, 0.172549f);
		whiteColor = Color.white;
		blackColor = Color.black;
		button = GetComponent<Button>();
		childGraphics = GetComponentsInChildren<Graphic>();
		ColorBlock colors = button.colors;
		colors.normalColor = Color.white;
		colors.highlightedColor = orangeHoverColor;
		colors.pressedColor = orangeHoverColor;
		colors.selectedColor = orangeHoverColor;
		button.colors = colors;
		GetComponent<Graphic>().color = Color.white;
	}

	private void OnDisable()
	{
		if (childGraphics != null)
		{
			for (int i = 1; i < childGraphics.Length; i++)
			{
				childGraphics[i].color = blackColor;
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		for (int i = 1; i < childGraphics.Length; i++)
		{
			childGraphics[i].color = whiteColor;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		for (int i = 1; i < childGraphics.Length; i++)
		{
			childGraphics[i].color = blackColor;
		}
	}
}
