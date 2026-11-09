using System;
using UnityEngine;

public class Terrain : TerrainBase
{
	private MeshFilter filter;

	private MeshCollider meshCollider;

	private MeshRenderer meshRenderer;

	private float meshColliderGenerationDistance;

	private bool dirtyCollider;

	private bool threadedRequest;

	public HeightMap heightMap;

	public Chunk chunk;

	private bool containsAirport;

	public event Action TerrainComplete;

	public void Init(bool containsAirport, ContinentType continent)
	{
		GameManager instance = Singleton<GameManager>.Instance;
		filter = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();
		meshRenderer = GetComponent<MeshRenderer>();
		filter.mesh = null;
		this.containsAirport = containsAirport;
		UpdateTerrain();
		float num = Singleton<ChunckManager>.Instance.chunkSize * Singleton<ChunckManager>.Instance.chunkSize;
		meshColliderGenerationDistance = Mathf.Sqrt(num + num) + 200f;
		if (!instance.gameModeData.disableAirports)
		{
			meshRenderer.material = continent.terrainMaterial;
		}
		else
		{
			meshRenderer.material = Singleton<TerrainGenerationManager>.Instance.defaultMaterial;
		}
		Update();
	}

	private void Update()
	{
		if (Vector3.Distance(Singleton<PlaneContainer>.Instance.transform.position, base.transform.position) < meshColliderGenerationDistance && dirtyCollider)
		{
			meshCollider.sharedMesh = filter.sharedMesh;
		}
	}

	private void UpdateTerrain()
	{
		float num = base.transform.position.x / base.transform.localScale.x;
		float num2 = base.transform.position.z / base.transform.localScale.z;
		Vector2Int zero = Vector2Int.zero;
		zero.x = Mathf.RoundToInt(Singleton<PlaneContainer>.Instance.transform.position.x / 1000f);
		zero.y = Mathf.RoundToInt(Singleton<PlaneContainer>.Instance.transform.position.z / 1000f);
		threadedRequest = new Vector2Int((int)num, (int)num2) != zero;
		Singleton<TerrainGenerationManager>.Instance.RequestHeightmap(num, num2, DataReceived, threadedRequest);
		if (!threadedRequest)
		{
			Update();
		}
	}

	private void DataReceived(HeightMap data)
	{
		heightMap = data;
		UpdateMesh(heightMap);
		if (TerrainComplete != null)
		{
			TerrainComplete();
		}
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
			dirtyCollider = true;
		}
	}

	private void OnDestroy()
	{
		if (TerrainComplete != null)
		{
			TerrainComplete = null;
		}
		if (!(meshRenderer == null))
		{
			UnityEngine.Object.Destroy(meshRenderer.material);
			UnityEngine.Object.Destroy(filter.sharedMesh);
		}
	}
}
