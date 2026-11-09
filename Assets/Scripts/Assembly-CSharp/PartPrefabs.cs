using System.Collections.Generic;
using UnityEngine;

public class PartPrefabs : MonoBehaviour
{
	public GameObject[] partPrefabs;

	public Texture2D[] decals;

	public GameObject decalPrefab;

	public ResearchTree researchTree;

	private static PartPrefabs instance;

	private List<GameObject> allPartPrefabs;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		allPartPrefabs = new List<GameObject>(partPrefabs);
		for (int i = 0; i < researchTree.items.Count; i++)
		{
			GameObject prefab = researchTree.items[i].Prefab;
			if (prefab != null && !allPartPrefabs.Contains(prefab))
			{
				allPartPrefabs.Add(prefab);
			}
		}
	}

	public static Texture2D GetDecalTexture(string decalName)
	{
		for (int i = 0; i < instance.decals.Length; i++)
		{
			if (instance.decals[i].name == decalName)
			{
				return instance.decals[i];
			}
		}
		return null;
	}

	public static GameObject GetPartPrefab(string partName)
	{
		partName = partName.Replace("(Clone)", "");
		for (int i = 0; i < instance.allPartPrefabs.Count; i++)
		{
			if (instance.allPartPrefabs[i].name == partName)
			{
				return instance.allPartPrefabs[i];
			}
		}
		Debug.LogError(partName);
		return null;
	}

	public static GameObject GetDecalPrefab()
	{
		return instance.decalPrefab;
	}

	public static Texture2D[] GetAllDecalTextures()
	{
		return instance.decals;
	}

	public static List<GameObject> GetAllPrefabs()
	{
		return instance.allPartPrefabs;
	}
}
