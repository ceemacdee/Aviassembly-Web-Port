using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WarningManager : Singleton<WarningManager>
{
	private class WarningInstance
	{
		public int lastCallFrame;

		public GameObject warning;

		public WarningInstance(int lastCallFrame, GameObject warning)
		{
			this.lastCallFrame = lastCallFrame;
			this.warning = warning;
		}
	}

	public Camera cam;

	public GameObject warningPrefab;

	public Color warningColor;

	public float warningFlashSpeed;

	public Texture tempratureIcon;

	public Texture weightIcon;

	private Dictionary<PlanePart, WarningInstance> warnings = new Dictionary<PlanePart, WarningInstance>();

	public void ShowWarning(PlanePart part, Texture icon = null)
	{
		if (warnings.ContainsKey(part))
		{
			SetWarningPosition(warnings[part].warning, CustomMath.GetObjectCenter(part.transform.gameObject));
			warnings[part].lastCallFrame = Time.frameCount;
			return;
		}
		WarningInstance warningInstance = new WarningInstance(Time.frameCount, CreateWarning());
		SetWarningPosition(warningInstance.warning, part.transform.position);
		warnings.Add(part, warningInstance);
		if (icon == null)
		{
			warningInstance.warning.transform.GetChild(0).GetComponent<RawImage>().texture = tempratureIcon;
		}
		else
		{
			warningInstance.warning.transform.GetChild(0).GetComponent<RawImage>().texture = icon;
		}
	}

	private GameObject CreateWarning()
	{
		GameObject obj = Object.Instantiate(warningPrefab);
		obj.transform.SetParent(base.transform, worldPositionStays: true);
		obj.transform.localScale = Vector3.one;
		return obj;
	}

	private void SetWarningPosition(GameObject warning, Vector3 worldPosition)
	{
		warning.transform.position = cam.WorldToScreenPoint(worldPosition);
	}

	private void FixedUpdate()
	{
		for (int num = warnings.Count - 1; num >= 0; num--)
		{
			KeyValuePair<PlanePart, WarningInstance> keyValuePair = warnings.ElementAt(num);
			if (keyValuePair.Value.lastCallFrame < Time.frameCount - 1)
			{
				warnings.Remove(keyValuePair.Key);
				Object.Destroy(keyValuePair.Value.warning);
			}
		}
	}

	private void Update()
	{
		for (int num = warnings.Count - 1; num >= 0; num--)
		{
			KeyValuePair<PlanePart, WarningInstance> keyValuePair = warnings.ElementAt(num);
			Color color = Color.Lerp(Color.clear, warningColor, ((Mathf.Sin(Time.time * warningFlashSpeed) + 1f) * 0.5f > 0.5f) ? 1 : 0);
			Singleton<HighlightRenderer>.Instance.AddHighlightObject(PartPlacer.GetBuildingPartComponent(keyValuePair.Key.gameObject).gameObject, color);
		}
	}
}
