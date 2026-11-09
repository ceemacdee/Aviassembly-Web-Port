using System.Collections.Generic;
using UnityEngine;

public class MenuTerrain : TerrainBase
{
	public SurfaceDecoration[] treePrefab;

	private MeshFilter filter;

	private List<Tree> trees = new List<Tree>();

	private ObjectPool surfaceDecorationPool;

	public HeightMap heightMap;

	public Transform cam;

	public int increment = 25;

	private void Awake()
	{
		filter = GetComponent<MeshFilter>();
		float xPos = base.transform.position.x / base.transform.localScale.x;
		float zPos = base.transform.position.z / base.transform.localScale.z;
		Singleton<TerrainGenerationManager>.Instance.RequestHeightmap(xPos, zPos, DataReceived);
		surfaceDecorationPool = Singleton<ObjectPool>.Instance;
	}

	private void SpawnTree(Vector2 spawnCoordinate, SurfaceDecoration treePrefab)
	{
		float maxInclusive = (float)treePrefab.density + treePrefab.randomizationOffset;
		if (Random.Range(0, 5) == 0)
		{
			return;
		}
		Vector3 vector = new Vector3(spawnCoordinate.x, 0f, spawnCoordinate.y);
		vector += new Vector3(Random.Range(0f, maxInclusive), 0f, Random.Range(0f, maxInclusive));
		if (!(vector.x < -500f) && !(vector.x > 500f) && !(vector.z < -500f) && !(vector.z > 500f))
		{
			Vector3 position = vector + base.transform.position;
			position.y = heightMap.GetHeight(1000, new Vector2(vector.x, vector.z) + new Vector2(500f, 500f));
			Vector3 normal = heightMap.GetNormal(1000f, new Vector2(vector.x, vector.z) + new Vector2(500f, 500f));
			float num = 1f - Vector3.Dot(normal, Vector3.up);
			if (!(position.y < -2f) && !(num < treePrefab.minSlope) && !(num > treePrefab.maxSlope))
			{
				Tree tree = surfaceDecorationPool.GetObject(treePrefab);
				tree.transform.position = position;
				tree.transform.rotation = Quaternion.Euler(Random.Range(10, -10), Random.Range(0, 360), Random.Range(10, -10));
				tree.transform.localScale = Vector3.one * Random.Range(1.5f, 2f);
				trees.Add(tree);
			}
		}
	}

	public void UpdateTreeSpawning()
	{
		for (int i = 0; i < treePrefab.Length; i++)
		{
			int density = treePrefab[i].density;
			for (int j = -500; j < 500; j += density)
			{
				for (int k = -500; k < 500; k += density)
				{
					SpawnTree(new Vector2(j, k), treePrefab[i]);
				}
			}
		}
	}

	private void DataReceived(HeightMap data)
	{
		heightMap = data;
		UpdateMesh(data);
		UpdateTreeSpawning();
	}

	protected override void MeshDone(TerrainMeshData data)
	{
		if (!(filter == null))
		{
			Mesh sharedMesh = new Mesh();
			filter.sharedMesh = sharedMesh;
			filter.sharedMesh.vertices = data.verts.ToArray();
			filter.sharedMesh.triangles = data.triangles.ToArray();
			filter.sharedMesh.uv = data.uvs.ToArray();
			filter.sharedMesh.RecalculateNormals();
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(filter.sharedMesh);
		ObjectPool instance = Singleton<ObjectPool>.Instance;
		if (!(instance != null))
		{
			return;
		}
		for (int i = 0; i < trees.Count; i++)
		{
			if (trees[i].gameObject != null)
			{
				instance.ReleaseObject(trees[i]);
			}
		}
	}
}
