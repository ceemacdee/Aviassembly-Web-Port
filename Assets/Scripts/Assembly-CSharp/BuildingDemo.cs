using UnityEngine;

public class BuildingDemo : MonoBehaviour
{
	public GameObject[] fullVersionObjects;

	private void Awake()
	{
		for (int i = 0; i < fullVersionObjects.Length; i++)
		{
			fullVersionObjects[i].gameObject.SetActive(value: true);
		}
	}
}
