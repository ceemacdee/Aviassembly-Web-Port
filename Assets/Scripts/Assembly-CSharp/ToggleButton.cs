using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
	public RawImage icon;

	public Image background;

	public Color selectedColor;

	public Color deselectedColor;

	public void SetEnabled(bool value)
	{
		if (value)
		{
			background.color = selectedColor;
			icon.color = Color.white;
		}
		if (!value)
		{
			background.color = deselectedColor;
			icon.color = Color.black;
		}
	}
}
