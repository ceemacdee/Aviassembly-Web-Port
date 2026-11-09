using UnityEngine;

public class EarthquakeBreakObject : MonoBehaviour
{
	public Rigidbody rb;

	private void Start()
	{
		rb.useGravity = false;
	}

	private void Update()
	{
		if (Singleton<StoryState>.Instance.earthquakeStarted && !Singleton<StoryState>.Instance.postEarthquake)
		{
			rb.useGravity = true;
		}
	}
}
