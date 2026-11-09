using System.Collections.Generic;
using UnityEngine;

public class PartPainter : Singleton<PartPainter>
{
	public Color currentColor;

	public Color[] colors;

	public AudioDef paintSplash;

	public ColorPickerUI colorPicker;

	public PartPlacer partPlacer;

	public DecalPlacer decalPlacer;

	public GameObject pickerToggle;

	public GameObject customColorOutline;

	public GameObject colorPickingPrefab;

	public Transform colorPickersParent;

	public GameObject colorPickingPanel;

	private IPaintable lastHoverPart;

	private GameObject lastHoverPartGameObject;

	public static bool PaintModeEnabled;

	private List<GameObject> raycastIgnoreObjects = new List<GameObject>();

	private List<ColorPick> colorPicks = new List<ColorPick>();

	private bool pipetMode;

	private void Start()
	{
		PaintModeEnabled = false;
		for (int i = 0; i < colors.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(colorPickingPrefab);
			gameObject.transform.parent = colorPickersParent;
			gameObject.transform.localScale = Vector3.one;
			gameObject.GetComponent<ColorPick>().Init(this, colors[i]);
			colorPicks.Add(gameObject.GetComponent<ColorPick>());
			AddRaycastIgnoreObject(gameObject);
		}
		colorPicks[0].Select();
		raycastIgnoreObjects.Add(colorPicker.gameObject);
		for (int j = 0; j < colorPicker.transform.childCount; j++)
		{
			raycastIgnoreObjects.Add(colorPicker.transform.GetChild(j).gameObject);
		}
		raycastIgnoreObjects.Add(pickerToggle.gameObject);
		colorPicker.gameObject.SetActive(value: false);
		customColorOutline.gameObject.SetActive(value: false);
	}

	public void SetCurrentColor(Color color)
	{
		for (int i = 0; i < colorPicks.Count; i++)
		{
			colorPicks[i].Deselect();
		}
		currentColor = color;
		if (colorPicker.gameObject.activeInHierarchy)
		{
			ToggleColorPicker();
		}
	}

	public void ToggleColorPipet()
	{
		pipetMode = !pipetMode;
		Singleton<MouseInput>.Instance.SetCursor((!pipetMode) ? CursorTypes.Paint : CursorTypes.ColorPick);
	}

	public void ToggleColorPicker()
	{
		colorPicker.gameObject.SetActive(!colorPicker.gameObject.activeInHierarchy);
		customColorOutline.gameObject.SetActive(colorPicker.gameObject.activeInHierarchy);
		if (!colorPicker.gameObject.activeInHierarchy)
		{
			pipetMode = false;
			if (PaintModeEnabled)
			{
				Singleton<MouseInput>.Instance.SetCursor(CursorTypes.Paint);
			}
			else
			{
				Singleton<MouseInput>.Instance.SetCursor(CursorTypes.Normal);
			}
		}
	}

	public void SetPaintModeEnabled(bool value)
	{
		PaintModeEnabled = value;
		colorPickingPanel.gameObject.SetActive(PaintModeEnabled);
		if (!PaintModeEnabled)
		{
			colorPicker.gameObject.SetActive(value: false);
			pipetMode = false;
		}
		if (PaintModeEnabled)
		{
			colorPicker.gameObject.SetActive(customColorOutline.activeInHierarchy);
		}
	}

	public void AddRaycastIgnoreObject(GameObject obj)
	{
		raycastIgnoreObjects.Add(obj);
	}

	private void Update()
	{
		bool flag = pipetMode;
		colorPickingPanel.gameObject.SetActive(PaintModeEnabled);
		if (lastHoverPartGameObject != null)
		{
			IPaintable mirrorPaintable = GetMirrorPaintable(lastHoverPart);
			if (lastHoverPart != null)
			{
				lastHoverPart.ResetColor();
			}
			mirrorPaintable?.ResetColor();
		}
		if (pipetMode)
		{
			lastHoverPart = GetHoverPart();
			if (lastHoverPart != null && MouseInput.GetMouseButton(0))
			{
				currentColor = lastHoverPart.GetCurrentColor();
				colorPicker.SetColor(currentColor);
				ToggleColorPipet();
			}
			if (MouseInput.GetMouseButtonUp(1))
			{
				ToggleColorPipet();
			}
		}
		if (!PaintModeEnabled || pipetMode)
		{
			return;
		}
		lastHoverPart = GetHoverPart();
		IPaintable mirrorPaintable2 = GetMirrorPaintable(lastHoverPart);
		Color color = (colorPicker.gameObject.activeInHierarchy ? colorPicker.GetColor() : currentColor);
		if (lastHoverPart != null)
		{
			lastHoverPart.SetColor(color, apply: false);
			mirrorPaintable2?.SetColor(color, apply: false);
			if (MouseInput.GetMouseButtonDown(0))
			{
				bool flag2 = false;
				if (lastHoverPart.GetCurrentColor() != color)
				{
					flag2 = true;
				}
				lastHoverPart.SetColor(color, apply: true);
				if (mirrorPaintable2 != null)
				{
					if (mirrorPaintable2.GetCurrentColor() != color)
					{
						flag2 = true;
					}
					mirrorPaintable2.SetColor(color, apply: true);
				}
				if (flag2)
				{
					Singleton<AudioManager>.Instance.PlaySound(paintSplash);
					Singleton<PlaneStorage>.Instance.UpdateHistory();
				}
			}
		}
		if ((Singleton<MouseInput>.Instance.GetPointerIsOverUI(raycastIgnoreObjects) && MouseInput.GetMouseButtonDown(0)) || (MouseInput.GetMouseButtonUp(1) && !partPlacer.buildingCamera.Rotated && !flag))
		{
			SetPaintModeEnabled(value: false);
		}
	}

	private IPaintable GetMirrorPaintable(IPaintable part)
	{
		if (!PartPlacer.mirrorMode || part == null)
		{
			return null;
		}
		if (part.GetType() == typeof(BuildingPart))
		{
			BuildingPart originalPart = (BuildingPart)part;
			return partPlacer.partContainer.GetMirrorPart(originalPart, partPlacer);
		}
		if (part.GetType() == typeof(Decal))
		{
			Decal decal = (Decal)part;
			Decal mirrorDecal = Singleton<DecalContainer>.Instance.GetMirrorDecal(decal, partPlacer);
			if (mirrorDecal != null)
			{
				return mirrorDecal;
			}
		}
		return null;
	}

	private IPaintable GetHoverPart()
	{
		Ray ray = BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition());
		Decal hoverDecal = decalPlacer.GetHoverDecal();
		if (hoverDecal != null)
		{
			lastHoverPartGameObject = hoverDecal.gameObject;
			return hoverDecal;
		}
		if (Physics.Raycast(ray, out var hitInfo))
		{
			BuildingPart buildingPartComponent = PartPlacer.GetBuildingPartComponent(hitInfo.collider.gameObject);
			if (buildingPartComponent != null)
			{
				lastHoverPartGameObject = buildingPartComponent.gameObject;
				return buildingPartComponent;
			}
		}
		return null;
	}

	private void OnDestroy()
	{
		PaintModeEnabled = false;
		if (Singleton<MouseInput>.Instance != null)
		{
			Singleton<MouseInput>.Instance.SetCursor(CursorTypes.Normal);
		}
	}
}
