using System.Collections.Generic;
using UnityEngine;

public class TempratureManager : MonoBehaviour
{
	public Texture electicityIcon;

	public Texture tempratureIcon;

	private int coolerCount;

	public float energyConsumption;

	private void Start()
	{
		coolerCount = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<CoolingFan>().Length;
	}

	private void ShowElectricityWarning()
	{
		CoolingFan[] componentsInChildren = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<CoolingFan>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Singleton<WarningManager>.Instance.ShowWarning(componentsInChildren[i], electicityIcon);
		}
	}

	private void Update()
	{
		if (Singleton<PlaneContainer>.Instance.FlightModeInitialized)
		{
			CustomMath.SnapToIncrement(GetTempratureAtPlanePosition(), 1f);
		}
	}

	public float GetTempratureAtPlanePosition()
	{
		Vector3 position = Singleton<PlaneContainer>.Instance.transform.position;
		List<ContinentData> continents = Singleton<ContinentManager>.Instance.continents;
		List<float> list = new List<float>();
		float num = 0f;
		for (int i = 0; i < continents.Count; i++)
		{
			float num2 = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(continents[i].origin.x, continents[i].origin.z));
			float num3 = 1f / (num2 + 0.1f);
			list.Add(num3);
			num += num3;
		}
		float num4 = 0f;
		for (int j = 0; j < list.Count; j++)
		{
			float num5 = list[j] / num;
			num4 += (float)continents[j].continentType.temprature * num5;
		}
		num4 -= position.y * 0.05f;
		return Mathf.Clamp(num4, -75f, 42f);
	}
}
