using UnityEngine;

public class PlaneParticle : MonoBehaviour
{
	public ParticleSystem[] systems;

	private float[] defaultRates;

	[HideInInspector]
	public Transform trackTransform;

	[HideInInspector]
	public Vector3 offset;

	[HideInInspector]
	public Quaternion particleRotation;

	protected virtual void Awake()
	{
		defaultRates = new float[systems.Length];
		for (int i = 0; i < systems.Length; i++)
		{
			ParticleSystem.EmissionModule emission = systems[i].emission;
			defaultRates[i] = emission.rateOverTime.constant;
		}
	}

	public virtual void SetValue(float value)
	{
		for (int i = 0; i < systems.Length; i++)
		{
			ParticleSystem.EmissionModule emission = systems[i].emission;
			emission.rateOverTime = defaultRates[i] * value;
		}
	}
}
