using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AirportData : ScriptableObject
{
	[Header("Prefab")]
	[Space(3f)]
	public GameObject airportPrefab;

	[Space(10f)]
	[Header("Airport Settings")]
	[Space(3f)]
	public string airportName;

	public bool lockedCargo;

	public bool refuelAvailable;

	[HideInInspector]
	public CargoType[] cargoTypes;

	public bool baseAirport;

	public bool initialSpawnAirport;

	[Space(10f)]
	[Header("Positioning")]
	[Space(3f)]
	public bool rotationOverride;

	public float rotation;

	[Space(10f)]
	[Header("Terrain Blending")]
	[Space(3f)]
	public Vector2 footprint;

	public Vector2 treeSpawningFootprint;

	[Space(3f)]
	public float blendingDistance;

	public bool offshoreAirport;

	[HideInInspector]
	public Rect editorWindow;

	[Space(10f)]
	public List<QuestData> quests;

	public List<QuestData> baseQuests;

	public string landingMessage;

	public Vector3 GetAirportWorldPosition()
	{
		float x = editorWindow.position.x * 10f;
		float z = (0f - editorWindow.position.y) * 10f;
		return new Vector3(x, 0f, z);
	}
}
