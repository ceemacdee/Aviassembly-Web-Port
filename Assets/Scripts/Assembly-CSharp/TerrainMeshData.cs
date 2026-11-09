using System.Collections.Generic;
using UnityEngine;

public class TerrainMeshData
{
	public List<Vector3> verts = new List<Vector3>();

	public List<Vector2> uvs = new List<Vector2>();

	public List<int> triangles = new List<int>();

	public void AddTriangle(int a, int b, int c)
	{
		triangles.Add(a);
		triangles.Add(b);
		triangles.Add(c);
	}
}
