using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContractPanel : MonoBehaviour
{
	public Transform childQuestParent;

	public GameObject childQuestPrefab;

	public GameObject mainQuestIndicator;

	public GameObject explosion;

	public bool autoFindAiport;

	public GameObject randomQuestIcon;

	private List<GameObject> childQuestUI = new List<GameObject>();

	public TMP_Text header;

	public TMP_Text description;

	public TMP_Text airportName;

	public TMP_Text airportNameDropshadow;

	public RawImage[] cargoIcons;

	public GameObject cargoIconsParent;

	public RawImage descriptioIcon;

	public RectTransform compoundParent;

	public TMP_Text reward;

	public TMP_Text researchReward;

	public TMP_Text advancedResearchReward;

	public GameObject rewaredParent;

	public GameObject questText;

	public TMP_Text refuelText;

	public Image refuelBackground;

	public Color green;

	public Color red;

	public Map map;

	private void Update()
	{
		if (autoFindAiport)
		{
			Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(Singleton<PlaneContainer>.Instance.transform.position);
			SelectAirport(closestAirport);
		}
	}

	public void SelectAirport(Airport airport)
	{
		float num = 350f;
		description.transform.parent.gameObject.SetActive(value: false);
		rewaredParent.gameObject.SetActive(value: false);
		questText.gameObject.SetActive(value: false);
		RefreshChildQuestsButtons(null);
		randomQuestIcon.SetActive(airport.currentQuest != null && airport.questInitialized && !airport.currentQuest.completed && airport.currentQuest.randomGeneratedQuest);
		if (airport.currentQuest != null && (airport.questInitialized || !map.useFogOfWar) && !airport.currentQuest.completed)
		{
			SelectQuest(airport.currentQuest);
		}
		else
		{
			num -= 250f;
		}
		airportName.text = airport.airportName;
		airportNameDropshadow.text = airport.airportName;
		int num2 = 0;
		for (int i = 0; i < cargoIcons.Length; i++)
		{
			cargoIcons[i].gameObject.SetActive(value: false);
		}
		if (airport.cargoType != null)
		{
			for (int j = 0; j < airport.cargoType.Length; j++)
			{
				cargoIcons[j].gameObject.SetActive(value: true);
				cargoIcons[j].texture = airport.cargoType[j].icon;
				num2++;
			}
		}
		if (num2 == 0)
		{
			num -= 60f;
		}
		mainQuestIndicator.SetActive(value: false);
		if (mainQuestIndicator.activeInHierarchy)
		{
			num += 50f;
		}
		cargoIconsParent.gameObject.SetActive(airport.cargoType != null && airport.cargoType.Length != 0);
		bool flag = airport.currentQuest != null && airport.currentQuest.GetType() == typeof(DeliveryQuest);
		descriptioIcon.gameObject.SetActive(flag);
		if (flag)
		{
			descriptioIcon.texture = ((DeliveryQuest)airport.currentQuest).cargoType.icon;
		}
		bool flag2 = airport.data == null || airport.data.refuelAvailable;
		refuelBackground.color = (flag2 ? green : red);
		refuelText.text = (flag2 ? "Refuel Available" : "Refuel Unavailable");
		int childQuestCount = GetChildQuestCount(airport.currentQuest);
		compoundParent.sizeDelta = new Vector2(compoundParent.sizeDelta.x, 30 + 100 * childQuestCount);
		if (airport.currentQuest != null && airport.currentQuest.reward == 0f && airport.currentQuest.advancedReseachPointReward == 0 && airport.currentQuest.researchPointReward == 0)
		{
			num -= 70f;
		}
		((RectTransform)base.transform).sizeDelta = new Vector2(((RectTransform)base.transform).sizeDelta.x, num + compoundParent.sizeDelta.y);
	}

	private bool QuestInitialized(Quest quest)
	{
		if (!quest.questGiver.questInitialized)
		{
			return !map.useFogOfWar;
		}
		return true;
	}

	private int GetChildQuestCount(Quest quest)
	{
		if (quest == null || !QuestInitialized(quest) || quest.completed)
		{
			return 0;
		}
		if (quest == null || !(quest.GetType() == typeof(ContainerQuest)))
		{
			return 0;
		}
		return ((ContainerQuest)quest).childQuests.Count;
	}

	private void RefreshChildQuestsButtons(Quest quest)
	{
		int count = childQuestUI.Count;
		int num = GetChildQuestCount(quest) - count;
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				GameObject item = Object.Instantiate(childQuestPrefab, childQuestParent);
				childQuestUI.Add(item);
			}
		}
		if (num < 0)
		{
			for (int j = 0; j < Mathf.Abs(num); j++)
			{
				GameObject obj = childQuestUI[childQuestUI.Count - 1].gameObject;
				childQuestUI.RemoveAt(childQuestUI.Count - 1);
				Object.Destroy(obj);
			}
		}
	}

	public void SelectQuest(Quest quest)
	{
		RefreshChildQuestsButtons(quest);
		if (quest.GetType() == typeof(ContainerQuest))
		{
			ContainerQuest containerQuest = (ContainerQuest)quest;
			for (int i = 0; i < containerQuest.childQuests.Count; i++)
			{
				childQuestUI[i].GetComponent<QuestChildUI>().quest = containerQuest.childQuests[i];
			}
		}
		explosion.gameObject.SetActive(quest.questName == "The Explosion");
		description.transform.parent.gameObject.SetActive(value: true);
		rewaredParent.gameObject.SetActive(value: true);
		questText.gameObject.SetActive(value: true);
		header.text = quest.questName;
		description.text = quest.description;
		reward.text = quest.reward.ToString();
		reward.transform.parent.gameObject.SetActive(quest.reward != 0f);
		researchReward.text = quest.researchPointReward.ToString();
		researchReward.transform.parent.gameObject.SetActive((float)quest.researchPointReward != 0f);
		advancedResearchReward.text = quest.advancedReseachPointReward.ToString();
		advancedResearchReward.transform.parent.gameObject.SetActive((float)quest.advancedReseachPointReward != 0f);
	}
}
