using UnityEngine;

public class TimedCargoClock : MonoBehaviour
{
	public AudioSource audioSource;

	private void Update()
	{
		audioSource.volume = Singleton<GameManager>.Instance.clockVolume;
	}
}
