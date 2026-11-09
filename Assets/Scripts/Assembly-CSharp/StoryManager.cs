using UnityEngine;

public class StoryManager : Singleton<StoryManager>
{
	[Header("Messages")]
	public string[] mysteriousMessages;

	public string[] mysteriousMessages2;

	public string[] mysteriousMessages3;

	public string finalMessage;

	public float mysteriousMessagesInterval;

	public float mysteriousMessagesIntervalIncrease;

	[Space(15f)]
	[Header("Airports")]
	public AirportData radioTower;

	public AirportData researchFacility;

	public AirportData undergroundStorage;

	public AirportData explosionSite;

	[Space(15f)]
	[Header("Refrences")]
	[Range(0f, 1f)]
	public float earthQuake;

	public CameraController cameraController;

	public AudioSource earthQuackeAudio;

	public FlyingUIController flyingUIController;

	public AudioDef MysterySound;

	public ParticleSystem earthquakeParticle;

	public EndCutscene endCutscene;

	[Header("Music")]
	public AudioClip postEarthQuakeSoundtrack;

	public AudioClip postFinalMessageSoundtrack;

	public AudioClip finalSequenceSoundtrack;

	private float earthquakeTimer;

	private float earthquackeDelay;

	private bool postEarthQuackeMessagePlayed;

	private Airport hospitalAirport;

	private bool mysteryMessageDirty;

	private float finalMessageTimer;

	private bool messagePlayed;

	private bool finalWarningPlayed;

	private void Start()
	{
		if (Singleton<GameManager>.Instance.gameModeData.disableAirports)
		{
			base.enabled = false;
			SetEarthquakeStrength(0f);
			return;
		}
		for (int i = 0; i < Singleton<AirportManager>.Instance.airports.Count; i++)
		{
			AirportData data = Singleton<AirportManager>.Instance.airports[i].data;
			if (data.airportName.Contains("Weather"))
			{
				radioTower = data;
			}
			if (data.airportName.Contains("Research Facility"))
			{
				researchFacility = data;
			}
			if (data.airportName.Contains("Storage"))
			{
				undergroundStorage = data;
			}
			if (data.airportName.Contains("Site"))
			{
				explosionSite = data;
			}
		}
		hospitalAirport = Singleton<AirportManager>.Instance.GetClosestAirport(new Vector3(0f, 0f, 3000f));
		finalMessageTimer = 15f;
	}

	public bool FinalCutSceneIsPlaying()
	{
		return endCutscene.cutSceneStarted;
	}

	public bool UpdateFinalCutscene()
	{
		Airport airport = Singleton<AirportManager>.Instance.GetAirport(undergroundStorage);
		if (AllQuestCompleted(airport) && !Singleton<StoryState>.Instance.playedEndCutscene && !Singleton<QuestAdditionUI>.Instance.IsOpen())
		{
			Singleton<StoryState>.Instance.playedEndCutscene = true;
			endCutscene.StartCutscene();
			Singleton<FlyingBackgroundMusic>.Instance.SetOverrideClip(finalSequenceSoundtrack);
			Singleton<AchievementManager>.Instance.BeatCampaign();
			return true;
		}
		return false;
	}

	private void Update()
	{
		AirportManager instance = Singleton<AirportManager>.Instance;
		UpdateEarthquake();
		UpdateRestrictedZones();
		UpdateFinalCutscene();
		if (Singleton<StoryState>.Instance.postEarthquake && !AllQuestCompleted(instance.GetAirport(researchFacility)) && Singleton<PlaneContainer>.Instance.DistanceFromGround() > 50f && Singleton<PlaneContainer>.Instance.gameObject.activeInHierarchy)
		{
			Singleton<StoryState>.Instance.mysteriousMessageTimer -= Time.deltaTime;
		}
		if (Singleton<StoryState>.Instance.mysteriousMessageTimer < 0f)
		{
			BroadcastMysteriousMessage();
			Singleton<StoryState>.Instance.mysteriousMessageTimer = mysteriousMessagesInterval + mysteriousMessagesIntervalIncrease * (float)Singleton<StoryState>.Instance.mysteriousMessageIndex;
		}
		if (mysteryMessageDirty && !Singleton<QuestAdditionUI>.Instance.IsOpen())
		{
			Singleton<AudioManager>.Instance.PlaySound(MysterySound);
			mysteryMessageDirty = false;
		}
		if (!Singleton<StoryState>.Instance.finalMessagePlayed && AllQuestCompleted(instance.GetAirport(researchFacility)))
		{
			if (Singleton<PlaneContainer>.Instance.DistanceFromGround() > 50f && Singleton<PlaneContainer>.Instance.DistanceFromGround() > 50f && !Singleton<QuestAdditionUI>.Instance.IsOpen() && !Singleton<PlaneContainer>.Instance.IsAtAirport())
			{
				finalMessageTimer -= Time.deltaTime;
			}
			if (finalMessageTimer < 0f)
			{
				Singleton<StoryState>.Instance.finalMessagePlayed = true;
				Singleton<FlyingBackgroundMusic>.Instance.SetOverrideClip(postFinalMessageSoundtrack);
				Singleton<QuestAdditionUI>.Instance.DisplayText(finalMessage, MessageType.Mystery);
				mysteryMessageDirty = true;
				Singleton<FogOfWar>.Instance.RemoveFogAtPosition(Singleton<AirportManager>.Instance.GetAirport(explosionSite).position);
			}
		}
		bool flag = true;
		for (int i = 0; i < instance.airports.Count; i++)
		{
			if (!AllQuestCompleted(instance.airports[i]))
			{
				flag = false;
				break;
			}
		}
		Airport airport = Singleton<AirportManager>.Instance.GetAirport(undergroundStorage);
		if (flag)
		{
			Singleton<AchievementManager>.Instance.UnlockAchievement("ACH_COMPLETIONIST");
		}
		if (AllQuestCompleted(airport))
		{
			Singleton<AchievementManager>.Instance.BeatCampaign();
		}
	}

	private bool AllQuestCompleted(Airport airport)
	{
		for (int i = 0; i < airport.allQuests.Count; i++)
		{
			if (!airport.allQuests[i].completed && !airport.allQuests[i].randomGeneratedQuest)
			{
				return false;
			}
		}
		return true;
	}

	private int QuestCompletedCount(Airport airport)
	{
		int num = 0;
		for (int i = 0; i < airport.allQuests.Count; i++)
		{
			if (airport.allQuests[i].completed)
			{
				num++;
			}
		}
		return num;
	}

	private float GetFlatDistance(Vector3 point1, Vector3 point2)
	{
		return Vector3.Distance(new Vector3(point1.x, 0f, point1.z), new Vector3(point2.x, 0f, point2.z));
	}

	private void UpdateRestrictedZones()
	{
		Airport airport = Singleton<AirportManager>.Instance.GetAirport(researchFacility);
		Airport airport2 = Singleton<AirportManager>.Instance.GetAirport(radioTower);
		ContinentData continentData = Singleton<ContinentManager>.Instance.continents[1];
		airport2.cargoLocked = !AllQuestCompleted(airport2);
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		PlaneController component = Singleton<PlaneContainer>.Instance.gameObject.GetComponent<PlaneController>();
		GetFlatDistance(airport.position, instance.transform.position);
		float flatDistance = GetFlatDistance(continentData.origin, instance.transform.position);
		bool flag = AllQuestCompleted(airport2);
		bool num = AllQuestCompleted(airport) || Singleton<StoryState>.Instance.desertUnlocked;
		if (flag)
		{
			Singleton<FogOfWar>.Instance.RemoveFogAtPosition(airport.position);
			Singleton<StoryState>.Instance.snowUnlocked = true;
		}
		if (Singleton<StoryState>.Instance.postEarthquake)
		{
			Singleton<FogOfWar>.Instance.RemoveFogAtPosition(airport2.position);
		}
		if (num)
		{
			Singleton<StoryState>.Instance.desertUnlocked = true;
		}
		airport.questBlocked = !flag;
		if (Vector3.Distance(Singleton<PlaneContainer>.Instance.transform.position, airport.position) > 3000f)
		{
			airport.landingMessage = ((!flag) ? "Our friends at the weather station need your help" : null);
		}
		bool flag2 = !num && flatDistance < continentData.continentType.radius + 2000f;
		bool flag3 = !num && flatDistance < continentData.continentType.radius + 1000f;
		bool num2 = !num && flatDistance < continentData.continentType.radius;
		if (flag2 && !messagePlayed)
		{
			if (AllQuestCompleted(airport2))
			{
				Singleton<QuestAdditionUI>.Instance.DisplayText("This is a restricted zone. Complete research facility missions first!", MessageType.Misc);
			}
			else
			{
				Singleton<QuestAdditionUI>.Instance.DisplayText("This is a restricted zone. Complete weather station missions first!", MessageType.Misc);
			}
			messagePlayed = true;
		}
		if (flag3 && !finalWarningPlayed)
		{
			Singleton<QuestAdditionUI>.Instance.DisplayText("This is your final warning! Turn back now!!", MessageType.Misc);
			finalWarningPlayed = true;
		}
		if (num2 && !component.Exploded)
		{
			component.ExplodePlane();
		}
		if (!flag2)
		{
			messagePlayed = false;
			finalWarningPlayed = false;
		}
	}

	private void BroadcastMysteriousMessage()
	{
		if (Singleton<QuestAdditionUI>.Instance.IsOpen() || Singleton<PlaneContainer>.Instance.IsAtAirport())
		{
			return;
		}
		if (AllQuestCompleted(Singleton<AirportManager>.Instance.GetAirport(radioTower)))
		{
			if (Singleton<StoryState>.Instance.mysteriousMessageIndex2 <= mysteriousMessages2.Length - 1)
			{
				Singleton<QuestAdditionUI>.Instance.DisplayText(mysteriousMessages2[Singleton<StoryState>.Instance.mysteriousMessageIndex2], MessageType.Mystery);
				Singleton<StoryState>.Instance.mysteriousMessageIndex2++;
				mysteryMessageDirty = true;
			}
		}
		else if (QuestCompletedCount(Singleton<AirportManager>.Instance.GetAirport(researchFacility)) > 3)
		{
			if (Singleton<StoryState>.Instance.mysteriousMessageIndex3 <= mysteriousMessages3.Length - 1)
			{
				Singleton<QuestAdditionUI>.Instance.DisplayText(mysteriousMessages3[Singleton<StoryState>.Instance.mysteriousMessageIndex3], MessageType.Mystery);
				Singleton<StoryState>.Instance.mysteriousMessageIndex3++;
				mysteryMessageDirty = true;
			}
		}
		else if (Singleton<StoryState>.Instance.mysteriousMessageIndex <= mysteriousMessages.Length - 1)
		{
			Singleton<QuestAdditionUI>.Instance.DisplayText(mysteriousMessages[Singleton<StoryState>.Instance.mysteriousMessageIndex], MessageType.Mystery);
			Singleton<StoryState>.Instance.mysteriousMessageIndex++;
			mysteryMessageDirty = true;
		}
	}

	private void UpdateEarthquake()
	{
		if (earthQuake > 0.02f)
		{
			flyingUIController.CloseAllWindows();
			flyingUIController.LockUI();
		}
		earthquakeParticle.transform.position = Singleton<PlaneContainer>.Instance.transform.position;
		if (GetCompletedHospitalQuests() > 0 && !Singleton<QuestAdditionUI>.Instance.IsOpen() && !Singleton<StoryState>.Instance.postEarthquake)
		{
			earthquackeDelay += Time.deltaTime;
			if (earthquackeDelay > 2f)
			{
				if (earthquakeTimer <= 8f)
				{
					earthQuake += Time.deltaTime * 0.1f;
				}
				earthquakeTimer += Time.deltaTime;
				Singleton<StoryState>.Instance.earthquakeStarted = true;
				earthquakeParticle.Play();
			}
		}
		if (earthquakeTimer > 8f)
		{
			earthquakeParticle.Stop();
			earthQuake -= Time.deltaTime * 0.2f;
			if (!Singleton<StoryState>.Instance.postEarthquake && !postEarthQuackeMessagePlayed && earthQuake < 0.2f)
			{
				Singleton<QuestAdditionUI>.Instance.DisplayText("What was that?!... That seemed like an earthquake!!", MessageType.Misc);
				Singleton<QuestAdditionUI>.Instance.DisplayText("Can you check around the island if people need help?", MessageType.Misc);
				Singleton<QuestAdditionUI>.Instance.DisplayText("You should also visit the weather station to find out what is going on.", MessageType.Misc);
				Singleton<FlyingBackgroundMusic>.Instance.SetOverrideClip(postEarthQuakeSoundtrack);
				postEarthQuackeMessagePlayed = true;
			}
		}
		if (earthquakeTimer > 12f && earthQuake < 0.01f && !Singleton<QuestAdditionUI>.Instance.IsOpen())
		{
			Singleton<StoryState>.Instance.postEarthquake = true;
		}
		earthQuake = Mathf.Clamp01(earthQuake);
		SetEarthquakeStrength(earthQuake);
	}

	private void SetEarthquakeStrength(float strength)
	{
		strength = Mathf.Clamp01(strength);
		cameraController.shake = !Mathf.Approximately(0f, strength);
		earthQuackeAudio.enabled = !Mathf.Approximately(0f, strength);
		earthQuackeAudio.volume = strength;
		cameraController.shakeAmount = 0.5f * strength;
	}

	private int GetCompletedHospitalQuests()
	{
		int num = 0;
		for (int i = 0; i < hospitalAirport.allQuests.Count; i++)
		{
			if (hospitalAirport.allQuests[i].completed)
			{
				num++;
			}
		}
		return num;
	}
}
