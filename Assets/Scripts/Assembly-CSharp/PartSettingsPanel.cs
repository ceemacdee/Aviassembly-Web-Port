using UnityEngine;

public class PartSettingsPanel : MonoBehaviour
{
	public PartPlacer partPlacer;

	public float padding;

	public RectTransform content;

	public void Update()
	{
		if (partPlacer.currentSelectedPart == null)
		{
			base.gameObject.SetActive(value: false);
		}
		((RectTransform)base.transform).sizeDelta = content.sizeDelta + new Vector2(padding, padding);
		content.localPosition = new Vector3((0f - ((RectTransform)base.transform).sizeDelta.x) * 0.75f, 0f, 0f);
		if (partPlacer.currentSelectedPart != null)
		{
			Vector3 objectCenter = CustomMath.GetObjectCenter(partPlacer.currentSelectedPart.gameObject);
			base.transform.position = BuildingCamera.cam.WorldToScreenPoint(objectCenter);
		}
	}

	public void OpenSettings(PlanePart part, bool keybinds)
	{
		if (content != null)
		{
			Object.Destroy(content.gameObject);
		}
		GameObject gameObject = Object.Instantiate(keybinds ? part.keybindSettingsPrefab : part.settingsPrefab);
		gameObject.transform.parent = base.transform;
		gameObject.transform.localScale = Vector3.one;
		content = (RectTransform)gameObject.transform;
		content.gameObject.GetComponent<PartSettingsBase>().SelectPart(part);
		Update();
	}
}
