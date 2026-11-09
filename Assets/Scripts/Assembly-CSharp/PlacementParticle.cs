using System;
using TMPro;
using UnityEngine;

public class PlacementParticle : MonoBehaviour
{
	public GameObject coinsParent;

	public GameObject cargoParent;

	public TMP_Text coinsText;

	public TMP_Text cargoText;

	public float riseSpeed;

	public float riseHeight;

	[Range(0f, 1f)]
	public float fadeFreePercentage;

	public CanvasGroup group;

	private Vector3 origin;

	private Vector3 offset;

	public void Init(float coins, float cargo, GameObject placedObject)
	{
		int num = Mathf.RoundToInt(coins);
		float num2 = (float)Math.Round(cargo, 1);
		coinsParent.SetActive(num != 0);
		cargoParent.SetActive(num2 != 0f);
		coinsText.text = ValueToText(num);
		cargoText.text = ValueToText(num2);
		coinsText.color = ValueToColor(num);
		cargoText.color = ValueToColor(num2);
		SetPosition(CustomMath.GetObjectCenter(placedObject));
	}

	public void SetPosition(Vector3 worldPos)
	{
		base.transform.position = BuildingCamera.cam.WorldToScreenPoint(worldPos);
		origin = worldPos;
	}

	private string ValueToText(float value)
	{
		if (value > 0f)
		{
			return "+ " + Mathf.Abs(value);
		}
		return "- " + Mathf.Abs(value);
	}

	private Color ValueToColor(float value)
	{
		if (value > 0f)
		{
			return Color.white;
		}
		return Color.red;
	}

	private void Update()
	{
		offset += Vector3.up * Time.unscaledDeltaTime * riseSpeed;
		base.transform.position = BuildingCamera.cam.WorldToScreenPoint(origin) + offset;
		float num = riseHeight - riseHeight * fadeFreePercentage;
		float num2 = riseHeight - num;
		float num3 = 1f - (offset.magnitude - num) / num2;
		group.alpha = num3;
		if (num3 < 0f)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void DestroyParticle()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
