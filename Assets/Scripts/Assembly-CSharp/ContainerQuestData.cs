public class ContainerQuestData : QuestData
{
	public QuestData[] subQuests;

	public override Quest CreateQuest(Airport airport)
	{
		ContainerQuest containerQuest = new ContainerQuest();
		InitQuest(containerQuest, airport);
		for (int i = 0; i < subQuests.Length; i++)
		{
			if (subQuests[i] != null)
			{
				containerQuest.childQuests.Add(subQuests[i].CreateQuest(airport));
			}
		}
		return containerQuest;
	}
}
