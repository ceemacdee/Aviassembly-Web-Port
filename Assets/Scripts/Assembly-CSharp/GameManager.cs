using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
	public Fader fader;

	public GraphicsSettings graphicsSettings;

	public string currentSaveFile;

	public Vector3 planePosistion;

	public Quaternion planeRotatation;

	public static GameMode gameMode;

	public static bool firstMenuOpen;

	public bool inMenu;

	public bool menuIntroPlayed;

	public bool modalsOpen;

	public bool autoSteer;

	public bool autoSave;

	public bool raceEnabled;

	public int unlockRaces;

	public float clockVolume;

	public GameModeData gameModeData;

	public GameMode savedGameMode;

	private bool startup;

	public bool Loading { get; private set; }

	public static event Action flyModeLoaded;

	public static event Action buildModeLoaded;

	protected override void Awake()
	{
		base.Awake();
		fader = Singleton<Fader>.Instance;
		gameModeData = GetComponent<GameModeData>();
	}

	private void Start()
	{
		firstMenuOpen = true;
		SceneManager.LoadScene("Menu", LoadSceneMode.Additive);
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void Update()
	{
		graphicsSettings.drawDistance = Mathf.MoveTowards(graphicsSettings.drawDistance, graphicsSettings.targetDrawDistance, Time.unscaledDeltaTime * 2000f);
		graphicsSettings.drawDistance = Mathf.Clamp(graphicsSettings.drawDistance, 1000f, 4000f);
	}

	public void StartMenu()
	{
		if (!Loading)
		{
			firstMenuOpen = false;
			Loading = true;
			StartCoroutine(LoadMenu());
		}
	}

	public void StartGame()
	{
		if (!Loading)
		{
			Loading = true;
			StartCoroutine(StartNewGame());
		}
	}

	public void StartFlyMode(bool resetPlane = true, bool resetCargo = false)
	{
		if (!Loading)
		{
			if (resetCargo)
			{
				Singleton<CargoInventory>.Instance.ClearAll();
			}
			Loading = true;
			StartCoroutine(LoadFlyMode(resetPlane));
		}
	}

	public void StartBuildMode(bool force)
	{
		if (!Loading)
		{
			Loading = true;
			StartCoroutine(LoadBuildMode());
		}
	}

	private IEnumerator LoadMenu()
	{
		fader.FadeIn();
		while (!fader.FadeReady())
		{
			yield return null;
		}
		if (SceneManager.GetSceneByName("Flying").isLoaded)
		{
			SceneManager.UnloadSceneAsync("Flying");
		}
		if (SceneManager.GetSceneByName("Building").isLoaded)
		{
			SceneManager.UnloadSceneAsync("Building");
		}
		while (SceneManager.GetSceneByName("Flying").isLoaded || SceneManager.GetSceneByName("Building").isLoaded)
		{
			yield return null;
		}
		if (SceneManager.GetSceneByName("Game").isLoaded)
		{
			SceneManager.UnloadSceneAsync("Game");
		}
		while (SceneManager.GetSceneByName("Flying").isLoaded)
		{
			yield return null;
		}
		SceneManager.LoadScene("Menu", LoadSceneMode.Additive);
		yield return new WaitForSecondsRealtime(2f);
		fader.FadeOut();
	}

	private IEnumerator StartNewGame()
	{
		fader.FadeIn();
		while (!fader.FadeReady())
		{
			yield return null;
		}
		yield return new WaitForSecondsRealtime(1f);
		if (SceneManager.GetSceneByName("Menu").isLoaded)
		{
			SceneManager.UnloadSceneAsync("Menu");
		}
		SceneManager.LoadScene("Game", LoadSceneMode.Additive);
	}

	private IEnumerator LoadFlyMode(bool resetPlane)
	{
		fader.FadeIn();
		while (!fader.FadeReady())
		{
			yield return null;
		}
		Singleton<PlaneContainer>.Instance.ResetPlane();
		Singleton<PlaneContainer>.Instance.transform.position = (resetPlane ? GetPlaneSpawnPoint() : planePosistion);
		Singleton<PlaneContainer>.Instance.transform.rotation = (resetPlane ? GetPlaneSpawnRotation() : planeRotatation);
		yield return null;
		if (SceneManager.GetSceneByName("Flying").isLoaded)
		{
			AsyncOperation unloadSceneOperation = SceneManager.UnloadSceneAsync("Flying");
			while (!unloadSceneOperation.isDone)
			{
				yield return null;
			}
		}
		if (SceneManager.GetSceneByName("Building").isLoaded)
		{
			AsyncOperation unloadSceneOperation = SceneManager.UnloadSceneAsync("Building");
			while (!unloadSceneOperation.isDone)
			{
				yield return null;
			}
		}
		AsyncOperation loadSceneOperation = SceneManager.LoadSceneAsync("Flying", LoadSceneMode.Additive);
		while (!loadSceneOperation.isDone)
		{
			yield return null;
		}
		Singleton<PlaneContainer>.Instance.ActivateFlyMode();
		if (!Singleton<AirportManager>.Instance.GetClosestAirport(Singleton<AirportManager>.Instance.LastAirport).data.refuelAvailable)
		{
			Singleton<PlaneContainer>.Instance.fuel = Singleton<AirportManager>.Instance.FuelAtLand;
			Singleton<PlaneContainer>.Instance.electricity = Singleton<AirportManager>.Instance.ElecticityAtLand;
		}
		else
		{
			Singleton<PlaneContainer>.Instance.Refuel();
		}
		yield return null;
		yield return new WaitForSecondsRealtime(0.25f);
		fader.FadeOut();
	}

	private IEnumerator LoadBuildMode()
	{
		fader.FadeIn();
		while (!fader.FadeReady())
		{
			yield return null;
		}
		if (SceneManager.GetSceneByName("Flying").isLoaded)
		{
			AsyncOperation unloadSceneOperation = SceneManager.UnloadSceneAsync("Flying");
			while (!unloadSceneOperation.isDone)
			{
				yield return null;
			}
		}
		AsyncOperation loadSceneOperation = SceneManager.LoadSceneAsync("Building", LoadSceneMode.Additive);
		while (!loadSceneOperation.isDone)
		{
			yield return null;
		}
		Singleton<PlaneContainer>.Instance.ResetPlane();
		yield return null;
		fader.FadeOut();
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		inMenu = scene.name == "Menu";
		if (inMenu)
		{
			Time.timeScale = 1f;
			StartCoroutine(InitialFadeOut());
			Loading = false;
		}
		if (scene.name == "Game")
		{
			BuildingCamera.intialized = false;
			Loading = false;
			startup = true;
			if (startup && currentSaveFile != "")
			{
				Singleton<PersistentStorage>.Instance.Load(currentSaveFile);
				if (savedGameMode == GameMode.Flying)
				{
					StartFlyMode(resetPlane: false);
				}
				else
				{
					StartCoroutine(LoadBuildMode());
				}
			}
			if (currentSaveFile == "")
			{
				StartCoroutine(LoadBuildMode());
			}
			Singleton<PersistentStorage>.Instance.currentHeader.timeAtGameStart = Time.unscaledTime;
		}
		if (scene.name == "Building")
		{
			if (Singleton<PlaneContainer>.Instance != null)
			{
				Singleton<PlaneContainer>.Instance.OnBuildModeLoaded();
			}
			if (buildModeLoaded != null)
			{
				buildModeLoaded();
			}
			gameMode = GameMode.Building;
			Loading = false;
			startup = false;
		}
		if (scene.name == "Flying")
		{
			if (flyModeLoaded != null)
			{
				flyModeLoaded();
			}
			gameMode = GameMode.Flying;
			Loading = false;
		}
	}

	private IEnumerator InitialFadeOut()
	{
		yield return new WaitForSecondsRealtime(1.5f);
		fader.FadeOut();
	}

	public static Vector3 GetPlaneSpawnPoint()
	{
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(Singleton<AirportManager>.Instance.LastAirport);
		if (closestAirport == null || closestAirport.data == null || closestAirport.data.airportPrefab == null)
		{
			return new Vector3(0f, 0f - Singleton<PlaneContainer>.Instance.GetLowestPosition() + Singleton<TerrainGenerationManager>.Instance.GetTerrainHeight(Vector3.zero) + 0.1f, -215f);
		}
		Quaternion airportRotation = Singleton<AirportManager>.Instance.GetAirportRotation(closestAirport);
		float num = Singleton<TerrainGenerationManager>.Instance.GetAvarageTerrainHeight(closestAirport.position);
		float num2 = 0f - Singleton<PlaneContainer>.Instance.GetLowestPosition();
		Vector3 position = closestAirport.data.airportPrefab.GetComponent<AirportSpawnPoint>().spawnPoint.transform.position;
		position = Quaternion.Inverse(closestAirport.data.airportPrefab.transform.rotation) * airportRotation * position;
		if (closestAirport.data.offshoreAirport)
		{
			num = position.y - 8f;
		}
		position += closestAirport.position;
		position.y = num2 + num + 0.1f;
		return position;
	}

	public static Quaternion GetPlaneSpawnRotation()
	{
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(Singleton<AirportManager>.Instance.LastAirport);
		if (closestAirport == null || closestAirport.data == null || closestAirport.data.airportPrefab == null)
		{
			return Quaternion.identity;
		}
		Vector3 vector = Singleton<AirportManager>.Instance.GetAirportRotation(closestAirport) * closestAirport.data.airportPrefab.GetComponent<AirportSpawnPoint>().spawnPoint.forward;
		Vector3 fromDirection = Singleton<PlaneContainer>.Instance.UpdateForwardDirection();
		if (vector.magnitude < 0.5f)
		{
			vector = Vector3.forward;
		}
		return Quaternion.LookRotation(Quaternion.FromToRotation(fromDirection, Singleton<PlaneContainer>.Instance.transform.forward) * vector, Vector3.up);
	}

	public void Save(GameDataWriter writer)
	{
		gameModeData.Save(writer);
	}

	public void Load(GameDataReader reader)
	{
		if (reader.version >= 16)
		{
			gameModeData.Load(reader);
		}
	}
}
