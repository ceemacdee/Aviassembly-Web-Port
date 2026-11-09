using System.Collections.Generic;
using UnityEngine;

public class MeshDecal : MonoBehaviour
{
	private struct Vertex
	{
		public Vector3 position;

		public Vector3 normal;

		public Vector2 uv;

		public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
		{
			this.position = position;
			this.normal = normal;
			this.uv = uv;
		}

		public static Vertex Lerp(Vertex A, Vertex B, float d)
		{
			return new Vertex
			{
				position = Vector3.Lerp(A.position, B.position, d),
				normal = Vector3.Lerp(A.normal, B.normal, d),
				uv = Vector2.Lerp(A.uv, B.uv, d)
			};
		}
	}

	private struct Triangle
	{
		public Vertex A;

		public Vertex B;

		public Vertex C;

		public Triangle(Vertex A, Vertex B, Vertex C)
		{
			this.A = A;
			this.B = B;
			this.C = C;
		}

		private void NewTriangle(Vertex A, Vertex B, Vertex C, float fA, float fB, float fC, ref Queue<Triangle> triangleList)
		{
			float d = (1f - fA) / (fB - fA);
			float d2 = (1f - fA) / (fC - fA);
			Vertex b = Vertex.Lerp(A, B, d);
			Vertex c = Vertex.Lerp(A, C, d2);
			triangleList.Enqueue(new Triangle
			{
				A = A,
				B = b,
				C = c
			});
		}

		private void NewQuad(Vertex A, Vertex B, Vertex C, float fA, float fB, float fC, ref Queue<Triangle> triangleList)
		{
			float d = (1f - fA) / (fB - fA);
			float d2 = (1f - fA) / (fC - fA);
			Vertex c = Vertex.Lerp(A, B, d);
			Vertex vertex = Vertex.Lerp(A, C, d2);
			triangleList.Enqueue(new Triangle
			{
				A = B,
				B = C,
				C = vertex
			});
			triangleList.Enqueue(new Triangle
			{
				A = B,
				B = vertex,
				C = c
			});
		}

		public bool Slice(Vector3 normal, ref Queue<Triangle> triangleList)
		{
			float num = Vector3.Dot(A.position, normal);
			float num2 = Vector3.Dot(B.position, normal);
			float num3 = Vector3.Dot(C.position, normal);
			if (num > 1f && num2 > 1f && num3 > 1f)
			{
				return true;
			}
			if (num < 1f && num2 > 1f && num3 > 1f)
			{
				NewTriangle(A, B, C, num, num2, num3, ref triangleList);
				return true;
			}
			if (num > 1f && num2 < 1f && num3 > 1f)
			{
				NewTriangle(B, C, A, num2, num3, num, ref triangleList);
				return true;
			}
			if (num > 1f && num2 > 1f && num3 < 1f)
			{
				NewTriangle(C, A, B, num3, num, num2, ref triangleList);
				return true;
			}
			if (num > 1f && num2 < 1f && num3 < 1f)
			{
				NewQuad(A, B, C, num, num2, num3, ref triangleList);
				return true;
			}
			if (num < 1f && num2 > 1f && num3 < 1f)
			{
				NewQuad(B, C, A, num2, num3, num, ref triangleList);
				return true;
			}
			if (num < 1f && num2 < 1f && num3 > 1f)
			{
				NewQuad(C, A, B, num3, num, num2, ref triangleList);
				return true;
			}
			return false;
		}
	}

	public Transform targetMeshTransform;

	public float offsetSteps = 0.01f;

	public int layer;

	public bool removeBackfaces = true;

	public bool serialized = true;

	public bool hideComponents = true;

	private MeshFilter meshFilter;

	private MeshRenderer meshRenderer;

	private MeshCollider meshCollider;

	private List<Vector3> vertices = new List<Vector3>();

	private List<Vector3> normals = new List<Vector3>();

	private List<Vector4> tangents = new List<Vector4>();

	private List<Vector2> originalUVs = new List<Vector2>();

	private List<int> triangles = new List<int>();

	private List<int> tempTriangles = new List<int>();

	private void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		meshCollider = GetComponent<MeshCollider>();
		targetMeshTransform = Singleton<PlaneContainer>.Instance.transform;
	}

	private bool IsInsideUnitCube(Vector3 p)
	{
		if (Mathf.Abs(p.x) <= 1f && Mathf.Abs(p.y) <= 1f)
		{
			return Mathf.Abs(p.z) <= 1f;
		}
		return false;
	}

	private void SetProjectionVolumeTransform()
	{
		Vector3 vector = base.transform.parent.position + base.transform.parent.up + base.transform.parent.right * base.transform.parent.localScale.x + base.transform.parent.forward * base.transform.parent.localScale.z;
		int num = 15;
		float num2 = base.transform.parent.localScale.x * 2f / (float)(num - 1);
		List<Vector3> list = new List<Vector3>();
		float num3 = 0f;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 origin = vector - base.transform.parent.right * i * num2 - base.transform.parent.forward * j * num2;
				if (Physics.Raycast(new Ray(origin, -base.transform.parent.up), out var hitInfo, 100f))
				{
					list.Add(hitInfo.normal);
					if (hitInfo.distance > num3)
					{
						num3 = hitInfo.distance;
					}
				}
			}
		}
		Vector3 zero = Vector3.zero;
		for (int k = 0; k < list.Count; k++)
		{
			zero += list[k];
		}
		base.transform.rotation = Quaternion.LookRotation(-zero.normalized);
		base.transform.localScale = new Vector3(1f, 1f, 1f);
	}

	private void Update()
	{
		base.transform.localScale = new Vector3(base.transform.localScale.x, base.transform.localScale.y, 1f);
	}

	public void Recalculate()
	{
		List<Triangle> list = new List<Triangle>();
		Queue<Triangle> queue = new Queue<Triangle>();
		Queue<Triangle> triangleList = new Queue<Triangle>();
		Bounds projectionVolumeAABB = GetProjectionVolumeAABB();
		Vector3[] array = new Vector3[6]
		{
			Vector3.left,
			Vector3.up,
			Vector3.right,
			Vector3.down,
			Vector3.back,
			Vector3.forward
		};
		MeshFilter[] componentsInChildren = targetMeshTransform.GetComponentsInChildren<MeshFilter>();
		Matrix4x4 worldToLocalMatrix = base.transform.worldToLocalMatrix;
		Transform transform = base.transform;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Transform transform2 = componentsInChildren[i].transform;
			Matrix4x4 localToWorldMatrix = transform2.localToWorldMatrix;
			_ = transform2.rotation;
			if (transform2 == meshFilter || componentsInChildren[i].sharedMesh == null || (bool)transform2.gameObject.GetComponent<MeshDecal>() || !componentsInChildren[i].GetComponent<MeshRenderer>().bounds.Intersects(projectionVolumeAABB))
			{
				continue;
			}
			if (!componentsInChildren[i].mesh.isReadable)
			{
				Debug.LogError(PartPlacer.GetBuildingPartComponent(componentsInChildren[i].gameObject).gameObject.name);
			}
			vertices = new List<Vector3>(componentsInChildren[i].sharedMesh.vertices);
			normals = new List<Vector3>(componentsInChildren[i].sharedMesh.normals);
			tangents = new List<Vector4>(componentsInChildren[i].sharedMesh.tangents);
			originalUVs = new List<Vector2>(componentsInChildren[i].sharedMesh.uv);
			triangles = new List<int>();
			tempTriangles = new List<int>();
			for (int j = 0; j < componentsInChildren[i].sharedMesh.subMeshCount; j++)
			{
				componentsInChildren[i].sharedMesh.GetTriangles(tempTriangles, j);
				triangles.AddRange(tempTriangles);
			}
			for (int k = 0; k < triangles.Count; k += 3)
			{
				Vector3 point = vertices[triangles[k]];
				Vector3 point2 = vertices[triangles[k + 1]];
				Vector3 point3 = vertices[triangles[k + 2]];
				Vector3 vector = worldToLocalMatrix.MultiplyPoint3x4(localToWorldMatrix.MultiplyPoint3x4(point));
				Vector3 vector2 = worldToLocalMatrix.MultiplyPoint3x4(localToWorldMatrix.MultiplyPoint3x4(point2));
				Vector3 vector3 = worldToLocalMatrix.MultiplyPoint3x4(localToWorldMatrix.MultiplyPoint3x4(point3));
				bool flag = false;
				foreach (Vector3 rhs in array)
				{
					float num = Vector3.Dot(vector, rhs);
					float num2 = Vector3.Dot(vector2, rhs);
					float num3 = Vector3.Dot(vector3, rhs);
					if (num > 1f && num2 > 1f && num3 > 1f)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
				Vector3 vector4 = transform.InverseTransformDirection(transform2.TransformDirection(normals[triangles[k]]));
				Vector3 vector5 = transform.InverseTransformDirection(transform2.TransformDirection(normals[triangles[k + 1]]));
				Vector3 vector6 = transform.InverseTransformDirection(transform2.TransformDirection(normals[triangles[k + 2]]));
				if (removeBackfaces)
				{
					Vector3 vector7 = vector4 + vector5 + vector6;
					if ((transform2.localScale.y < 0f && vector7.z < 0f) || (transform2.localScale.y > 0f && vector7.z > 0f))
					{
						continue;
					}
				}
				Vertex a = new Vertex(vector, vector4, originalUVs[triangles[k]]);
				Vertex b = new Vertex(vector2, vector5, originalUVs[triangles[k + 1]]);
				Vertex c = new Vertex(vector3, vector6, originalUVs[triangles[k + 2]]);
				if (transform2.localScale.y < 0f)
				{
					a = new Vertex(vector, -vector4, originalUVs[triangles[k]]);
					b = new Vertex(vector3, -vector6, originalUVs[triangles[k + 2]]);
					c = new Vertex(vector2, -vector5, originalUVs[triangles[k + 1]]);
				}
				if (IsInsideUnitCube(a.position) && IsInsideUnitCube(b.position) && IsInsideUnitCube(c.position))
				{
					list.Add(new Triangle(a, b, c));
					continue;
				}
				queue.Clear();
				triangleList.Clear();
				queue.Enqueue(new Triangle(a, b, c));
				for (int m = 0; m < array.Length; m++)
				{
					Vector3 normal = array[m];
					while (queue.Count > 0)
					{
						Triangle item = queue.Dequeue();
						if (!item.Slice(normal, ref triangleList))
						{
							triangleList.Enqueue(item);
						}
					}
					if (m != array.Length - 1)
					{
						Queue<Triangle> queue2 = queue;
						queue = triangleList;
						triangleList = queue2;
					}
				}
				while (triangleList.Count > 0)
				{
					list.Add(triangleList.Dequeue());
				}
			}
		}
		vertices.Clear();
		normals.Clear();
		tangents.Clear();
		originalUVs.Clear();
		triangles.Clear();
		int num4 = 0;
		foreach (Triangle item2 in list)
		{
			vertices.Add(item2.A.position);
			vertices.Add(item2.B.position);
			vertices.Add(item2.C.position);
			normals.Add(item2.A.normal);
			normals.Add(item2.B.normal);
			normals.Add(item2.C.normal);
			originalUVs.Add(item2.A.uv);
			originalUVs.Add(item2.B.uv);
			originalUVs.Add(item2.C.uv);
			triangles.Add(num4++);
			triangles.Add(num4++);
			triangles.Add(num4++);
		}
		List<Vector2> list2 = new List<Vector2>();
		for (int n = 0; n < vertices.Count; n++)
		{
			list2.Add(new Vector2(vertices[n].x * 0.5f + 0.5f, vertices[n].y * 0.5f + 0.5f));
			vertices[n] += normals[n] * offsetSteps * (layer + 1);
		}
		Object.Destroy(meshFilter.sharedMesh);
		meshFilter.sharedMesh = new Mesh
		{
			name = "decal" + Random.Range(0, 10000)
		};
		meshFilter.sharedMesh.Clear();
		meshFilter.sharedMesh.SetVertices(vertices);
		meshFilter.sharedMesh.SetNormals(normals);
		meshFilter.sharedMesh.SetTangents(tangents);
		meshFilter.sharedMesh.SetTriangles(triangles, 0);
		meshFilter.sharedMesh.SetUVs(0, list2);
		meshFilter.sharedMesh.SetUVs(1, originalUVs);
		meshFilter.sharedMesh.Optimize();
		meshCollider.sharedMesh = meshFilter.sharedMesh;
		meshRenderer.materials[0].renderQueue = 3000 + layer;
	}

	private void OnDestroy()
	{
		Object.Destroy(meshFilter.sharedMesh);
		Object.Destroy(meshRenderer.materials[0]);
	}

	public Bounds GetProjectionVolumeAABB()
	{
		Vector3 vector = new Vector3(Mathf.Abs(base.transform.localScale.x), Mathf.Abs(base.transform.localScale.y), Mathf.Abs(base.transform.localScale.z)) * 2f;
		Matrix4x4 matrix4x = Matrix4x4.Rotate(base.transform.rotation);
		return new Bounds(size: new Vector3(Mathf.Abs(matrix4x.m00) * vector.x + Mathf.Abs(matrix4x.m01) * vector.y + Mathf.Abs(matrix4x.m02) * vector.z, Mathf.Abs(matrix4x.m10) * vector.x + Mathf.Abs(matrix4x.m11) * vector.y + Mathf.Abs(matrix4x.m12) * vector.z, Mathf.Abs(matrix4x.m20) * vector.x + Mathf.Abs(matrix4x.m21) * vector.y + Mathf.Abs(matrix4x.m22) * vector.z), center: base.transform.position);
	}

	public Bounds GetMeshAABB()
	{
		return meshRenderer.bounds;
	}

	private void OnDrawGizmos()
	{
		Vector3 vector = base.transform.TransformPoint(new Vector3(-1f, -1f, -1f));
		Vector3 vector2 = base.transform.TransformPoint(new Vector3(1f, -1f, -1f));
		Vector3 vector3 = base.transform.TransformPoint(new Vector3(-1f, 1f, -1f));
		Vector3 vector4 = base.transform.TransformPoint(new Vector3(1f, 1f, -1f));
		Vector3 vector5 = base.transform.TransformPoint(new Vector3(-1f, -1f, 1f));
		Vector3 vector6 = base.transform.TransformPoint(new Vector3(1f, -1f, 1f));
		Vector3 vector7 = base.transform.TransformPoint(new Vector3(-1f, 1f, 1f));
		Vector3 vector8 = base.transform.TransformPoint(new Vector3(1f, 1f, 1f));
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(vector, vector2);
		Gizmos.DrawLine(vector2, vector4);
		Gizmos.DrawLine(vector4, vector3);
		Gizmos.DrawLine(vector3, vector);
		Gizmos.DrawLine(vector, vector5);
		Gizmos.DrawLine(vector2, vector6);
		Gizmos.DrawLine(vector4, vector8);
		Gizmos.DrawLine(vector3, vector7);
		Gizmos.color = Color.black;
		Gizmos.DrawLine(vector5, vector6);
		Gizmos.DrawLine(vector6, vector8);
		Gizmos.DrawLine(vector8, vector7);
		Gizmos.DrawLine(vector7, vector5);
		Gizmos.color = Color.red;
		Bounds projectionVolumeAABB = GetProjectionVolumeAABB();
		Gizmos.DrawWireCube(projectionVolumeAABB.center, projectionVolumeAABB.size);
	}
}
