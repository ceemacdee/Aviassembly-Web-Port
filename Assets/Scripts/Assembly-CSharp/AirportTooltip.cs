using System.Collections.Generic;
using UnityEngine;

public class AirportTooltip : MonoBehaviour
{
	public GameObject prefab;

	private List<CargoTooltipElement> elements = new List<CargoTooltipElement>();

	private void Start()
	{
		CargoType[] allCargoTypes = Singleton<AirportManager>.Instance.allCargoTypes;
		for (int i = 0; i < allCargoTypes.Length; i++)
		{
			GameObject obj = Object.Instantiate(prefab);
			obj.transform.parent = base.transform;
			obj.transform.localScale = Vector3.one;
			CargoTooltipElement component = obj.GetComponent<CargoTooltipElement>();
			component.Init(allCargoTypes[i].icon, 0f);
			elements.Add(component);
		}
	}

	private void Update()
	{
		if (AirportMapIcon.tooltipData.prices != null)
		{
			for (int i = 0; i < AirportMapIcon.tooltipData.prices.Length; i++)
			{
				elements[i].SetValue(AirportMapIcon.tooltipData.prices[i]);
			}
		}
	}
}
