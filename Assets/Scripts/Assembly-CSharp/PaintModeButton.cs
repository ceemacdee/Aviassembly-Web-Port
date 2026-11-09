using UnityEngine;
using UnityEngine.UI;

public class PaintModeButton : MonoBehaviour
{
	public RawImage icon;

	public Image background;

	public Color selectedColor;

	public Color deselectedColor;

	public PartPainter partPainter;

	public DecalPlacer decalPlacer;

	private void Start()
	{
		partPainter.AddRaycastIgnoreObject(base.gameObject);
	}

	private void Update()
	{
		if (PartPainter.PaintModeEnabled)
		{
			background.color = selectedColor;
			icon.color = Color.white;
		}
		if (!PartPainter.PaintModeEnabled)
		{
			background.color = deselectedColor;
			icon.color = Color.black;
		}
	}

	public void Toggle()
	{
		partPainter.SetPaintModeEnabled(!PartPainter.PaintModeEnabled);
		if (PartPainter.PaintModeEnabled)
		{
			background.color = selectedColor;
			icon.color = Color.white;
			if (decalPlacer.IsPlacingDecal)
			{
				decalPlacer.ToggleDecalMode();
			}
		}
		if (!PartPainter.PaintModeEnabled)
		{
			background.color = deselectedColor;
			icon.color = Color.black;
		}
	}
}
