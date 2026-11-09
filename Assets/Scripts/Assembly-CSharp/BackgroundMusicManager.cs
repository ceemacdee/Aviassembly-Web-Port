using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
	public float musicVolume;

	public float fadeTime;

	[Space(10f)]
	public FlyingBackgroundMusic flyingMusic;

	public AudioSource buildingMusic;

	public MenuAudio mainMenuMusic;

	[Space(10f)]
	public AudioClip[] buildingTracks;

	private float buildingMusicTargetVolume;

	private float menuMusicTargetVolume;

	private float flyingMusicTargetVolume;

	private int currentBuildMusicIndex;

	private void Awake()
	{
		mainMenuMusic.Init();
		flyingMusic.maxVolume = 0f;
		buildingMusic.volume = 0f;
		mainMenuMusic.SetVolume(0f);
		mainMenuMusic.StartPlaying();
		flyingMusic.fadeTime = fadeTime;
		GameManager.buildModeLoaded += BuildModeLoaded;
	}

	private void Update()
	{
		SetTargetVolumes();
		float maxDelta = Time.deltaTime * 1f / Mathf.Max(fadeTime, 0.1f);
		flyingMusic.maxVolume = Mathf.MoveTowards(flyingMusic.maxVolume, flyingMusicTargetVolume, maxDelta);
		buildingMusic.volume = Mathf.MoveTowards(buildingMusic.volume, buildingMusicTargetVolume, maxDelta);
		mainMenuMusic.SetVolume(Mathf.MoveTowards(mainMenuMusic.volume, menuMusicTargetVolume, maxDelta));
	}

	private void SetTargetVolumes()
	{
		if (Singleton<GameManager>.Instance.inMenu)
		{
			menuMusicTargetVolume = musicVolume;
			buildingMusicTargetVolume = 0f;
			flyingMusicTargetVolume = 0f;
		}
		else if (GameManager.gameMode == GameMode.Flying)
		{
			menuMusicTargetVolume = 0f;
			buildingMusicTargetVolume = 0f;
			flyingMusicTargetVolume = musicVolume;
		}
		else
		{
			menuMusicTargetVolume = 0f;
			buildingMusicTargetVolume = musicVolume;
			flyingMusicTargetVolume = 0f;
		}
	}

	private void BuildModeLoaded()
	{
		buildingMusic.clip = buildingTracks[GetBuildMusicIndex()];
		buildingMusic.Play();
	}

	private int GetBuildMusicIndex()
	{
		currentBuildMusicIndex++;
		if (buildingTracks.Length - 1 < currentBuildMusicIndex)
		{
			currentBuildMusicIndex = 0;
		}
		return currentBuildMusicIndex;
	}
}
