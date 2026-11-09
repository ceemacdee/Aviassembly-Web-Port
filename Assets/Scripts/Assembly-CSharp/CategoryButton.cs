using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
	[Header("Background")]
	public MaskableGraphic backroundImage;

	public Color selectedBackgroundColor;

	public Color deselectedBackgroundColor;

	[Header("Icon")]
	public MaskableGraphic iconImage;

	public Color selectedIconColor;

	public Color deselectedIconColor;

	public void Select()
	{
		backroundImage.color = selectedBackgroundColor;
		iconImage.color = selectedIconColor;
	}

	public void Deselect()
	{
		backroundImage.color = deselectedBackgroundColor;
		iconImage.color = deselectedIconColor;
	}
}
