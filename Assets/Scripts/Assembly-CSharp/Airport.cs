using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class Airport
{
	public string id;

	public CargoType[] cargoType;

	public bool IsBaseAirport;

	public Quest currentQuest;

	public List<Quest> allQuests = new List<Quest>();

	public AirportData data;

	public bool cargoLocked;

	public string airportName;

	public Texture collisionMap;

	public bool questBlocked;

	public bool questInitialized;

	private AirportManager manager;

	private bool hospitalMessagePlayed;

	public string landingMessage;

	public FlatnessRectangle flatnessObject;

	public FlatnessRectangle treeSpawningFootprint;

	public Vector3 position { get; private set; }

	public Vector3 spawnPoint { get; private set; }

	public Vector3 spawnDirection { get; private set; }

	public void Init(Vector3 position)
	{
		this.position = position;
		manager = Singleton<AirportManager>.Instance;
		manager.AddAirport(this);
		spawnPoint = new Vector3(0f, -10000f, 0f);
	}

	public bool CargoUnlocked()
	{
		if (data == null)
		{
			return false;
		}
		return !cargoLocked;
	}

	public void SetAirportData(AirportData airportData, ContinentData continent)
	{
		data = airportData;
		cargoType = airportData.cargoTypes;
		landingMessage = airportData.landingMessage;
		id = Regex.Replace(data.name + continent.continentType.name, "\\s+", "");
		if (!string.IsNullOrEmpty(airportData.airportName))
		{
			airportName = airportData.airportName;
		}
		else
		{
			airportName = airportData.name;
		}
		if (airportData.quests.Count > 0)
		{
			currentQuest = airportData.quests[0].CreateQuest(this);
		}
	}

	public void PlaneLandedAtAirport()
	{
		if (currentQuest != null && questInitialized)
		{
			currentQuest.CheckForCompletion();
		}
		if ((currentQuest == null || currentQuest.completed) && !data.baseAirport && Singleton<StoryState>.Instance.playedEndCutscene)
		{
			currentQuest = Singleton<QuestGenerator>.Instance.GenerateQuest(this);
			questInitialized = false;
		}
		if (!questInitialized && currentQuest != null && Singleton<StoryState>.Instance.QuestAvailable(currentQuest) && !questBlocked)
		{
			if (!string.IsNullOrEmpty(currentQuest.guestGiveMessage))
			{
				Singleton<QuestAdditionUI>.Instance.DisplayText(currentQuest.guestGiveMessage, MessageType.QuestAdded, TextDisplayCallback);
			}
			questInitialized = true;
			Vector3[] relatedAirportLocations = currentQuest.GetRelatedAirportLocations();
			for (int i = 0; i < relatedAirportLocations.Length; i++)
			{
				Singleton<FogOfWar>.Instance.RemoveFogAtPosition(relatedAirportLocations[i]);
			}
		}
		if (!questInitialized && currentQuest != null && currentQuest.storyState == StoryStateID.PostEarthquake && !Singleton<StoryState>.Instance.QuestAvailable(currentQuest) && !hospitalMessagePlayed)
		{
			Singleton<QuestAdditionUI>.Instance.DisplayText("We’ve heard that the hospital needs your help.", MessageType.Misc);
			hospitalMessagePlayed = true;
		}
		if (!string.IsNullOrEmpty(landingMessage))
		{
			Singleton<QuestAdditionUI>.Instance.DisplayText(landingMessage, MessageType.Misc);
			landingMessage = null;
		}
	}

	private void TextDisplayCallback(bool completed)
	{
		if (!completed)
		{
			questInitialized = false;
		}
	}

	public bool ContainsCargoType(CargoType cargoType)
	{
		if (this.cargoType == null)
		{
			return false;
		}
		for (int i = 0; i < this.cargoType.Length; i++)
		{
			if (this.cargoType[i] == cargoType)
			{
				return true;
			}
		}
		return false;
	}

	public float GetPrice(CargoType cargoType)
	{
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(position, cargoType);
		float num = 0f;
		if (closestAirport != null)
		{
			num = Vector3.Distance(closestAirport.position, position);
		}
		float num2 = (float)((int)(num / 500f) * 500) / 3000f;
		float num3 = 0.25f;
		if (cargoType.expires)
		{
			num3 = 0.5f;
		}
		return cargoType.basePrice * (1f + num2 * num3);
	}

	public void Save(GameDataWriter writer, long writerPosition)
	{
		writer.Write(allQuests.Count);
		for (int i = 0; i < allQuests.Count; i++)
		{
			writer.Write(allQuests[i].questID);
			writer.Write(allQuests[i].GetType());
			Stream stream = new MemoryStream();
			GameDataWriter writer2 = new GameDataWriter(new BinaryWriter(stream));
			allQuests[i].Save(writer2);
			writer.Write(stream.Length);
			writer.Write(stream);
		}
		writer.Write(currentQuest != null);
		if (currentQuest != null)
		{
			writer.Write(currentQuest.questID);
		}
		writer.Write(questInitialized);
	}

	public long GetQuestSize(Quest quest)
	{
		MemoryStream memoryStream = new MemoryStream();
		GameDataWriter writer = new GameDataWriter(new BinaryWriter(memoryStream));
		quest.Save(writer);
		return memoryStream.Length;
	}

	public void Load(GameDataReader reader)
	{
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string text = reader.ReadString();
			Type type = null;
			if (reader.version > 9)
			{
				type = reader.ReadType();
			}
			Quest quest = GetQuest(text);
			long num2 = reader.ReadLong();
			if (quest == null || (num2 != GetQuestSize(quest) && reader.version < 10) || (type != null && type != quest.GetType()))
			{
				reader.SetStreamPosition(reader.GetStreamPosition() + num2);
			}
			else
			{
				quest.Load(reader);
			}
		}
		bool flag = false;
		if (reader.ReadBool())
		{
			string questID = reader.ReadString();
			currentQuest = GetQuest(questID);
			if (Singleton<QuestGenerator>.Instance.TryGetQuest(questID) != null)
			{
				flag = true;
			}
		}
		if (allQuests.Count > 0 && !flag)
		{
			currentQuest = allQuests[0];
			for (int j = 0; j < 1000; j++)
			{
				if (currentQuest == null)
				{
					break;
				}
				if (!currentQuest.completed)
				{
					break;
				}
				currentQuest = currentQuest.followupQuest;
			}
		}
		Singleton<AirportManager>.Instance.hoveredQuests.Add(currentQuest);
		questInitialized = reader.ReadBool();
		if (reader.version < 17 && airportName == "Weather Station" && !allQuests[0].completed)
		{
			currentQuest = allQuests[0];
		}
	}

	private Quest GetQuest(string id)
	{
		for (int i = 0; i < allQuests.Count; i++)
		{
			if (allQuests[i].questID == id)
			{
				return allQuests[i];
			}
		}
		return null;
	}
}
