using UnityEngine;

public class PaintModeToggles : MonoBehaviour
{
	public ToggleButton decalModeButton;

	public ToggleButton paintModeButton;

	public PartPainter partPainter;

	public DecalPlacer decalPlacer;

	public GameObject decalSelectionMenu;

	private bool prevMode;

	private void Start()
	{
		partPainter.AddRaycastIgnoreObject(paintModeButton.gameObject);
	}

	public void Update()
	{
		bool flag = decalPlacer.IsPlacingDecal || PartPainter.PaintModeEnabled;
		if (flag != prevMode)
		{
			Singleton<MouseInput>.Instance.SetCursor((decalPlacer.IsPlacingDecal || PartPainter.PaintModeEnabled) ? CursorTypes.Paint : CursorTypes.Normal);
		}
		decalModeButton.SetEnabled(decalPlacer.IsPlacingDecal);
		paintModeButton.SetEnabled(PartPainter.PaintModeEnabled);
		decalSelectionMenu.gameObject.SetActive(decalPlacer.IsPlacingDecal);
		prevMode = flag;
	}

	public void TogglePaintMode()
	{
		partPainter.SetPaintModeEnabled(!PartPainter.PaintModeEnabled);
		if (PartPainter.PaintModeEnabled && decalPlacer.IsPlacingDecal)
		{
			decalPlacer.ToggleDecalMode();
		}
		Update();
	}

	public void ToggleDecalMode()
	{
		decalPlacer.ToggleDecalMode();
		if (decalPlacer.IsPlacingDecal)
		{
			partPainter.SetPaintModeEnabled(value: false);
		}
		Update();
	}
}
