using System.Collections.Generic;
using UnityEngine;

public class ResearchTreeItem : ScriptableObject
{
	public GameObject Prefab;

	public int Cost1;

	public int Cost2;

	[HideInInspector]
	public List<ResearchTreeItem> children = new List<ResearchTreeItem>();

	[HideInInspector]
	public Rect window;

	public Rect GetChildConnectionRect()
	{
		return new Rect(window.x - 10f, window.y + 75f - 10f, 10f, 20f);
	}

	public Rect GetParentConnectionRect()
	{
		return new Rect(window.x + 150f, window.y + 75f - 10f, 10f, 20f);
	}
}
