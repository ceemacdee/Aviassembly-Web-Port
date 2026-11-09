using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProceduralFuselageMesh))]
public class ProceduralFuselage : MonoBehaviour
{
	public ProceduralFuselageTransform appliedTransform;

	private ProceduralFuselageTransform currentTransform;

	[Space(10f)]
	public Transform p1;

	public Transform p2;

	public Transform collisionPoint;

	private Fuselage fuselage;

	private BuildingPart buildingPart;

	private FuselageData baseData;

	private FuselageData currentData;

	private ProceduralFuselageMesh mesh;

	private List<Vector3> childPositions = new List<Vector3>();

	public ProceduralFuselageTransform AppliedTransform
	{
		get
		{
			return appliedTransform;
		}
		private set
		{
		}
	}

	public ProceduralFuselageTransform CurrentTransform
	{
		get
		{
			return currentTransform;
		}
		private set
		{
		}
	}

	public void PreviewTransformation(ProceduralFuselageTransform transform)
	{
		mesh.UpdateMesh(transform);
		RecalculateChildren(transform);
		RecalculateCollisionAndSnapPoints(transform);
		buildingPart.price = GetPrice(transform);
		currentTransform = transform;
	}

	public void ApplyTransformation(ProceduralFuselageTransform transform, bool forceUpdate = false)
	{
		if (!appliedTransform.Equals(transform, includeRoundness: true) || forceUpdate)
		{
			PreviewTransformation(transform);
			appliedTransform = transform;
			mesh.UpdateBounds();
			UpdateStats();
			RecalculatePivot();
			currentTransform = appliedTransform;
			UpdateChildPosition();
		}
	}

	public void ResetTransformation()
	{
		ApplyTransformation(appliedTransform);
	}

	public void ResetToDefault()
	{
		ApplyTransformation(GetDefaultTransform());
	}

	public float GetPrice(ProceduralFuselageTransform settings)
	{
		return baseData.ChangeVolume(mesh.GetMeshVolume(settings)).price;
	}

	public float GetCargoSpace(ProceduralFuselageTransform settings)
	{
		return baseData.ChangeVolume(mesh.GetMeshVolume(settings)).cargoVolume;
	}

	private void Awake()
	{
		fuselage = GetComponent<Fuselage>();
		buildingPart = GetComponent<BuildingPart>();
		mesh = GetComponent<ProceduralFuselageMesh>();
		mesh.Init();
		baseData = new FuselageData(fuselage.cargoVolume, fuselage.weight, GetComponent<BuildingPart>().price, mesh.GetMeshVolume(GetDefaultTransform()));
		currentData = baseData;
		ResetToDefault();
	}

	private ProceduralFuselageTransform GetDefaultTransform()
	{
		return new ProceduralFuselageTransform
		{
			side1 = 
			{
				radius = new Vector2(0.5f, 0.5f),
				lengthOffset = new Vector3(0f, 0f, 0.5f),
				roundness = new Vector4(1f, 1f, 1f, 1f)
			},
			side2 = 
			{
				radius = new Vector2(0.5f, 0.5f),
				lengthOffset = new Vector3(0f, 0f, 0.5f),
				roundness = new Vector4(1f, 1f, 1f, 1f)
			}
		};
	}

	private void RecalculateCollisionAndSnapPoints(ProceduralFuselageTransform settings)
	{
		p1.position = settings.GetBaseOrigin1(base.transform);
		p2.position = settings.GetBaseOrigin2(base.transform);
		Vector3 a = p1.position - base.transform.right * settings.side1.radius.x;
		Vector3 b = p2.position - base.transform.right * settings.side2.radius.x;
		Vector3 position = Vector3.Lerp(a, b, 0.5f);
		collisionPoint.transform.position = position;
	}

	private void RecalculatePivot()
	{
		Vector3 vector = CustomMath.GetObjectCenter(base.gameObject) - base.transform.position;
		base.transform.position += vector;
		Vector3 vector2 = base.transform.InverseTransformDirection(vector);
		appliedTransform.side1.lengthOffset -= new Vector3(vector2.x, vector2.y * Mathf.Sign(base.transform.localScale.y), vector2.z);
		appliedTransform.side2.lengthOffset -= new Vector3(vector2.x, vector2.y * Mathf.Sign(base.transform.localScale.y), 0f - vector2.z);
		mesh.UpdateMesh(appliedTransform);
		mesh.UpdateBounds();
		RecalculateCollisionAndSnapPoints(appliedTransform);
	}

	private void UpdateStats()
	{
		float meshVolume = mesh.GetMeshVolume(appliedTransform);
		if (meshVolume != currentData.volume)
		{
			FuselageData fuselageData = baseData.ChangeVolume(meshVolume);
			fuselage.cargoVolume = fuselageData.cargoVolume;
			fuselage.weight = fuselageData.mass;
			buildingPart.price = fuselageData.price;
			if (buildingPart.hasBeenPlaced)
			{
				Singleton<MoneyManager>.Instance.ChangeMoneyAmount(currentData.price - fuselageData.price);
			}
			currentData = fuselageData;
		}
	}

	public void UpdateChildPosition()
	{
		if (appliedTransform.Equals(currentTransform))
		{
			childPositions.Clear();
			for (int i = 0; i < buildingPart.children.Count; i++)
			{
				childPositions.Add(buildingPart.children[i].transform.position);
			}
		}
	}

	private void RecalculateChildren(ProceduralFuselageTransform settings)
	{
		if (childPositions.Count != buildingPart.children.Count)
		{
			Debug.LogError("Child positions wrong length. Count: " + childPositions.Count + " Required Count: " + buildingPart.children.Count);
			return;
		}
		Vector3 baseOrigin = appliedTransform.GetBaseOrigin1(base.transform);
		Vector3 baseOrigin2 = appliedTransform.GetBaseOrigin2(base.transform);
		Vector3 baseOrigin3 = settings.GetBaseOrigin1(base.transform);
		Vector3 baseOrigin4 = settings.GetBaseOrigin2(base.transform);
		Vector3 normalized = (baseOrigin2 - baseOrigin).normalized;
		Vector3 normalized2 = (baseOrigin4 - baseOrigin3).normalized;
		float num = Vector3.Distance(baseOrigin, baseOrigin2);
		float num2 = Vector3.Distance(baseOrigin3, baseOrigin4);
		for (int i = 0; i < buildingPart.children.Count; i++)
		{
			Vector3 vector = childPositions[i];
			Vector3 closestPointOnLine = CustomMath.GetClosestPointOnLine(baseOrigin, normalized, vector);
			Vector3 vector2 = vector - closestPointOnLine;
			float num3 = Vector3.Distance(baseOrigin, closestPointOnLine);
			if (Vector3.Dot(normalized, baseOrigin - closestPointOnLine) > 0f)
			{
				num3 = 0f;
			}
			float num4 = num3 / num;
			if ((double)Mathf.Abs(num4) < 0.3)
			{
				num4 = 0f;
				vector2 = vector - baseOrigin;
			}
			if ((double)Mathf.Abs(num4) > 0.7)
			{
				num4 = 1f;
				vector2 = vector - baseOrigin2;
			}
			float num5 = num2 * num4;
			buildingPart.children[i].SetPosition(baseOrigin3 + normalized2 * num5 + vector2);
		}
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(appliedTransform.side1.radius);
		writer.Write(appliedTransform.side2.radius);
		writer.Write(appliedTransform.side1.lengthOffset);
		writer.Write(appliedTransform.side2.lengthOffset);
		writer.Write(appliedTransform.side1.roundness);
		writer.Write(appliedTransform.side2.roundness);
	}

	public void Load(GameDataReader reader)
	{
		if (reader.version < 9)
		{
			LegacyLoad(reader);
			return;
		}
		appliedTransform.side1.radius = reader.ReadVector2();
		appliedTransform.side2.radius = reader.ReadVector2();
		appliedTransform.side1.lengthOffset = reader.ReadVector3();
		appliedTransform.side2.lengthOffset = reader.ReadVector3();
		appliedTransform.side1.roundness = reader.ReadVector4();
		appliedTransform.side2.roundness = reader.ReadVector4();
		ApplyTransformation(appliedTransform, forceUpdate: true);
	}

	private void LegacyLoad(GameDataReader reader)
	{
		appliedTransform.side1.radius.x = reader.ReadFloat();
		appliedTransform.side2.radius.x = reader.ReadFloat();
		appliedTransform.side1.radius.y = appliedTransform.side1.radius.x;
		appliedTransform.side2.radius.y = appliedTransform.side2.radius.x;
		appliedTransform.side1.lengthOffset.z = reader.ReadFloat();
		appliedTransform.side2.lengthOffset.z = reader.ReadFloat();
		appliedTransform.side1.roundness = Vector4.one * reader.ReadFloat();
		appliedTransform.side2.roundness = Vector4.one * reader.ReadFloat();
		ApplyTransformation(appliedTransform, forceUpdate: true);
	}
}
