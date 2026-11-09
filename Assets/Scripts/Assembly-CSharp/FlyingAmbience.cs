using UnityEngine;

public class FlyingAmbience : MonoBehaviour
{
	public VolumeSource volumeSource;

	public float maxVolume;

	public bool inverse;

	private AudioSource source;

	private void Start()
	{
		source = GetComponent<AudioSource>();
	}

	private void Update()
	{
		if (!Singleton<GameManager>.Instance.inMenu && Singleton<PlaneContainer>.Instance != null)
		{
			if (volumeSource == VolumeSource.Height)
			{
				source.volume = Mathf.Clamp01(Singleton<PlaneContainer>.Instance.transform.position.y / 300f);
			}
			if (volumeSource == VolumeSource.Velocity)
			{
				source.volume = Mathf.Clamp01(Singleton<PlaneContainer>.Instance.GetVelocityMagintude() / 200f);
			}
		}
		source.volume = Mathf.Lerp(0f, maxVolume, source.volume);
		if (inverse)
		{
			source.volume = 1f - source.volume;
		}
	}
}
