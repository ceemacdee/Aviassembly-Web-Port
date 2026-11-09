using UnityEngine;

public class FlyingBackgroundMusic : Singleton<FlyingBackgroundMusic>
{
	public float maxVolume;

	[HideInInspector]
	public float fadeTime;

	public AudioSource activeSource;

	public AudioSource inactiveSource;

	private float targetVolume;

	private AudioClip overrideClip;

	private void SetNewClip(AudioClip clip)
	{
		AudioSource audioSource = activeSource;
		activeSource = inactiveSource;
		inactiveSource = audioSource;
		activeSource.clip = clip;
		activeSource.Play();
		activeSource.loop = true;
	}

	public void SetOverrideClip(AudioClip audioClip)
	{
		SetNewClip(audioClip);
		overrideClip = audioClip;
		activeSource.loop = false;
	}

	private void Update()
	{
		float num = Time.deltaTime * 1f / Mathf.Max(0.01f, fadeTime);
		if (!Singleton<GameManager>.Instance.inMenu && Singleton<PlaneContainer>.Instance != null)
		{
			if (!activeSource.isPlaying)
			{
				activeSource.loop = true;
				overrideClip = null;
			}
			if (overrideClip == null)
			{
				AudioClip backgroundMusic = Singleton<ContinentManager>.Instance.GetClosestContinent(Singleton<PlaneContainer>.Instance.transform.position).continentType.backgroundMusic;
				if (backgroundMusic != activeSource.clip)
				{
					SetNewClip(backgroundMusic);
				}
			}
			targetVolume = Mathf.Clamp01(Singleton<PlaneContainer>.Instance.transform.position.y / 250f);
			if (overrideClip != null)
			{
				targetVolume = 1f;
			}
			activeSource.volume = Mathf.MoveTowards(activeSource.volume, targetVolume, num);
			float t = activeSource.volume - Mathf.InverseLerp(0f, maxVolume, inactiveSource.volume);
			activeSource.volume = Mathf.Lerp(0f, maxVolume, t);
			inactiveSource.volume -= num;
			inactiveSource.volume = Mathf.Clamp01(inactiveSource.volume);
		}
		else
		{
			activeSource.volume = Mathf.Min(maxVolume, activeSource.volume);
			inactiveSource.volume = 0f;
		}
	}
}
