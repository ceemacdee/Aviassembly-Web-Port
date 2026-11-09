using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
	public GameObject cloudPrefab;

	public int numClouds;

	public float radius;

	public float height;

	private GameObject[] clouds;

	private void Start()
	{
		clouds = new GameObject[numClouds];
		for (int i = 0; i < numClouds; i++)
		{
			GameObject gameObject = Object.Instantiate(cloudPrefab);
			clouds[i] = gameObject;
			SetCloudPostion(gameObject);
		}
	}

	private void Update()
	{
		for (int i = 0; i < clouds.Length; i++)
		{
			if (Vector3.Distance(Singleton<PlaneContainer>.Instance.transform.position, clouds[i].transform.position) > radius)
			{
				SetCloudPostion(clouds[i]);
			}
		}
	}

	private void SetCloudPostion(GameObject cloud)
	{
		cloud.transform.position = Singleton<PlaneContainer>.Instance.transform.position + Random.insideUnitSphere * radius;
		cloud.transform.position = new Vector3(cloud.transform.position.x, 200 + Random.Range(0, 300), cloud.transform.position.z);
	}
}
