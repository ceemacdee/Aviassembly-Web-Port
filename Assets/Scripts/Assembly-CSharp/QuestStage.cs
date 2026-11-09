public struct QuestStage
{
	public string description;

	public int stage;

	public QuestStage(string description, int stage)
	{
		this.description = description;
		this.stage = stage;
	}

	public static QuestStage NullStage()
	{
		return new QuestStage("", -1);
	}
}
