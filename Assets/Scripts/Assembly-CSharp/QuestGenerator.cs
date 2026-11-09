using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestGenerator : Singleton<QuestGenerator>
{
	private CargoType[] allCargoTypes;

	private float gravity;

	private int currentGenerationIDCount;

	private int totalRewardGenerated;

	private List<ContainerQuest> generatedQuests = new List<ContainerQuest>();

	private List<PlaneDescription> usedDesigns = new List<PlaneDescription>();

	private void Start()
	{
		allCargoTypes = Singleton<AirportManager>.Instance.allCargoTypes;
		gravity = Singleton<PlaneContainer>.Instance.RealGravity;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		for (int i = 0; i < allCargoTypes.Length; i++)
		{
			float cargoWeight = GetCargoWeight(allCargoTypes[i]);
			if (allCargoTypes[i].expires)
			{
				num3 += cargoWeight;
				num4 += allCargoTypes[i].basePrice;
			}
			else
			{
				num += cargoWeight;
				num2 += allCargoTypes[i].basePrice;
			}
		}
	}

	public void CacheCurrentDesign()
	{
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		List<GameObject> allPrefabs = PartPrefabs.GetAllPrefabs();
		Wing[] componentsInChildren = instance.gameObject.GetComponentsInChildren<Wing>();
		List<Wing> list = new List<Wing>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			for (int j = 0; j < allPrefabs.Count; j++)
			{
				if (componentsInChildren[i].name.Replace("(Clone)", "").Trim() == allPrefabs[j].name)
				{
					list.Add(allPrefabs[j].GetComponent<Wing>());
				}
			}
		}
		Engine[] componentsInChildren2 = instance.gameObject.GetComponentsInChildren<Engine>();
		List<Engine> list2 = new List<Engine>();
		for (int k = 0; k < componentsInChildren2.Length; k++)
		{
			for (int l = 0; l < allPrefabs.Count; l++)
			{
				if (componentsInChildren2[k].name.Replace("(Clone)", "").Trim() == allPrefabs[l].name)
				{
					list2.Add(allPrefabs[l].GetComponent<Engine>());
				}
			}
		}
		FuelTank[] componentsInChildren3 = instance.gameObject.GetComponentsInChildren<FuelTank>();
		List<FuelTank> list3 = new List<FuelTank>();
		for (int m = 0; m < componentsInChildren3.Length; m++)
		{
			for (int n = 0; n < allPrefabs.Count; n++)
			{
				if (componentsInChildren3[m].name.Replace("(Clone)", "").Trim() == allPrefabs[n].name)
				{
					list3.Add(allPrefabs[n].GetComponent<FuelTank>());
				}
			}
		}
		componentsInChildren = list.ToArray();
		componentsInChildren2 = list2.ToArray();
		componentsInChildren3 = list3.ToArray();
		float mass = instance.GetMass();
		float liniarDamping = instance.GetLiniarDamping();
		float planeCost = instance.GetComponent<PlaneStorage>().GetPlaneCost();
		float cargoVolume = instance.GetCargoVolume();
		mass -= cargoVolume * 0.1f / 15f;
		planeCost -= cargoVolume * 5f;
		PlaneDescription item = new PlaneDescription(planeCost, mass, liniarDamping, componentsInChildren2, componentsInChildren, componentsInChildren3);
		usedDesigns.Add(item);
	}

	private bool CanCompleteQuest(CargoType cargoType, int amount, Airport airport, QuestDifficulty questDifficulty)
	{
		float num = Vector3.Distance(Singleton<AirportManager>.Instance.GetCargoAirport(cargoType).position, airport.position);
		if (!cargoType.expires)
		{
			num = Mathf.Min(6500f, num);
		}
		float num2 = Singleton<MoneyManager>.Instance.money + Singleton<PlaneContainer>.Instance.GetComponent<PlaneStorage>().GetPlaneCost();
		float num3 = questDifficulty switch
		{
			QuestDifficulty.Medium => num2 * 1.2f, 
			QuestDifficulty.Easy => num2, 
			_ => num2 * 1.55f, 
		};
		float num4 = cargoType.weight / 15f * (float)amount;
		float num5 = cargoType.cargoSpace * amount;
		float num6 = num4 + num5 * 0.1f / 15f;
		float num7 = num5 * 5f;
		float num8 = -1f;
		if (cargoType.expires)
		{
			num8 = num / cargoType.expirationTime;
		}
		for (int num9 = usedDesigns.Count - 1; num9 >= 0; num9--)
		{
			PlaneDescription planeDescription = usedDesigns[num9];
			planeDescription.mass += num6;
			planeDescription.value += num7;
			PlaneStatData planeStatData = EstimateStats(planeDescription);
			if (planeStatData.airtime * planeStatData.speed > num && planeStatData.speed > num8 && num3 >= planeDescription.value)
			{
				return true;
			}
		}
		return false;
	}

	private PlaneStatData EstimateStats(PlaneDescription planeDescription)
	{
		PlaneStatData result = new PlaneStatData
		{
			speed = -1f,
			airtime = -1f
		};
		int num = planeDescription.engines.Length;
		int num2 = planeDescription.wings.Length;
		int num3 = planeDescription.fuelTanks.Length;
		float num4 = 1f;
		for (int i = 0; i < num3; i++)
		{
			num4 += planeDescription.fuelTanks[i].volume;
		}
		float num5 = 0f;
		for (int j = 0; j < num; j++)
		{
			num5 += planeDescription.engines[j].fuelConsumption;
		}
		float airtime = num4 / num5;
		for (float num6 = 0.5f; num6 < 15f; num6 += 0.5f)
		{
			float num7 = 0f;
			for (int k = 0; k < num; k++)
			{
				num7 += planeDescription.engines[k].GetThrustAtSpeed(2000f);
			}
			float num8 = num7 / (planeDescription.mass * planeDescription.linearDamping);
			for (int l = 0; l < 5; l++)
			{
				num7 = 0f;
				for (int m = 0; m < num; m++)
				{
					num7 += planeDescription.engines[m].GetThrustAtSpeed(num8);
				}
				float num9 = num7 * Mathf.Cos(num6 * (MathF.PI / 180f));
				float num10 = 0f;
				for (int n = 0; n < num2; n++)
				{
					num10 += planeDescription.wings[n].GetDragForce(num8, num6);
				}
				num8 = Mathf.Lerp((num9 - num10) / (planeDescription.mass * planeDescription.linearDamping), num8, Mathf.Min(0.5f, l));
			}
			float num11 = 0f;
			float num12 = num7 - num7 * Mathf.Cos(num6 * (MathF.PI / 180f));
			for (int num13 = 0; num13 < num2; num13++)
			{
				num11 += planeDescription.wings[num13].GetLiftForce(num8, num6, Singleton<PlaneContainer>.Instance);
			}
			if (num11 + num12 > gravity * planeDescription.mass)
			{
				result.airtime = airtime;
				result.speed = num8;
				return result;
			}
		}
		return result;
	}

	public ContainerQuest GenerateQuest(Airport airport)
	{
		List<DeliveryQuest> list = new List<DeliveryQuest>();
		int num = UnityEngine.Random.Range(1, 4);
		num = 3;
		if (UnityEngine.Random.Range(0, 2) == 0 && num > 1)
		{
			num--;
		}
		List<CargoType> list2 = new List<CargoType>();
		int num2 = 3 - num - 1;
		for (int i = 0; i < num; i++)
		{
			Tuple<CargoType, int> deliverySpecification = GetDeliverySpecification((QuestDifficulty)Mathf.Max(0, num2 + i), airport, list2);
			list.Add(CreateDeliveryQuest(deliverySpecification.Item1, deliverySpecification.Item2, airport));
			list2.Add(deliverySpecification.Item1);
		}
		ContainerQuest containerQuest = CreateContainerQuest(list.ToArray(), airport);
		containerQuest.questID = GetQuestID();
		for (int j = 0; j < num; j++)
		{
			list[j].questID = GetQuestID();
		}
		return containerQuest;
	}

	private Tuple<CargoType, int> GetDeliverySpecification(QuestDifficulty questDifficulty, Airport airport, List<CargoType> ignoreTypes)
	{
		List<CargoType> list = new List<CargoType>();
		for (int i = 0; i < allCargoTypes.Length; i++)
		{
			if ((airport.cargoType == null || airport.cargoType.Length == 0 || !new List<CargoType>(airport.cargoType).Contains(allCargoTypes[i])) && !ignoreTypes.Contains(allCargoTypes[i]) && CanCompleteQuest(allCargoTypes[i], 1, airport, questDifficulty))
			{
				list.Add(allCargoTypes[i]);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		CargoType cargoType = list[UnityEngine.Random.Range(0, list.Count)];
		int item = 1;
		for (int j = 1; j < 1000; j++)
		{
			if (!CanCompleteQuest(cargoType, j, airport, questDifficulty))
			{
				item = Mathf.Max(1, j - 1);
				break;
			}
		}
		return new Tuple<CargoType, int>(cargoType, item);
	}

	private ContainerQuest CreateContainerQuest(DeliveryQuest[] subQuests, Airport airport)
	{
		ContainerQuest containerQuest = new ContainerQuest();
		containerQuest.randomGeneratedQuest = true;
		for (int i = 0; i < subQuests.Length; i++)
		{
			containerQuest.childQuests.Add(subQuests[i]);
		}
		int researchPointReward = 5 * subQuests.Length;
		int advancedReseachPointReward = 0;
		if (subQuests.Length == 3)
		{
			advancedReseachPointReward = 5;
		}
		string questName = "Random quest";
		string description = "Randomly generated quest";
		string giveMessage = "Can you deliver some cargo here?";
		string completeMessage = "Thank you, here are some coins for the effort";
		InitQuest(containerQuest, airport, questName, description, giveMessage, completeMessage, researchPointReward, advancedReseachPointReward);
		generatedQuests.Add(containerQuest);
		return containerQuest;
	}

	private float GetCargoWeight(CargoType cargoType)
	{
		float num = (float)cargoType.cargoSpace * 0.1f;
		float weight = cargoType.weight;
		return num + weight;
	}

	private float GetReward(CargoType cargoType, int amount, Airport airport)
	{
		float num = airport.GetPrice(cargoType);
		if (cargoType.expires)
		{
			num *= 0.7f;
		}
		float b = GetCargoWeight(cargoType) * (float)(cargoType.expires ? 20 : 8);
		return Mathf.Lerp(num, b, 0.5f) * Mathf.Pow(amount, 0.7f);
	}

	private DeliveryQuest CreateDeliveryQuest(CargoType cargoType, int amount, Airport airport)
	{
		DeliveryQuest deliveryQuest = new DeliveryQuest();
		deliveryQuest.deliveryAirport = airport;
		deliveryQuest.randomGeneratedQuest = true;
		deliveryQuest.cargoType = cargoType;
		deliveryQuest.amount = amount;
		deliveryQuest.reward = CustomMath.SnapToIncrement(GetReward(cargoType, amount, airport), 5f);
		deliveryQuest.rewardPerCargo = deliveryQuest.reward / (float)amount;
		totalRewardGenerated++;
		string questName = "Deliver[amount][cargoType]";
		string completeMessage = "Thank you for the " + cargoType.GetCargoName() + ". Here are <b> " + deliveryQuest.reward + " </b>coins for the effort";
		InitQuest(deliveryQuest, airport, questName, "", "", completeMessage, 0, 0);
		deliveryQuest.questName = deliveryQuest.questName.Replace("[amount]", " " + amount + " ");
		deliveryQuest.questName = deliveryQuest.questName.Replace("[cargoType]", cargoType.GetCargoName());
		return deliveryQuest;
	}

	private void InitQuest(Quest quest, Airport airport, string questName, string description, string giveMessage, string completeMessage, int researchPointReward, int advancedReseachPointReward)
	{
		quest.questName = questName;
		quest.description = description;
		quest.guestGiveMessage = giveMessage;
		quest.questCompleteMessage = completeMessage;
		quest.questNameOriginal = questName;
		quest.questDescirptionOriginal = description;
		quest.questGiveMessageOriginal = giveMessage;
		quest.questCompleteMessageOriginal = completeMessage;
		quest.researchPointReward = researchPointReward;
		quest.advancedReseachPointReward = advancedReseachPointReward;
		quest.questGiver = airport;
		quest.storyState = StoryStateID.PostEarthquake;
		quest.guestGiveMessage = quest.guestGiveMessage.Replace("[scrapReward]", researchPointReward.ToString());
		quest.description = quest.description.Replace("[scrapReward]", researchPointReward.ToString());
		quest.questCompleteMessage = quest.questCompleteMessage.Replace("[scrapReward]", researchPointReward.ToString());
		quest.guestGiveMessage = quest.guestGiveMessage.Replace("[advancedScrapReward]", advancedReseachPointReward.ToString());
		quest.description = quest.description.Replace("[advancedScrapReward]", advancedReseachPointReward.ToString());
		quest.questCompleteMessage = quest.questCompleteMessage.Replace("[advancedScrapReward]", advancedReseachPointReward.ToString());
		airport.allQuests.Add(quest);
	}

	private string GetQuestID()
	{
		currentGenerationIDCount++;
		return UnityEngine.Random.Range(0, -1000000) + "-" + generatedQuests.Count + "-" + currentGenerationIDCount;
	}

	public ContainerQuest TryGetQuest(string questID)
	{
		for (int i = 0; i < generatedQuests.Count; i++)
		{
			if (generatedQuests[i].questID == questID)
			{
				return generatedQuests[i];
			}
		}
		return null;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(generatedQuests.Count);
		for (int i = 0; i < generatedQuests.Count; i++)
		{
			writer.Write(generatedQuests[i].childQuests.Count);
			writer.Write(generatedQuests[i].questGiver.position);
			writer.Write(generatedQuests[i].questID);
			for (int j = 0; j < generatedQuests[i].childQuests.Count; j++)
			{
				DeliveryQuest deliveryQuest = (DeliveryQuest)generatedQuests[i].childQuests[j];
				writer.Write(deliveryQuest.questID);
				writer.Write(deliveryQuest.cargoType.name);
				writer.Write(deliveryQuest.amount);
			}
		}
		writer.Write(usedDesigns.Count);
		for (int k = 0; k < usedDesigns.Count; k++)
		{
			usedDesigns[k].Save(writer);
		}
	}

	public void Load(GameDataReader reader)
	{
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			int num2 = reader.ReadInt();
			Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(reader.ReadVector3());
			string questID = reader.ReadString();
			List<DeliveryQuest> list = new List<DeliveryQuest>();
			for (int j = 0; j < num2; j++)
			{
				string questID2 = reader.ReadString();
				CargoType cargoType = TryGetCargoType(reader.ReadString());
				int amount = reader.ReadInt();
				DeliveryQuest deliveryQuest = CreateDeliveryQuest(cargoType, amount, closestAirport);
				deliveryQuest.questID = questID2;
				list.Add(deliveryQuest);
			}
			ContainerQuest containerQuest = CreateContainerQuest(list.ToArray(), closestAirport);
			containerQuest.questID = questID;
			containerQuest.randomGeneratedQuest = true;
			generatedQuests.Add(containerQuest);
		}
		if (reader.version > 26)
		{
			int num3 = reader.ReadInt();
			for (int k = 0; k < num3; k++)
			{
				usedDesigns.Add(PlaneDescription.Load(reader));
			}
		}
	}

	private CargoType TryGetCargoType(string cargoType)
	{
		CargoType[] array = Singleton<AirportManager>.Instance.allCargoTypes;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].name == cargoType)
			{
				return array[i];
			}
		}
		return array[0];
	}
}
