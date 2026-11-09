using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralFuselageMesh : MonoBehaviour
{
	private class MeshData
	{
		public List<Vector3> vertices = new List<Vector3>();

		public List<int> triangles = new List<int>();

		public List<Vector2> uvs = new List<Vector2>();

		public void Clear()
		{
			vertices.Clear();
			triangles.Clear();
			uvs.Clear();
		}

		public void AddMesh(MeshData data, bool clockWise)
		{
			if (!clockWise)
			{
				data.InvertWindingOrder();
			}
			for (int i = 0; i < data.triangles.Count; i++)
			{
				data.triangles[i] += vertices.Count;
			}
			FillUVs();
			vertices.AddRange(data.vertices);
			triangles.AddRange(data.triangles);
			uvs.AddRange(data.uvs);
		}

		private void FillUVs()
		{
			if (vertices.Count != uvs.Count)
			{
				int num = vertices.Count - uvs.Count;
				for (int i = 0; i < num; i++)
				{
					uvs.Add(new Vector2(0.2f, 0.2f));
				}
			}
		}

		public void InvertWindingOrder()
		{
			for (int i = 0; i < triangles.Count; i += 3)
			{
				int value = triangles[i + 2];
				triangles[i + 2] = triangles[i];
				triangles[i] = value;
			}
		}

		public Mesh ToMesh(Mesh mesh)
		{
			FillUVs();
			mesh.SetVertices(vertices.ToArray());
			mesh.SetUVs(0, uvs.ToArray());
			mesh.SetTriangles(triangles.ToArray(), 0);
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			return mesh;
		}
	}

	public int resolution;

	private MeshFilter meshFilter;

	private MeshCollider meshCollider;

	private Mesh mesh;

	private static List<Tuple<ProceduralFuselageTransform, float>> volumeCache = new List<Tuple<ProceduralFuselageTransform, float>>();

	private int currentCacheIndex;

	public ProceduralFuselageTransform currentTransform { get; private set; }

	public void Init()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();
		meshFilter.sharedMesh = new Mesh();
		meshCollider.sharedMesh = meshFilter.sharedMesh;
	}

	public void UpdateBounds()
	{
		meshFilter.sharedMesh.RecalculateBounds();
	}

	public void UpdateMesh(ProceduralFuselageTransform settings)
	{
		if (!settings.Equals(currentTransform, includeRoundness: true))
		{
			GetMeshDataFromSettings(settings).ToMesh(meshFilter.sharedMesh);
			meshCollider.sharedMesh = meshFilter.sharedMesh;
			currentTransform = settings;
		}
	}

	private MeshData GetMeshDataFromSettings(ProceduralFuselageTransform settings)
	{
		float height = Mathf.Abs(settings.side1.lengthOffset.z + settings.side2.lengthOffset.z);
		List<Vector3> list = GenerateVertices(settings.side1.radius, new Vector3(settings.side1.lengthOffset.x, settings.side1.lengthOffset.y, settings.side1.lengthOffset.z), settings.side1.roundness, resolution);
		List<Vector3> list2 = GenerateVertices(settings.side2.radius, new Vector3(settings.side2.lengthOffset.x, settings.side2.lengthOffset.y, 0f - settings.side2.lengthOffset.z), settings.side2.roundness, resolution);
		MeshData meshData = new MeshData();
		meshData.uvs.AddRange(SetUVs(list, top: true, height));
		meshData.uvs.AddRange(SetUVs(list2, top: false, height));
		meshData.vertices.AddRange(list);
		meshData.vertices.AddRange(list2);
		BridgeVertices(meshData);
		MeshData data = FillVertices(list);
		MeshData data2 = FillVertices(list2);
		MeshData meshData2 = new MeshData();
		meshData2.AddMesh(meshData, clockWise: false);
		meshData2.AddMesh(data, clockWise: true);
		meshData2.AddMesh(data2, clockWise: false);
		return meshData2;
	}

	private float GetVolumeFromCache(ProceduralFuselageTransform settings)
	{
		for (int i = 0; i < volumeCache.Count; i++)
		{
			if (settings.Equals(volumeCache[i].Item1, includeRoundness: true))
			{
				return volumeCache[i].Item2;
			}
		}
		return -1f;
	}

	private void AddToCache(ProceduralFuselageTransform settings, float volume)
	{
		if (!(GetVolumeFromCache(settings) >= 0f))
		{
			Tuple<ProceduralFuselageTransform, float> item = new Tuple<ProceduralFuselageTransform, float>(settings, Mathf.Abs(volume));
			if (volumeCache.Count > 15)
			{
				volumeCache.RemoveAt(0);
			}
			volumeCache.Add(item);
		}
	}

	public float GetMeshVolume(ProceduralFuselageTransform settings)
	{
		float volumeFromCache = GetVolumeFromCache(settings);
		if (volumeFromCache >= 0f)
		{
			return volumeFromCache;
		}
		MeshData meshDataFromSettings = GetMeshDataFromSettings(settings);
		int num = meshDataFromSettings.triangles.Count / 3;
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			int num3 = i * 3;
			Vector3 point = meshDataFromSettings.vertices[meshDataFromSettings.triangles[num3]];
			Vector3 point2 = meshDataFromSettings.vertices[meshDataFromSettings.triangles[num3 + 1]];
			Vector3 point3 = meshDataFromSettings.vertices[meshDataFromSettings.triangles[num3 + 2]];
			num2 += SignedVolumeOfTriangle(point, point2, point3);
		}
		AddToCache(settings, Mathf.Abs(num2));
		return Mathf.Abs(num2);
	}

	private List<Vector3> GenerateVertices(Vector2 size, Vector3 position, Vector4 bevelRadius, int resolution)
	{
		List<Vector3> list = new List<Vector3>();
		List<Vector3> list2 = new List<Vector3>
		{
			new Vector3(0f - size.x + position.x, 0f - size.y + position.y, position.z),
			new Vector3(0f - size.x + position.x, size.y + position.y, position.z),
			new Vector3(size.x + position.x, size.y + position.y, position.z),
			new Vector3(size.x + position.x, 0f - size.y + position.y, position.z)
		};
		float num = size.x * 2f / (float)resolution;
		float num2 = size.y * 2f / (float)resolution;
		int num3 = 0;
		Vector3 vector = list2[0];
		Vector3 vector2 = (list2[1] - vector).normalized;
		for (int i = 0; i < resolution * 4; i++)
		{
			list.Add(vector);
			float num4 = ((Mathf.Abs(vector2.x) < 0.1f) ? num2 : num);
			vector += vector2 * num4;
			num3++;
			if (num3 == resolution)
			{
				vector2 = Quaternion.Euler(0f, 0f, -90f) * vector2;
				num3 = 0;
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			Vector3 vector3 = CustomMath.ClosestPoint(list[j], list2);
			int index = CustomMath.ClosestPointIndex(list[j], list2);
			float num5 = Vector3.Distance(list[j], vector3);
			float num6 = Mathf.Clamp01(bevelRadius[index]);
			float num7 = Mathf.Min(size.x, size.y);
			if (num5 < num6 * num7)
			{
				float num8 = num6 * num7;
				Vector3 normalized = new Vector3(Mathf.Sign(vector3.x - position.x), Mathf.Sign(vector3.y - position.y), 0f).normalized;
				Vector3 vector4 = vector3 - normalized * Mathf.Sqrt(num8 * num8 + num8 * num8);
				Vector3 normalized2 = (list[j] - vector4).normalized;
				list[j] = vector4 + normalized2 * num8;
			}
		}
		return list;
	}

	private List<Vector2> SetUVs(List<Vector3> vertices, bool top, float height)
	{
		List<Vector2> list = new List<Vector2>();
		for (int i = 0; i < vertices.Count; i++)
		{
			list.Add(new Vector2(0.2f, top ? height : 0f));
		}
		return list;
	}

	private MeshData FillVertices(List<Vector3> vertices)
	{
		MeshData meshData = new MeshData();
		List<Vector3> list = new List<Vector3>();
		list.Add(CustomMath.GetCenter(vertices));
		list.AddRange(vertices);
		meshData.vertices = list;
		List<int> list2 = new List<int>();
		for (int i = 1; i < vertices.Count; i++)
		{
			list2.Add(i);
			list2.Add(0);
			list2.Add((i + 1 > vertices.Count) ? 1 : (i + 1));
		}
		list2.Add(vertices.Count);
		list2.Add(0);
		list2.Add(1);
		meshData.triangles = list2;
		return meshData;
	}

	private void BridgeVertices(MeshData data)
	{
		int num = data.vertices.Count / 2;
		for (int i = 0; i < num; i++)
		{
			data.triangles.Add(num + i);
			data.triangles.Add(i + 1);
			data.triangles.Add(i);
		}
		for (int j = num; j < data.vertices.Count; j++)
		{
			data.triangles.Add(j - 1);
			data.triangles.Add(j);
			data.triangles.Add(j - num);
		}
	}

	private float SignedVolumeOfTriangle(Vector3 point1, Vector3 point2, Vector3 point3)
	{
		float num = point3.x * point2.y * point1.z;
		float num2 = point2.x * point3.y * point1.z;
		float num3 = point3.x * point1.y * point2.z;
		float num4 = point1.x * point3.y * point2.z;
		float num5 = point2.x * point1.y * point3.z;
		float num6 = point1.x * point2.y * point3.z;
		return 1f / 6f * (0f - num + num2 + num3 - num4 - num5 + num6);
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(mesh);
	}

	public void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		if (meshFilter != null && meshFilter.sharedMesh != null)
		{
			Gizmos.DrawWireCube(base.transform.TransformPoint(meshFilter.sharedMesh.bounds.center), meshFilter.sharedMesh.bounds.size);
		}
		Gizmos.color = Color.white;
	}
}
