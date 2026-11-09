using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : Singleton<ObjectPool>
{
	public GameObject prefab;

	public int poolSize;

	public int activeObjectCount;

	private List<Tree> objects;

	private List<Tree> freeObjects = new List<Tree>();

	private void Start()
	{
		base.transform.position = Vector3.one;
		objects = new List<Tree>(5000);
		for (int i = 0; i < poolSize; i++)
		{
			Tree tree = AddObject();
			tree.SetEnabled(enabled: false);
			freeObjects.Add(tree);
		}
	}

	private Tree AddObject()
	{
		GameObject obj = Object.Instantiate(prefab);
		obj.transform.SetParent(base.transform);
		Tree component = obj.GetComponent<Tree>();
		objects.Add(component);
		return component;
	}

	public Tree GetObject(SurfaceDecoration surfaceDecoration)
	{
		activeObjectCount++;
		if (freeObjects.Count > 0)
		{
			Tree tree = freeObjects[0];
			tree.SetEnabled(enabled: true);
			tree.Init(surfaceDecoration);
			freeObjects.RemoveAt(0);
			return tree;
		}
		Tree tree2 = AddObject();
		tree2.Init(surfaceDecoration);
		return tree2;
	}

	public void ReleaseObject(Tree obj)
	{
		obj.SetEnabled(enabled: false);
		freeObjects.Add(obj);
		activeObjectCount--;
	}

	public void DisableAll()
	{
		for (int i = 0; i < objects.Count; i++)
		{
			objects[i].SetEnabled(enabled: false);
		}
	}
}
