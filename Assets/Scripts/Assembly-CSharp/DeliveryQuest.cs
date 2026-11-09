using System.Collections.Generic;
using UnityEngine;

public class DeliveryQuest : Quest
{
	public Airport deliveryAirport;

	public CargoType cargoType;

	public int amount;

	public bool dirty;

	public float rewardPerCargo;

	protected override bool CheckCompletion()
	{
		string text = (string.IsNullOrEmpty(cargoType.cargoName) ? cargoType.name : cargoType.cargoName);
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		bool flag = deliveryAirport.flatnessObject.GetDistanceToRectangle(instance.transform.position) < 100f;
		bool flag2 = instance.GetVelocityMagintude() < 25f;
		bool flag3 = Singleton<CargoInventory>.Instance.GetCargoCount(cargoType) >= amount;
		bool flag4 = flag & flag2 & flag3;
		if (flag4)
		{
			Singleton<CargoInventory>.Instance.RemoveCargo(cargoType, amount);
			UpdateText(completed: true);
		}
		int cargoCount = Singleton<CargoInventory>.Instance.GetCargoCount(cargoType);
		if ((!flag4 && cargoCount > 0) & flag & flag2)
		{
			dirty = true;
			Singleton<CargoInventory>.Instance.RemoveCargo(cargoType, cargoCount);
			float num = Mathf.RoundToInt((float)cargoCount * rewardPerCargo * 0.7f);
			int prevMoney = (int)Singleton<MoneyManager>.Instance.money;
			int researchPoints = Singleton<ResearchManager>.Instance.researchPoints;
			int advancedResearchPoints = Singleton<ResearchManager>.Instance.advancedResearchPoints;
			Singleton<MoneyManager>.Instance.ChangeMoneyAmount(num);
			reward -= num;
			amount -= cargoCount;
			UpdateText(completed: false);
			if (num > 0f)
			{
				string text2 = ((amount > 1) ? amount.ToString() : "one");
				string text3 = "Thank you for the delivery. Here are <b>" + num + "</b> coins for the effort. We only need <b>" + text2.ToLower() + "</b> more " + text.ToLower() + ".";
				CompletedPopupInfo completedPopupInfo = new CompletedPopupInfo();
				completedPopupInfo.completed = false;
				completedPopupInfo.prevMoney = prevMoney;
				completedPopupInfo.prevScrap = researchPoints;
				completedPopupInfo.prevAdvancedScrap = advancedResearchPoints;
				completedPopupInfo.targetScrap = Singleton<ResearchManager>.Instance.researchPoints;
				completedPopupInfo.targetMoney = (int)Singleton<MoneyManager>.Instance.money;
				completedPopupInfo.targetAdvancedScrap = Singleton<ResearchManager>.Instance.advancedResearchPoints;
				Singleton<QuestAdditionUI>.Instance.completedPopupInfos.Add(completedPopupInfo);
				Singleton<QuestAdditionUI>.Instance.DisplayText(text3, MessageType.QuestCompleted);
				Singleton<AchievementManager>.Instance.OnCompleteMission(partial: true);
			}
		}
		return flag4;
	}

	private void UpdateText(bool completed)
	{
		string text = (string.IsNullOrEmpty(cargoType.cargoName) ? cargoType.name : cargoType.cargoName);
		questName = questNameOriginal.Replace("[amount]", " " + amount + " ");
		questName = questName.Replace("[cargoType]", text);
		description = questDescirptionOriginal.Replace("[amount]", amount.ToString());
		description = description.Replace("[cargoType]", text);
		if (!string.IsNullOrEmpty(questCompleteMessageOriginal))
		{
			questCompleteMessage = questCompleteMessageOriginal.Replace("[reward]", Mathf.RoundToInt(reward).ToString());
			questCompleteMessage = questCompleteMessage.Replace("[scrapReward]", Mathf.RoundToInt(researchPointReward).ToString());
			questCompleteMessage = questCompleteMessage.Replace("[advancedScrapReward]", Mathf.RoundToInt(advancedReseachPointReward).ToString());
		}
		else
		{
			questCompleteMessage = "Thank you for the " + text + ". Here are <b> " + reward + " </b>coins for the effort";
		}
		if (completed)
		{
			questName = questNameOriginal.Replace("[amount]", " ");
			questName = questName.Replace("[cargoType]", text);
		}
	}

	public override Vector3 GetCurrentMarkerPosition()
	{
		CargoInventory instance = Singleton<CargoInventory>.Instance;
		int cargoCount = instance.GetCargoCount(cargoType);
		if (cargoCount >= amount || (cargoCount > 0 && !instance.EnoughSpace(cargoType)))
		{
			return deliveryAirport.position;
		}
		return Singleton<AirportManager>.Instance.GetCargoAirport(cargoType).position;
	}

	public override Vector3[] GetRelatedAirportLocations()
	{
		return new List<Vector3> { Singleton<AirportManager>.Instance.GetCargoAirport(cargoType).position }.ToArray();
	}

	public override QuestStage GetCurrentStageDescription()
	{
		CargoInventory instance = Singleton<CargoInventory>.Instance;
		int cargoCount = instance.GetCargoCount(cargoType);
		bool num = cargoCount >= amount || (cargoCount > 0 && !instance.EnoughSpace(cargoType));
		string text = (string.IsNullOrEmpty(cargoType.cargoName) ? cargoType.name : cargoType.cargoName);
		if (num)
		{
			return new QuestStage("Deliver the " + text.ToLower() + " at " + questGiver.airportName.ToLower(), 1);
		}
		return new QuestStage("Collect <b>" + text.ToLower() + "</b> at " + Singleton<AirportManager>.Instance.GetCargoAirport(cargoType).airportName.ToLower(), 0);
	}

	public override void Save(GameDataWriter writer)
	{
		base.Save(writer);
		writer.Write(amount);
		writer.Write(reward);
	}

	public override void Load(GameDataReader reader)
	{
		base.Load(reader);
		if (reader.version > 1)
		{
			amount = reader.ReadInt();
			UpdateText(completed);
		}
		if (reader.version > 3)
		{
			reward = reader.ReadFloat();
			UpdateText(completed);
		}
	}
}
