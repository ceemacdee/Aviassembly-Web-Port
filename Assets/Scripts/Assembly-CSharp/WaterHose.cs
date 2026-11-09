using UnityEngine;

public class WaterHose : MonoBehaviour
{
	public ParticleSystem water;

	private void Update()
	{
		ParticleSystem.EmissionModule emission = water.emission;
		emission.enabled = false;
		base.transform.position = Singleton<PlaneContainer>.Instance.transform.position - Singleton<PlaneContainer>.Instance.transform.up * 1.5f;
		base.transform.rotation = Singleton<PlaneContainer>.Instance.transform.rotation;
	}
}
