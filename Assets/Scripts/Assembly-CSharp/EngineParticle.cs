using UnityEngine;

[RequireComponent(typeof(Engine))]
public class EngineParticle : MonoBehaviour
{
	public GameObject particle;

	public float minSize;

	public float maxSize;

	private void Awake()
	{
		Deactivate();
	}

	public void Activate()
	{
		particle.gameObject.SetActive(value: true);
	}

	public void Deactivate()
	{
		particle.gameObject.SetActive(value: false);
	}

	public void SetThrottle(float throttle)
	{
		if (Mathf.Approximately(throttle, 0f))
		{
			Deactivate();
		}
		else
		{
			Activate();
		}
		particle.transform.localScale = new Vector3(1f, Mathf.Sign(base.transform.localScale.y), 1f) * Mathf.Lerp(minSize, maxSize, throttle);
	}
}
