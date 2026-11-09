using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestChildUI : MonoBehaviour
{
	public TMP_Text header;

	public TMP_Text reward;

	public GameObject checkbox;

	public Quest quest;

	public RawImage icon;

	private void Awake()
	{
		checkbox.SetActive(value: false);
	}

	private void Update()
	{
		if (quest != null)
		{
			header.text = quest.questName;
			checkbox.SetActive(quest.completed);
			reward.text = quest.reward.ToString();
			bool flag = quest.GetType() == typeof(DeliveryQuest);
			icon.gameObject.SetActive(flag);
			if (flag)
			{
				icon.texture = ((DeliveryQuest)quest).cargoType.icon;
			}
		}
	}
}
