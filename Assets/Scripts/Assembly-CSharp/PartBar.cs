using UnityEngine;

public class PartBar : MonoBehaviour
{
	public GameObject[] prefabs;

	public GameObject[] fullVersionPrefabs;

	public GameObject buttonPrefab;

	public PartPlacer placer;

	private void Awake()
	{
		SpawnButtons(prefabs);
		SpawnButtons(fullVersionPrefabs);
	}

	private void SpawnButtons(GameObject[] prefabs)
	{
		if (prefabs != null)
		{
			for (int i = 0; i < prefabs.Length; i++)
			{
				GameObject obj = Object.Instantiate(buttonPrefab);
				obj.transform.SetParent(base.transform);
				obj.transform.localScale = Vector3.one;
				obj.GetComponent<PartButton>().Init(placer, prefabs[i], Singleton<IconGenerator>.Instance);
			}
		}
	}
}
