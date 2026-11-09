using UnityEngine;

public abstract class Quest
{
	public string questID;

	public string questName;

	public string description;

	public string guestGiveMessage;

	public string questCompleteMessage;

	public string questNameOriginal;

	public string questDescirptionOriginal;

	public string questGiveMessageOriginal;

	public string questCompleteMessageOriginal;

	public StoryStateID storyState;

	public Airport questGiver;

	public float reward;

	public int researchPointReward;

	public int advancedReseachPointReward;

	public bool randomGeneratedQuest;

	public Quest followupQuest;

	public bool completed;

	public bool initialized;

	public bool CheckForCompletion()
	{
		if (completed)
		{
			return true;
		}
		if (CheckCompletion())
		{
			completed = true;
			Singleton<QuestAdditionUI>.Instance.DisplayText(questCompleteMessage, MessageType.QuestCompleted);
			CompletedPopupInfo completedPopupInfo = new CompletedPopupInfo();
			completedPopupInfo.completed = true;
			completedPopupInfo.prevMoney = (int)Singleton<MoneyManager>.Instance.money;
			completedPopupInfo.prevScrap = Singleton<ResearchManager>.Instance.researchPoints;
			completedPopupInfo.prevAdvancedScrap = Singleton<ResearchManager>.Instance.advancedResearchPoints;
			Singleton<MoneyManager>.Instance.ChangeMoneyAmount(reward);
			Singleton<ResearchManager>.Instance.researchPoints += researchPointReward;
			Singleton<ResearchManager>.Instance.advancedResearchPoints += advancedReseachPointReward;
			completedPopupInfo.targetAdvancedScrap = Singleton<ResearchManager>.Instance.advancedResearchPoints;
			completedPopupInfo.targetScrap = Singleton<ResearchManager>.Instance.researchPoints;
			completedPopupInfo.targetMoney = (int)Singleton<MoneyManager>.Instance.money;
			Singleton<QuestAdditionUI>.Instance.completedPopupInfos.Add(completedPopupInfo);
			if (followupQuest != null && questGiver != null)
			{
				questGiver.currentQuest = followupQuest;
				questGiver.questInitialized = false;
			}
			Singleton<AchievementManager>.Instance.OnCompleteMission(GetType() == typeof(ContainerQuest));
			return true;
		}
		return false;
	}

	public bool Equals(Quest other)
	{
		if (other.description == description && other.questName == questName)
		{
			return other.questGiver == questGiver;
		}
		return false;
	}

	public virtual void Save(GameDataWriter writer)
	{
		writer.Write(completed);
		writer.Write(initialized);
	}

	public virtual void Load(GameDataReader reader)
	{
		completed = reader.ReadBool();
		initialized = reader.ReadBool();
	}

	protected abstract bool CheckCompletion();

	public abstract Vector3 GetCurrentMarkerPosition();

	public abstract Vector3[] GetRelatedAirportLocations();

	public abstract QuestStage GetCurrentStageDescription();
}
