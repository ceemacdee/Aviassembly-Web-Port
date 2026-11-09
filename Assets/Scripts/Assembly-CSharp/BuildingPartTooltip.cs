using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPartTooltip : MonoBehaviour
{
	public TMP_Text nameHeader;

	public TMP_Text price;

	public PartStatUI weight;

	public RawImage priceIcon;

	public static PartUIData lastHoveredPart;

	public static bool researchButton;

	public GameObject priceObject;

	public GameObject statPrefab;

	public Transform statParent;

	private List<GameObject> statPrefabs = new List<GameObject>();

	private void Update()
	{
		if (lastHoveredPart == null)
		{
			return;
		}
		priceObject.gameObject.SetActive(lastHoveredPart.showPrice);
		((RectTransform)base.transform).sizeDelta = new Vector2(((RectTransform)base.transform).sizeDelta.x, 160 + statPrefabs.Count * 65 - ((!lastHoveredPart.showPrice) ? 35 : 0));
		nameHeader.text = lastHoveredPart.partName;
		float requiredAmount = lastHoveredPart.part.price;
		price.text = requiredAmount.ToString();
		price.color = ((Singleton<MoneyManager>.Instance.HasEnoughMoney(requiredAmount) || researchButton) ? Color.black : Color.red);
		priceIcon.color = price.color;
		float num = 0f;
		PlanePart[] componentsInChildren = lastHoveredPart.part.GetComponentsInChildren<PlanePart>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			num += componentsInChildren[i].weight;
		}
		weight.SetValue("Weight", Math.Round(num, 2).ToString());
		for (int num2 = statPrefabs.Count - 1; num2 >= 0; num2--)
		{
			UnityEngine.Object.Destroy(statPrefabs[num2]);
			statPrefabs.RemoveAt(num2);
		}
		PartStat[] partStats = lastHoveredPart.part.GetComponentInChildren<PlanePart>(includeInactive: true).GetPartStats();
		if (partStats != null)
		{
			for (int j = 0; j < partStats.Length; j++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(statPrefab);
				gameObject.transform.SetParent(statParent, worldPositionStays: true);
				gameObject.transform.localScale = Vector3.one;
				gameObject.GetComponent<PartStatUI>().SetValue(partStats[j].statName, partStats[j].statValue);
				statPrefabs.Add(gameObject);
			}
		}
	}
}
