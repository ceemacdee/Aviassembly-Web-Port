using UnityEngine;

public class EngineSound : MonoBehaviour
{
	public AudioSource engineSound;

	public AudioSource afterBurnerSound;

	private float pitchModulation;

	private float input;

	private GameManager gameManager;

	private AudioManager audioManager;

	private void Awake()
	{
		pitchModulation = Random.Range(0f, 0.2f);
		engineSound.volume = 0f;
		if (afterBurnerSound != null)
		{
			afterBurnerSound.volume = 0f;
		}
	}

	private void Start()
	{
		gameManager = Singleton<GameManager>.Instance;
		audioManager = Singleton<AudioManager>.Instance;
	}

	public void UpdateEngineSound(float input)
	{
		this.input = input;
	}

	private void Update()
	{
		engineSound.volume = Mathf.Abs(input) * audioManager.EnginesMuted();
		engineSound.pitch = 0.5f + Mathf.Abs(input) * 0.5f - pitchModulation;
		if (afterBurnerSound != null)
		{
			float num = Mathf.Clamp01(Mathf.Abs(input) - 0.8f) * 5f;
			afterBurnerSound.volume = num * audioManager.EnginesMuted();
			afterBurnerSound.pitch = 0.5f + num * 0.5f - pitchModulation;
		}
		if (GameManager.gameMode == GameMode.Building || gameManager.Loading)
		{
			engineSound.volume = 0f;
			if (afterBurnerSound != null)
			{
				afterBurnerSound.volume = 0f;
			}
		}
	}

	private void OnDisable()
	{
		engineSound.volume = 0f;
		if (afterBurnerSound != null)
		{
			afterBurnerSound.volume = 0f;
		}
	}
}
