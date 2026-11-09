using System.Collections.Generic;
using UnityEngine;

public class AnimalSpawner : MonoBehaviour
{
	public Camera cam;

	public GameObject animalPrefab;

	public float spawnInterval;

	public int poolSize;

	private List<EagleFlight> pool = new List<EagleFlight>();

	private float t;

	private void Start()
	{
		PopulatePool();
		for (int i = 0; i < 3; i++)
		{
			SpawnBird();
		}
	}

	private void PopulatePool()
	{
		for (int i = 0; i < poolSize; i++)
		{
			GameObject gameObject = Object.Instantiate(animalPrefab);
			pool.Add(gameObject.GetComponent<EagleFlight>());
			gameObject.gameObject.SetActive(value: false);
			gameObject.transform.parent = base.transform;
		}
	}

	private EagleFlight GetObject()
	{
		for (int i = 0; i < poolSize; i++)
		{
			if (!pool[i].gameObject.activeInHierarchy)
			{
				pool[i].gameObject.SetActive(value: true);
				return pool[i];
			}
		}
		return null;
	}

	private void Update()
	{
		t += Time.deltaTime;
		if (t > spawnInterval)
		{
			SpawnBird();
		}
	}

	private void SpawnBird()
	{
		EagleFlight eagleFlight = GetObject();
		if (!(eagleFlight == null))
		{
			Vector3 position = cam.ViewportToWorldPoint(new Vector3(Random.Range(0f, 1f), Random.Range(0f, 1f), cam.farClipPlane));
			position.y = Mathf.Max(Random.Range(100, 120), cam.transform.position.y + (float)Random.Range(-100, 100));
			eagleFlight.transform.position = position;
			eagleFlight.transform.eulerAngles = new Vector3(0f, Random.Range(0, 360), 0f);
			eagleFlight.transform.localScale = Vector3.one * 0.75f;
			eagleFlight.Init(cam.transform);
			t = 0f;
		}
	}
}
