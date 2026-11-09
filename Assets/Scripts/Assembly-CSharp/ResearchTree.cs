using System.Collections.Generic;
using UnityEngine;

public class ResearchTree : ScriptableObject
{
	public List<ResearchTreeItem> items = new List<ResearchTreeItem>();

	public bool Contains(GameObject prefab)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].Prefab == prefab)
			{
				return true;
			}
		}
		return false;
	}
}
