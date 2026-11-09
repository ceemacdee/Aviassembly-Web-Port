using UnityEngine;
using UnityEngine.UI;

public class SnappingButton : MonoBehaviour
{
	public PartPlacer placer;

	[Space(10f)]
	public Image buttonImage;

	public GameObject disabledText;

	public Color selectedColor;

	public Color deselectedColor;

	private void Start()
	{
		buttonImage.color = (PartPlacer.snapMode ? selectedColor : deselectedColor);
		UpdateColor();
	}

	private void UpdateColor()
	{
		buttonImage.color = (PartPlacer.snapMode ? selectedColor : deselectedColor);
		disabledText.SetActive(!PartPlacer.snapMode);
		((RectTransform)base.transform).sizeDelta = new Vector2((!PartPlacer.snapMode) ? 300 : 55, 55f);
	}

	public void Press()
	{
		PartPlacer.snapMode = !PartPlacer.snapMode;
		UpdateColor();
	}
}
