using System.Collections.Generic;
using UnityEngine;

public class ContainerQuest : Quest
{
	public List<Quest> childQuests = new List<Quest>();

	protected override bool CheckCompletion()
	{
		bool result = true;
		for (int i = 0; i < childQuests.Count; i++)
		{
			if (!childQuests[i].CheckForCompletion())
			{
				result = false;
			}
		}
		return result;
	}

	public override Vector3 GetCurrentMarkerPosition()
	{
		for (int i = 0; i < childQuests.Count; i++)
		{
			if (!childQuests[i].CheckForCompletion())
			{
				return childQuests[i].GetCurrentMarkerPosition();
			}
		}
		return Vector3.zero;
	}

	public override Vector3[] GetRelatedAirportLocations()
	{
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < childQuests.Count; i++)
		{
			if (childQuests[i].GetType() == typeof(DeliveryQuest))
			{
				DeliveryQuest deliveryQuest = (DeliveryQuest)childQuests[i];
				list.Add(Singleton<AirportManager>.Instance.GetCargoAirport(deliveryQuest.cargoType).position);
			}
		}
		return list.ToArray();
	}

	public override QuestStage GetCurrentStageDescription()
	{
		for (int i = 0; i < childQuests.Count; i++)
		{
			if (!childQuests[i].CheckForCompletion())
			{
				QuestStage currentStageDescription = childQuests[i].GetCurrentStageDescription();
				return new QuestStage(currentStageDescription.description, currentStageDescription.stage + i * 1000);
			}
		}
		return QuestStage.NullStage();
	}
}
