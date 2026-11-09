using UnityEngine;

public abstract class TerrainBase : MonoBehaviour
{
	protected void UpdateMesh(HeightMap heightMap)
	{
		TerrainMeshData templateTerrain = Singleton<TerrainGenerationManager>.Instance.templateTerrain;
		int count = templateTerrain.verts.Count;
		Mathf.Sqrt(count);
		for (int i = 0; i < count; i++)
		{
			Vector3 value = templateTerrain.verts[i];
			value.y = heightMap.GetHeight(i);
			templateTerrain.verts[i] = value;
		}
		MeshDone(templateTerrain);
	}

	protected abstract void MeshDone(TerrainMeshData data);
}
