using TMPro;
using UnityEngine;

public class PlacementUI : MonoBehaviour
{
	public PartPlacer placer;

	[Space(10f)]
	public TMP_Text moneyText;

	public RotationGizmo rotationGizmo;

	public GameObject moveGizmo;

	public PlacementToolbar toolbar;

	public GameObject rotationSnapSettings;

	public PartSettingsPanel settingsPanel;

	public ProceduralGizmoManager gizmoManager;

	private void Start()
	{
		settingsPanel.gameObject.SetActive(value: false);
		toolbar.rotationClicked += placer.RotatePart;
		toolbar.moveClicked += placer.MovePart;
		toolbar.deleteClicked += placer.DestroySelectedPart;
	}

	private void LateUpdate()
	{
		moneyText.text = Mathf.RoundToInt(Singleton<MoneyManager>.Instance.money).ToString();
		if (Singleton<GameManager>.Instance.gameModeData.creativeMode)
		{
			moneyText.text = "-";
		}
		if (placer.currentSelectedPart != null && placer.currentMovingPart == null && !rotationGizmo.gameObject.activeInHierarchy && !moveGizmo.activeInHierarchy && gizmoManager.currentGizmoMode == GizmoMode.Default)
		{
			toolbar.EnableToolbar(placer.currentSelectedPart.gameObject);
			ProceduralFuselage componentInChildren = placer.currentSelectedPart.GetComponentInChildren<ProceduralFuselage>();
			int num = ((componentInChildren != null) ? 2 : 0);
			if (num > 0 && placer.partContainer.SnappingPointToPart(componentInChildren.p1.position, placer.currentSelectedPart) != null && placer.partContainer.SnappingPointToPart(componentInChildren.p2.position, placer.currentSelectedPart) != null)
			{
				num = 1;
			}
			bool rotateable = !placer.currentSelectedPart.lockRotation;
			PlanePart componentInChildren2 = placer.currentSelectedPart.GetComponentInChildren<PlanePart>();
			bool hasSettings = componentInChildren2.settingsPrefab != null;
			bool hasKeybinds = componentInChildren2.keybindSettingsPrefab != null;
			toolbar.UpdateButtons(num, hasSettings, hasKeybinds, rotateable, moveable: true, isDecal: false);
		}
		if (Input.GetMouseButton(0) && !Singleton<MouseInput>.Instance.PointerIsOverUI)
		{
			settingsPanel.gameObject.SetActive(value: false);
		}
		if (placer.decalPlacer.selectedDecal == null && (placer.currentSelectedPart == null || placer.currentMovingPart != null))
		{
			rotationGizmo.gameObject.SetActive(value: false);
			moveGizmo.gameObject.SetActive(value: false);
		}
		rotationSnapSettings.SetActive(rotationGizmo.gameObject.activeInHierarchy);
	}

	public void OpenSettingsPanel()
	{
		settingsPanel.gameObject.SetActive(value: true);
		settingsPanel.OpenSettings(placer.currentSelectedPart.GetComponentInChildren<PlanePart>(), keybinds: false);
	}

	public void OpenKeybindsPanel()
	{
		settingsPanel.gameObject.SetActive(value: true);
		settingsPanel.OpenSettings(placer.currentSelectedPart.GetComponentInChildren<PlanePart>(), keybinds: true);
	}
}
