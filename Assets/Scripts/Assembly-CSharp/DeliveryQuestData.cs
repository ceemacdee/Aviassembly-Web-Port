public class DeliveryQuestData : QuestData
{
	public CargoType cargoType;

	public int amount;

	public float rewardOffset;

	public override Quest CreateQuest(Airport airport)
	{
		DeliveryQuest deliveryQuest = new DeliveryQuest();
		deliveryQuest.deliveryAirport = airport;
		deliveryQuest.cargoType = cargoType;
		deliveryQuest.amount = amount;
		InitQuest(deliveryQuest, airport);
		AutoGenerateContent(deliveryQuest, airport);
		deliveryQuest.rewardPerCargo = deliveryQuest.reward / (float)amount;
		deliveryQuest.questName = deliveryQuest.questName.Replace("[amount]", " " + amount + " ");
		deliveryQuest.guestGiveMessage = deliveryQuest.guestGiveMessage.Replace("[amount]", amount.ToString());
		deliveryQuest.questCompleteMessage = deliveryQuest.questCompleteMessage.Replace("[amount]", amount.ToString());
		deliveryQuest.description = deliveryQuest.description.Replace("[amount]", amount.ToString());
		string newValue = (string.IsNullOrEmpty(cargoType.cargoName) ? cargoType.name : cargoType.cargoName);
		deliveryQuest.questName = deliveryQuest.questName.Replace("[cargoType]", newValue);
		deliveryQuest.guestGiveMessage = deliveryQuest.guestGiveMessage.Replace("[cargoType]", newValue);
		deliveryQuest.questCompleteMessage = deliveryQuest.questCompleteMessage.Replace("[cargoType]", newValue);
		deliveryQuest.description = deliveryQuest.description.Replace("[cargoType]", newValue);
		return deliveryQuest;
	}

	private void AutoGenerateContent(Quest quest, Airport airport)
	{
		float num = 1f;
		if (Singleton<GameManager>.Instance.gameModeData.currentDifficulty == Difficulty.Relaxed)
		{
			num = 1.15f;
		}
		if (Singleton<GameManager>.Instance.gameModeData.currentDifficulty == Difficulty.Hard)
		{
			num = 0.66f;
		}
		quest.reward = CustomMath.SnapToIncrement((airport.GetPrice(cargoType) * (float)amount + rewardOffset) * num, 5f);
		if (StringIsEmpty(questName))
		{
			quest.questName = "Delivering " + cargoType.name;
		}
		if (StringIsEmpty(giveMessage))
		{
			quest.guestGiveMessage = "Welcome to " + airport.airportName + ". We need <b>" + amount + " " + cargoType.name + "</b>.";
		}
		if (StringIsEmpty(description))
		{
			quest.description = "Deliver " + amount + " " + cargoType.name + " to this airport";
		}
		if (StringIsEmpty(completeMessage))
		{
			quest.questCompleteMessage = "Thank you for the " + cargoType.name + ". Here are <b> " + quest.reward + " </b>coins for the effort";
		}
	}

	private bool StringIsEmpty(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return string.IsNullOrWhiteSpace(value);
		}
		return true;
	}
}
