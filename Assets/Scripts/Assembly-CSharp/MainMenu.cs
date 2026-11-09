using UnityEngine;

public class MainMenu : MonoBehaviour
{
	[Header("Menus")]
	public GameObject mainMenu;

	public GameObject playGameMenu;

	[Header("Panels")]
	public GameObject loadPanel;

	public GameObject demoQuitPanel;

	private void Start()
	{
		OpenObject(mainMenu);
#if UNITY_WEBGL
		HideQuitButtons();
#endif
	}

#if UNITY_WEBGL
	// WebGL port: a browser tab cannot be quit by the game, so buttons wired to QuitGame/InitQuit are hidden.
	private void HideQuitButtons()
	{
		UnityEngine.UI.Button[] array = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (UnityEngine.UI.Button button in array)
		{
			for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
			{
				string persistentMethodName = button.onClick.GetPersistentMethodName(i);
				if (persistentMethodName == "QuitGame" || persistentMethodName == "InitQuit")
				{
					button.gameObject.SetActive(value: false);
					break;
				}
			}
		}
	}
#endif

	public void OpenObject(GameObject gameObject)
	{
		loadPanel.SetActive(value: false);
		playGameMenu.SetActive(value: false);
		mainMenu.SetActive(value: false);
		if (gameObject != null)
		{
			gameObject.SetActive(value: true);
		}
	}

	public void OpenNewGameMenu()
	{
		OpenObject(playGameMenu);
	}

	public void OpenMainMenu()
	{
		OpenObject(mainMenu);
	}

	public void StartNewGame()
	{
		Singleton<GameManager>.Instance.currentSaveFile = "";
		Singleton<GameManager>.Instance.gameModeData.creativeMode = false;
		Singleton<GameManager>.Instance.gameModeData.disableAirports = false;
		Singleton<GameManager>.Instance.gameModeData.unlockAll = false;
		Singleton<GameManager>.Instance.StartGame();
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	public void InitQuit()
	{
		QuitGame();
	}

	public void StartCreativeMode()
	{
		Singleton<GameManager>.Instance.currentSaveFile = "";
		Singleton<GameManager>.Instance.gameModeData.creativeMode = true;
		Singleton<GameManager>.Instance.gameModeData.unlockAll = true;
		Singleton<GameManager>.Instance.gameModeData.disableAirports = true;
		Singleton<GameManager>.Instance.StartGame();
	}

	public void StartRace()
	{
		Singleton<GameManager>.Instance.currentSaveFile = "";
		Singleton<GameManager>.Instance.gameModeData.creativeMode = false;
		Singleton<GameManager>.Instance.gameModeData.unlockAll = true;
		Singleton<GameManager>.Instance.gameModeData.disableAirports = true;
		Singleton<GameManager>.Instance.StartGame();
	}

	public void SetMapType(int index)
	{
		Singleton<GameManager>.Instance.gameModeData.mapType = (MapType)index;
	}
}
