using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MirrorButton : MonoBehaviour
{
	public PartPlacer placer;

	[Space(10f)]
	public Image buttonImage;

	public TMP_Text text;

	public RawImage icon;

	public Color selectedColor;

	public Color deselectedColor;

	private void Start()
	{
		buttonImage.color = (PartPlacer.mirrorMode ? selectedColor : deselectedColor);
		UpdateColor();
	}

	private void UpdateColor()
	{
		buttonImage.color = (PartPlacer.mirrorMode ? selectedColor : deselectedColor);
		text.color = (PartPlacer.mirrorMode ? Color.white : Color.black);
		icon.color = (PartPlacer.mirrorMode ? Color.white : Color.black);
	}

	public void Press()
	{
		PartPlacer.mirrorMode = !PartPlacer.mirrorMode;
		UpdateColor();
	}
}
