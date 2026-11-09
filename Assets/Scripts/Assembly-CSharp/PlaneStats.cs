using System;
using TMPro;
using UnityEngine;

public class PlaneStats : MonoBehaviour
{
	public TMP_Text weightText;

	public TMP_Text fuelText;

	public TMP_Text cargoText;

	public float cargoSpace;

	private PlaneContainer planeContainer;

	private void Awake()
	{
		weightText.text = 0.ToString();
		fuelText.text = 0.ToString();
		cargoText.text = 0.ToString();
	}

	private void Update()
	{
		if (planeContainer == null)
		{
			planeContainer = Singleton<PlaneContainer>.Instance;
			return;
		}
		PlanePart[] componentsInChildren = planeContainer.GetComponentsInChildren<PlanePart>(includeInactive: true);
		float num = 0f;
		float num2 = 1f;
		float num3 = 0f;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (GetBuildingPartComponent(componentsInChildren[i].gameObject).hasBeenPlaced)
			{
				if (componentsInChildren[i].GetType() == typeof(FuelTank))
				{
					num2 += Mathf.Abs(((FuelTank)componentsInChildren[i]).volume);
				}
				if (componentsInChildren[i].GetType() == typeof(Wing))
				{
					num2 += Mathf.Abs(((Wing)componentsInChildren[i]).fuel);
				}
				if (componentsInChildren[i].GetType() == typeof(Fuselage))
				{
					num3 += Mathf.Abs(((Fuselage)componentsInChildren[i]).cargoVolume);
				}
				num += componentsInChildren[i].weight;
			}
		}
		weightText.text = Math.Round(num, 1).ToString();
		fuelText.text = Math.Round(num2, 1).ToString();
		cargoText.text = Mathf.RoundToInt(num3).ToString();
		cargoSpace = Mathf.RoundToInt(num3);
	}

	private BuildingPart GetBuildingPartComponent(GameObject obj)
	{
		BuildingPart buildingPart = obj.GetComponent<BuildingPart>();
		if (buildingPart == null && obj.transform.parent != null)
		{
			buildingPart = obj.transform.parent.gameObject.GetComponent<BuildingPart>();
		}
		if (buildingPart == null)
		{
			buildingPart = obj.GetComponentInChildren<BuildingPart>();
		}
		return buildingPart;
	}
}
