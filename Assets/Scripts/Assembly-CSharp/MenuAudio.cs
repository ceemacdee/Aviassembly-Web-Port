using UnityEngine;

public class MenuAudio : MonoBehaviour
{
	public AudioSource[] sources;

	private float[] defaultVolume;

	public float volume { get; private set; }

	public void Init()
	{
		defaultVolume = new float[sources.Length];
		for (int i = 0; i < sources.Length; i++)
		{
			defaultVolume[i] = sources[i].volume;
		}
	}

	public void StartPlaying()
	{
		for (int i = 0; i < sources.Length; i++)
		{
			sources[i].Play();
		}
	}

	public void SetVolume(float volume)
	{
		for (int i = 0; i < sources.Length; i++)
		{
			sources[i].volume = defaultVolume[i] * volume;
		}
		this.volume = volume;
	}
}
