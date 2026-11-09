public class StoryState : Singleton<StoryState>
{
	public StoryStateID currentStoryState;

	public bool postEarthquake;

	public bool earthquakeStarted;

	public bool desertUnlocked;

	public bool snowUnlocked;

	public int mysteriousMessageIndex;

	public int mysteriousMessageIndex2;

	public int mysteriousMessageIndex3;

	public float mysteriousMessageTimer;

	public bool playedEndCutscene;

	public bool finalMessagePlayed;

	public float selectNewQuestTimer;

	public bool unlockAll;

	protected override void Awake()
	{
		base.Awake();
		postEarthquake = false;
		desertUnlocked = false;
		snowUnlocked = false;
		mysteriousMessageIndex = 0;
		mysteriousMessageIndex2 = 0;
		mysteriousMessageIndex3 = 0;
		mysteriousMessageTimer = 70f;
		selectNewQuestTimer = 0f;
	}

	private void Update()
	{
		if (unlockAll)
		{
			postEarthquake = true;
			desertUnlocked = true;
		}
		currentStoryState = StoryStateID.PreEarthquake;
		if (postEarthquake)
		{
			currentStoryState = StoryStateID.PostEarthquake;
		}
		if (snowUnlocked)
		{
			currentStoryState = StoryStateID.SnowUnlocked;
		}
		if (desertUnlocked)
		{
			currentStoryState = StoryStateID.DesertUnlocked;
		}
	}

	public bool QuestAvailable(Quest quest)
	{
		if (quest.storyState == StoryStateID.PostEarthquake && currentStoryState == StoryStateID.PreEarthquake)
		{
			return false;
		}
		if (quest.storyState == StoryStateID.PreEarthquake && currentStoryState == StoryStateID.PostEarthquake)
		{
			return true;
		}
		return quest.storyState <= currentStoryState;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(postEarthquake);
		writer.Write(desertUnlocked);
		writer.Write(mysteriousMessageIndex);
		writer.Write(mysteriousMessageIndex2);
		writer.Write(mysteriousMessageIndex3);
		writer.Write(playedEndCutscene);
		writer.Write(finalMessagePlayed);
		writer.Write(snowUnlocked);
	}

	public void Load(GameDataReader reader)
	{
		if (reader.version < 11)
		{
			return;
		}
		postEarthquake = reader.ReadBool();
		desertUnlocked = reader.ReadBool();
		if (reader.version < 14)
		{
			return;
		}
		mysteriousMessageIndex = reader.ReadInt();
		mysteriousMessageIndex2 = reader.ReadInt();
		mysteriousMessageIndex3 = reader.ReadInt();
		if (reader.version < 15)
		{
			return;
		}
		playedEndCutscene = reader.ReadBool();
		if (reader.version >= 16)
		{
			finalMessagePlayed = reader.ReadBool();
			if (reader.version >= 21)
			{
				snowUnlocked = reader.ReadBool();
			}
		}
	}
}
