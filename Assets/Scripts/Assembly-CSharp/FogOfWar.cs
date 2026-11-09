using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FogOfWar : Singleton<FogOfWar>
{
	public const int FogTextureSize = 1024;

	public float partitionSize;

	public Vector2Int currentPlayerPartition;

	public TMP_Text rewardText;

	public AudioDef rewardSound;

	private List<Vector2Int> discoveredPartitions = new List<Vector2Int>();

	private List<Vector2Int> discoveredAirports = new List<Vector2Int>();

	private Texture2D texture;

	private Color[] colors;

	private void Start()
	{
		texture = new Texture2D(1024, 1024);
		texture.filterMode = FilterMode.Point;
		colors = new Color[texture.width * texture.height];
	}

	public Vector2Int WorldPositionToPartition(Vector3 worldPosition)
	{
		int x = Mathf.RoundToInt(worldPosition.x / partitionSize);
		int y = Mathf.RoundToInt(worldPosition.z / partitionSize);
		return new Vector2Int(x, y);
	}

	private void Update()
	{
		if (GameManager.gameMode != GameMode.Building && !(Singleton<PlaneContainer>.Instance == null) && !(Singleton<ChunckManager>.Instance == null))
		{
			currentPlayerPartition = WorldPositionToPartition(Singleton<PlaneContainer>.Instance.transform.position);
			if (!discoveredPartitions.Contains(currentPlayerPartition))
			{
				discoveredPartitions.Add(currentPlayerPartition);
				GetFogOfWarTexture();
			}
			AddNewLocation(currentPlayerPartition);
		}
	}

	public void RemoveFogAtPosition(Vector3 worldPosition)
	{
		Vector2Int item = WorldPositionToPartition(worldPosition);
		if (!discoveredPartitions.Contains(item))
		{
			discoveredPartitions.Add(item);
			GetFogOfWarTexture();
		}
	}

	private void AddNewLocation(Vector2Int partition)
	{
		Vector3 position = new Vector3((float)partition.x * partitionSize, 0f, (float)partition.y * partitionSize);
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(position);
		if (closestAirport != null && !(closestAirport.position.magnitude < Singleton<ChunckManager>.Instance.chunkSize))
		{
			Vector3 position2 = closestAirport.position;
			Vector2Int item = WorldPositionToPartition(position2);
			bool num = position2.x > position.x - partitionSize * 0.5f && position2.x < position.x + partitionSize * 0.5f;
			bool flag = position2.z > position.z - partitionSize * 0.5f && position2.z < position.z + partitionSize * 0.5f;
			if ((num & flag) && !discoveredAirports.Contains(item))
			{
				discoveredAirports.Add(item);
			}
		}
	}

	public bool LocationDiscovered(Vector3 worldPos)
	{
		Vector2Int item = WorldPositionToPartition(worldPos);
		return discoveredPartitions.Contains(item);
	}

	public Texture GetFogOfWarTexture()
	{
		Map instance = Singleton<Map>.Instance;
		int num = colors.Length;
		int width = texture.width;
		int height = texture.height;
		Vector2Int vector2Int = new Vector2Int(width, height);
		Vector3 vector = new Vector3(partitionSize, 0f, partitionSize);
		Color clear = Color.clear;
		Color white = Color.white;
		for (int i = 0; i < num; i++)
		{
			colors[i] = white;
		}
		for (int j = 0; j < discoveredPartitions.Count; j++)
		{
			Vector3 vector2 = new Vector3(discoveredPartitions[j].x, 0f, discoveredPartitions[j].y) * partitionSize;
			vector2 -= vector * 0.5f;
			Vector3 worldPosition = vector2 + vector;
			Vector2 vector3 = instance.GetInterpolator(vector2) * vector2Int;
			Vector2 vector4 = instance.GetInterpolator(worldPosition) * vector2Int;
			if (vector4.x < 0f || vector4.y < 0f || vector3.x > (float)(width - 1) || vector3.y > (float)(height - 1))
			{
				continue;
			}
			for (int k = (int)vector3.y; k < (int)vector4.y; k++)
			{
				for (int l = (int)vector3.x; l < (int)vector4.x; l++)
				{
					int num2 = k * height + l;
					if (num2 <= num - 1 && l >= 0 && k >= 0 && l <= width - 1 && k <= height - 1)
					{
						colors[num2] = clear;
					}
				}
			}
		}
		texture.SetPixels(0, 0, texture.width, texture.height, colors);
		texture.Apply();
		return texture;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(discoveredPartitions.Count);
		for (int i = 0; i < discoveredPartitions.Count; i++)
		{
			writer.Write(discoveredPartitions[i].x);
			writer.Write(discoveredPartitions[i].y);
		}
		writer.Write(discoveredAirports.Count);
		for (int j = 0; j < discoveredAirports.Count; j++)
		{
			writer.Write(discoveredAirports[j].x);
			writer.Write(discoveredAirports[j].y);
		}
	}

	public void Load(GameDataReader reader)
	{
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			int x = reader.ReadInt();
			int y = reader.ReadInt();
			discoveredPartitions.Add(new Vector2Int(x, y));
		}
		int num2 = reader.ReadInt();
		for (int j = 0; j < num2; j++)
		{
			int x2 = reader.ReadInt();
			int y2 = reader.ReadInt();
			discoveredAirports.Add(new Vector2Int(x2, y2));
		}
	}
}
