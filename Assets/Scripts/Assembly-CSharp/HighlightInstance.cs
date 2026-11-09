using System;
using UnityEngine;

[Serializable]
public class HighlightInstance
{
	public GameObject prefab;

	public GameObject mapPrefab;

	public float maxDistance;

	public float minDistance;

	public Vector3 position;

	public float fadeDistance;

	public bool enabled;

	public int priority;

	public bool alwaysVisable;

	public Vector2 mapOffset;

	public HighlightInstance(GameObject prefab, Vector3 position, float maxDistance, float minDistance, float fadeDistance = 50f, bool enabled = true)
	{
		this.prefab = prefab;
		this.position = position;
		this.maxDistance = maxDistance;
		this.minDistance = minDistance;
		this.fadeDistance = fadeDistance;
		this.enabled = enabled;
	}
}
