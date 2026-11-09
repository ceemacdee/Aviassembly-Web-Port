public abstract class QuestData : Node
{
	public string questName;

	public StoryStateID storyState;

	public QuestData followupQuest;

	public string description;

	public string giveMessage;

	public string completeMessage;

	public int researchPointReward;

	public int advancedReseachPointReward;

	public abstract Quest CreateQuest(Airport airport);

	public void InitQuest(Quest quest, Airport airport)
	{
		quest.questID = base.name;
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
		quest.storyState = storyState;
		quest.guestGiveMessage = quest.guestGiveMessage.Replace("[scrapReward]", researchPointReward.ToString());
		quest.description = quest.description.Replace("[scrapReward]", researchPointReward.ToString());
		quest.questCompleteMessage = quest.questCompleteMessage.Replace("[scrapReward]", researchPointReward.ToString());
		quest.guestGiveMessage = quest.guestGiveMessage.Replace("[advancedScrapReward]", advancedReseachPointReward.ToString());
		quest.description = quest.description.Replace("[advancedScrapReward]", advancedReseachPointReward.ToString());
		quest.questCompleteMessage = quest.questCompleteMessage.Replace("[advancedScrapReward]", advancedReseachPointReward.ToString());
		airport.allQuests.Add(quest);
		if (followupQuest != null)
		{
			quest.followupQuest = followupQuest.CreateQuest(airport);
		}
	}
}
