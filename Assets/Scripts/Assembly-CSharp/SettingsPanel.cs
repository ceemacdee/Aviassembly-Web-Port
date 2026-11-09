using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
	public TMP_Dropdown resolutionDropdown;

	public Toggle fullscreenToggle;

	public Toggle autoSteerToggle;

	public Toggle autoSaveToggle;

	public Toggle vSync;

	public Toggle shadows;

	public Toggle SSAO;

	public Toggle antiAliassing;

	public Slider volumeSlider;

	public Slider musicVolumeSlider;

	public Slider clockVolumeSlider;

	public Slider cursorSpeed;

	public Slider cursorSlowdown;

	public Slider drawDistance;

	public GameObject[] panels;

	public SettingsPanelHeaderButton[] buttons;

	public Color selectedButton;

	public Color deselectedButton;

	public Color selectedButtonShadow;

	public Color deselectedButtonShadow;

	public Color selectedIcon;

	public Color deselectedIcon;

	public RectTransform scrollRectContentParent;

	public ScrollRect scrollRect;

	private BackgroundMusicManager backgroundMusicManager;

	private GameSettings gameSettings;

	private int currentPanelIndex;

	private void Awake()
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].settingsPanel = this;
			buttons[i].Init();
		}
		OpenPanel(0);
		backgroundMusicManager = Object.FindFirstObjectByType<BackgroundMusicManager>();
		gameSettings = new GameSettings();
		gameSettings.LoadSettings();
		if (GameManager.firstMenuOpen)
		{
			Singleton<GameManager>.Instance.unlockRaces = gameSettings.raceProgress;
		}
		volumeSlider.SetValueWithoutNotify(gameSettings.masterVolume);
		musicVolumeSlider.SetValueWithoutNotify(gameSettings.musicVolume);
		clockVolumeSlider.SetValueWithoutNotify(gameSettings.clockVolume);
#if !UNITY_WEBGL
		Screen.SetResolution(gameSettings.resolution.x, gameSettings.resolution.y, gameSettings.fullscreen);
#endif
		resolutionDropdown.ClearOptions();
		Resolution[] resolutions = Screen.resolutions;
		List<string> list = new List<string>();
		int valueWithoutNotify = 0;
		for (int j = 0; j < resolutions.Length; j++)
		{
			list.Add(resolutions[j].width + " x " + resolutions[j].height);
			if (gameSettings.resolution.x == resolutions[j].width && gameSettings.resolution.y == resolutions[j].height)
			{
				valueWithoutNotify = j;
			}
		}
		resolutionDropdown.AddOptions(list);
		resolutionDropdown.SetValueWithoutNotify(valueWithoutNotify);
#if UNITY_WEBGL
		// WebGL port: the canvas follows the browser window and fullscreen can only be entered from a
		// click, so the resolution picker is hidden and fullscreen starts off until the player toggles it.
		resolutionDropdown.gameObject.SetActive(value: false);
		fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
#else
		fullscreenToggle.SetIsOnWithoutNotify(gameSettings.fullscreen);
#endif
		Singleton<GameManager>.Instance.graphicsSettings.drawDistance = gameSettings.drawDistance;
		Singleton<GameManager>.Instance.graphicsSettings.targetDrawDistance = gameSettings.drawDistance;
		drawDistance.SetValueWithoutNotify(Mathf.InverseLerp(1000f, 4000f, Singleton<GameManager>.Instance.graphicsSettings.drawDistance));
		Update();
		Singleton<GameManager>.Instance.autoSteer = gameSettings.autoSteer;
		autoSteerToggle.SetIsOnWithoutNotify(Singleton<GameManager>.Instance.autoSteer);
		Singleton<GameManager>.Instance.autoSave = gameSettings.autoSave;
		autoSaveToggle.SetIsOnWithoutNotify(Singleton<GameManager>.Instance.autoSave);
		shadows.SetIsOnWithoutNotify(gameSettings.shadows);
		ToggleShadows(gameSettings.shadows);
		vSync.SetIsOnWithoutNotify(gameSettings.vSync);
		ToggleVSync(gameSettings.vSync);
		SSAO.SetIsOnWithoutNotify(gameSettings.ssao);
		ToggleSSAO(gameSettings.ssao);
		antiAliassing.SetIsOnWithoutNotify(gameSettings.antiAliasing);
		ToggleAntiAliasing(gameSettings.antiAliasing);
	}

	private void Start()
	{
		cursorSpeed.SetValueWithoutNotify(Singleton<MouseInput>.Instance.cursorSpeed / 2000f);
		cursorSlowdown.SetValueWithoutNotify(Singleton<MouseInput>.Instance.mouseSlowdown);
	}

	private void Update()
	{
		AudioListener.volume = volumeSlider.value;
		backgroundMusicManager.musicVolume = musicVolumeSlider.value;
		Singleton<GameManager>.Instance.clockVolume = clockVolumeSlider.value;
		gameSettings.musicVolume = musicVolumeSlider.value;
		gameSettings.masterVolume = volumeSlider.value;
		gameSettings.clockVolume = clockVolumeSlider.value;
		Singleton<GameManager>.Instance.graphicsSettings.targetDrawDistance = Mathf.Lerp(1000f, 4000f, drawDistance.value);
		gameSettings.drawDistance = Mathf.Lerp(1000f, 4000f, drawDistance.value);
		gameSettings.raceProgress = Singleton<GameManager>.Instance.unlockRaces;
		gameSettings.SaveSettings();
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panels[currentPanelIndex].transform);
		scrollRectContentParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((RectTransform)panels[currentPanelIndex].transform).rect.height);
	}

	private void LateUpdate()
	{
		Singleton<MouseInput>.Instance.cursorSpeed = Mathf.Max(cursorSpeed.value * 2000f, 100f);
		Singleton<MouseInput>.Instance.mouseSlowdown = cursorSlowdown.value;
		Singleton<MouseInput>.Instance.SaveSettings();
	}

	public void ToggleFullscreen(bool value)
	{
#if UNITY_WEBGL
		Screen.fullScreen = value;
#else
		Screen.fullScreenMode = (value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
		gameSettings.fullscreen = value;
	}

	public void ToggleShadows(bool value)
	{
		Singleton<GameManager>.Instance.graphicsSettings.shadows = value;
		gameSettings.shadows = value;
	}

	public void ToggleVSync(bool value)
	{
		Singleton<GameManager>.Instance.graphicsSettings.vSync = value;
		QualitySettings.vSyncCount = (value ? 1 : 0);
		gameSettings.vSync = value;
	}

	public void ToggleSSAO(bool value)
	{
		Singleton<GameManager>.Instance.graphicsSettings.SSAO = value;
		gameSettings.ssao = value;
	}

	public void ToggleAutoSteer(bool value)
	{
		gameSettings.autoSteer = value;
		Singleton<GameManager>.Instance.autoSteer = value;
	}

	public void ToggleAutoSave(bool value)
	{
		gameSettings.autoSave = value;
		Singleton<GameManager>.Instance.autoSave = value;
	}

	public void ToggleAntiAliasing(bool value)
	{
		QualitySettings.antiAliasing = (value ? 4 : 0);
		gameSettings.antiAliasing = value;
	}

	public void ChangeResolution(int index)
	{
		Vector2Int resolution = new Vector2Int(Screen.resolutions[index].width, Screen.resolutions[index].height);
#if !UNITY_WEBGL
		Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);
#endif
		gameSettings.resolution = resolution;
	}

	public void OpenPanel(int index)
	{
		for (int i = 0; i < panels.Length; i++)
		{
			panels[i].gameObject.SetActive(index == i);
			buttons[i].SetColor((index == i) ? selectedButton : deselectedButton, (index == i) ? selectedIcon : deselectedIcon, (index == i) ? selectedButtonShadow : deselectedButtonShadow);
		}
		currentPanelIndex = index;
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panels[index].transform);
		scrollRectContentParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ((RectTransform)panels[index].transform).rect.height);
		scrollRect.StopMovement();
		scrollRect.verticalNormalizedPosition = 1f;
	}

	public void ResetBindings()
	{
		Singleton<InputManager>.Instance.ResetAllKeybinds();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}
}
