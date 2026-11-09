using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
	public int sourceBufferSize;

	private Transform audioListenerTransform;

	private AudioSource[] sources;

	private int currentSourceIndex;

	public GameObject menuDialog;

	private bool muteEngines;

	private float currentEngineVolume;

	private float engineVolumeVelocity;

	protected override void Awake()
	{
		base.Awake();
		InitializeAudioSources();
		base.transform.position = Vector3.zero;
	}

	public void SetEnginesMutes(bool muteEngines)
	{
		this.muteEngines = muteEngines;
	}

	private void Update()
	{
		float target = ((!menuDialog.activeInHierarchy && !muteEngines) ? 1 : 0);
		currentEngineVolume = Mathf.SmoothDamp(currentEngineVolume, target, ref engineVolumeVelocity, 0.5f, float.MaxValue, Time.unscaledDeltaTime);
	}

	public float EnginesMuted()
	{
		return currentEngineVolume;
	}

	public void PlaySound(AudioDef audioDef)
	{
		sources[currentSourceIndex].transform.position = GetListenerPosition();
		sources[currentSourceIndex].volume = audioDef.volume;
		sources[currentSourceIndex].PlayOneShot(audioDef.clip);
		currentSourceIndex++;
		if (currentSourceIndex > sourceBufferSize - 1)
		{
			currentSourceIndex = 0;
		}
	}

	private void InitializeAudioSources()
	{
		sources = new AudioSource[sourceBufferSize];
		for (int i = 0; i < sourceBufferSize; i++)
		{
			GameObject gameObject = new GameObject();
			gameObject.name = "Audio Source";
			gameObject.transform.SetParent(base.transform);
			sources[i] = gameObject.AddComponent<AudioSource>();
		}
	}

	private Vector3 GetListenerPosition()
	{
		if (audioListenerTransform == null)
		{
			AudioListener audioListener = Object.FindFirstObjectByType<AudioListener>();
			audioListenerTransform = audioListener.transform;
		}
		if (audioListenerTransform != null)
		{
			return audioListenerTransform.position;
		}
		return Vector3.zero;
	}
}
