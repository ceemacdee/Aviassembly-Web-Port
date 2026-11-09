using System.Collections.Generic;
using UnityEngine;

public class BuildingPart : MonoBehaviour, IPaintable
{
	public string partName;

	[Space(30f)]
	public Transform[] snapPoints;

	public Transform collisionPoint;

	public ItemFraming framing;

	public float price;

	[Header("Placement options")]
	public bool ignoreSnappingOnTangentPlane;

	public bool tryOrientUpwards;

	public bool tryMatchParentRotation;

	public bool lockRotation;

	public Vector3 forwardOrientation;

	public bool symetrical;

	public bool flipSymmetry;

	public float radius;

	public bool basePart;

	[HideInInspector]
	public bool isBasePart;

	[HideInInspector]
	public bool hasBeenPlaced;

	[HideInInspector]
	public List<BuildingPart> children = new List<BuildingPart>();

	[HideInInspector]
	public List<Decal> decals = new List<Decal>();

	public Quaternion baseRotation;

	public Quaternion rotationOffset;

	private PaintableRenderer[] paintableRenderers;

	public Color currentColor;

	public BuildingPart parent { get; private set; }

	private void Awake()
	{
		paintableRenderers = GetComponentsInChildren<PaintableRenderer>();
		for (int i = 0; i < paintableRenderers.Length; i++)
		{
			paintableRenderers[i].Awake();
		}
		if (paintableRenderers.Length != 0)
		{
			currentColor = paintableRenderers[0].currentColor;
			SetColor(currentColor, apply: true);
		}
	}

	private void Start()
	{
		rotationOffset = Quaternion.identity;
		if (forwardOrientation.magnitude < 1f)
		{
			Debug.LogWarning("Forward orientation not set for " + base.gameObject.name);
		}
	}

	public void SetColor(Color color, bool apply)
	{
		if (paintableRenderers == null)
		{
			return;
		}
		for (int i = 0; i < paintableRenderers.Length; i++)
		{
			if (apply)
			{
				paintableRenderers[i].ApplyColor(color);
				currentColor = color;
			}
			else
			{
				paintableRenderers[i].PreviewColor(color);
			}
		}
	}

	public void ResetColor()
	{
		if (paintableRenderers != null)
		{
			for (int i = 0; i < paintableRenderers.Length; i++)
			{
				paintableRenderers[i].ResetColor();
			}
		}
	}

	public Color GetCurrentColor()
	{
		return currentColor;
	}

	public bool IsAttatched()
	{
		if (isBasePart)
		{
			return true;
		}
		if (parent != null)
		{
			return parent.IsAttatched();
		}
		return false;
	}

	public bool IsChild(BuildingPart part)
	{
		if (children.Contains(part))
		{
			return true;
		}
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].IsChild(part))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsParent(BuildingPart part)
	{
		if (parent == part)
		{
			return true;
		}
		if (parent != null && parent.IsParent(part))
		{
			return true;
		}
		return false;
	}

	public void SetParent(GameObject parent)
	{
		if (parent == base.gameObject)
		{
			Debug.LogError("Can't assign self as parent. Object: " + parent.name);
			return;
		}
		BuildingPart buildingPart = ((parent == null) ? null : parent.GetComponent<BuildingPart>());
		if (this.parent != null)
		{
			this.parent.RemoveChild(this);
		}
		this.parent = buildingPart;
		if (parent != null)
		{
			buildingPart.AddChild(this);
		}
	}

	public void SetRotation(Quaternion rotation)
	{
		SetPositionAndRotation(base.transform.position, rotation);
	}

	public void SetPosition(Vector3 position)
	{
		SetPositionAndRotation(position, base.transform.rotation);
	}

	public void SetActive(bool value)
	{
		base.gameObject.gameObject.SetActive(value);
		for (int i = 0; i < decals.Count; i++)
		{
			decals[i].gameObject.SetActive(value);
		}
		for (int j = 0; j < children.Count; j++)
		{
			children[j].SetActive(value);
		}
	}

	public Vector3 GetForwardOrientation()
	{
		return (base.transform.TransformPoint(forwardOrientation.normalized) - base.transform.position).normalized;
	}

	public void FlipOrientation()
	{
		forwardOrientation *= -1f;
	}

	public float GetPrice()
	{
		float num = 0f;
		if (!hasBeenPlaced)
		{
			num += price;
		}
		for (int i = 0; i < children.Count; i++)
		{
			num += children[i].GetPrice();
		}
		return num;
	}

	public float GetCargoVolume()
	{
		float num = 0f;
		Fuselage componentInChildren = base.gameObject.GetComponentInChildren<Fuselage>();
		num += ((componentInChildren != null) ? componentInChildren.cargoVolume : 0f);
		for (int i = 0; i < children.Count; i++)
		{
			num += children[i].GetCargoVolume();
		}
		return num;
	}

	public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
	{
		SetTransformParentRecursive(base.transform);
		base.transform.position = position;
		base.transform.rotation = rotation;
		ResetTransformParent(base.transform.parent);
	}

	public void SetRotationRotator(Quaternion rotation)
	{
		Vector3 position = Singleton<PlaneContainer>.Instance.transform.position;
		Singleton<PlaneContainer>.Instance.transform.position = Vector3.zero;
		SetTransformParentRecursive(base.transform);
		base.transform.rotation = rotation;
		ResetTransformParent(base.transform.parent);
		Singleton<PlaneContainer>.Instance.transform.position = position;
	}

	public void SetScale(Vector3 localScale)
	{
		SetTransformParentRecursive(base.transform);
		base.transform.localScale = localScale;
		ResetTransformParent(base.transform.parent);
	}

	public void EnableColliders(bool value)
	{
		Collider[] componentsInChildren = base.gameObject.GetComponentsInChildren<Collider>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = value;
		}
	}

	public Vector3 GetCenter()
	{
		Vector3 total = Vector3.zero;
		int count = 0;
		GetCenterInteral(ref total, ref count);
		return total / count;
	}

	private void GetCenterInteral(ref Vector3 total, ref int count)
	{
		total += CustomMath.GetObjectCenter(base.gameObject);
		count++;
		for (int i = 0; i < children.Count; i++)
		{
			children[i].GetCenterInteral(ref total, ref count);
		}
	}

	public float DestoryPart(PartContainer container, ref float priceValue, ref float cargoVolume)
	{
		SetParent(null);
		if (hasBeenPlaced)
		{
			priceValue += price;
			cargoVolume += (GetComponent<Fuselage>() ? GetComponent<Fuselage>().cargoVolume : 0f);
		}
		container.RemovePart(this);
		for (int num = children.Count - 1; num >= 0; num--)
		{
			children[num].DestoryPart(container, ref priceValue, ref cargoVolume);
		}
		for (int num2 = decals.Count - 1; num2 >= 0; num2--)
		{
			decals[num2].DestroyDecal();
		}
		Object.Destroy(base.gameObject);
		return priceValue;
	}

	public BuildingPart MakeMirrorCopy(PartPlacer placer)
	{
		GameObject gameObject = Object.Instantiate(PartPrefabs.GetPartPrefab(base.gameObject.name));
		BuildingPart buildingPartComponent = PartPlacer.GetBuildingPartComponent(gameObject);
		gameObject.transform.SetParent(Singleton<PlaneContainer>.Instance.transform, worldPositionStays: true);
		gameObject.transform.position = placer.symmetryPlane.GetMirroredPosition(base.transform.position);
		gameObject.transform.rotation = placer.symmetryPlane.GetMirroredRotation(base.transform);
		gameObject.transform.localScale = placer.symmetryPlane.GetMirroredScale(base.transform.localScale);
		ProceduralFuselage component = gameObject.GetComponent<ProceduralFuselage>();
		if (component != null)
		{
			component.ApplyTransformation(GetComponent<ProceduralFuselage>().AppliedTransform);
		}
		for (int i = 0; i < decals.Count; i++)
		{
			placer.decalPlacer.MakeMirrorCopy(decals[i], buildingPartComponent);
		}
		for (int j = 0; j < children.Count; j++)
		{
			children[j].MakeMirrorCopy(placer).SetParent(gameObject);
		}
		return buildingPartComponent;
	}

	public BuildingPart FindOverlappingPartRecursive(PartContainer partContainer, BuildingPart originalParent)
	{
		BuildingPart buildingPart = partContainer.FindOverlappingPart(this, originalParent);
		if (buildingPart != null)
		{
			return buildingPart;
		}
		for (int i = 0; i < children.Count; i++)
		{
			buildingPart = children[i].FindOverlappingPartRecursive(partContainer, originalParent);
			if (buildingPart != null)
			{
				return buildingPart;
			}
		}
		return null;
	}

	public void RemovePartFromList(List<BuildingPart> list)
	{
		list.Remove(this);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].RemovePartFromList(list);
		}
	}

	private void OnDestroy()
	{
		SetParent(null);
	}

	private void AddChild(BuildingPart child)
	{
		if (!children.Contains(child))
		{
			children.Add(child);
		}
	}

	private void RemoveChild(BuildingPart child)
	{
		children.Remove(child);
	}

	private void ResetTransformParent(Transform parent)
	{
		for (int i = 0; i < children.Count; i++)
		{
			children[i].transform.SetParent(parent, worldPositionStays: true);
			children[i].ResetTransformParent(parent);
		}
		Transform transform = Singleton<DecalContainer>.Instance.transform;
		for (int j = 0; j < decals.Count; j++)
		{
			decals[j].transform.SetParent(transform, worldPositionStays: true);
		}
	}

	private void SetTransformParentRecursive(Transform parent)
	{
		for (int i = 0; i < children.Count; i++)
		{
			children[i].transform.SetParent(parent, worldPositionStays: true);
			children[i].SetTransformParentRecursive(parent);
		}
		for (int j = 0; j < decals.Count; j++)
		{
			decals[j].transform.SetParent(base.transform, worldPositionStays: true);
		}
	}

	public void Save(GameDataWriter writer, PlaneStorage planeStorage)
	{
		writer.Write(hasBeenPlaced);
		writer.Write(isBasePart);
		planeStorage.SaveBuildingPart(parent, writer);
		writer.Write(children.Count);
		for (int i = 0; i < children.Count; i++)
		{
			planeStorage.SaveBuildingPart(children[i], writer);
		}
		GetComponentInChildren<PlanePart>().Save(writer);
		writer.Write(currentColor);
		writer.Write(decals.Count);
		for (int j = 0; j < decals.Count; j++)
		{
			planeStorage.SaveDecal(decals[j], writer);
		}
	}

	public void Load(GameDataReader reader, PlaneStorage planeStorage)
	{
		hasBeenPlaced = reader.ReadBool();
		isBasePart = reader.ReadBool();
		BuildingPart buildingPart = planeStorage.LoadBuildingPart(reader, this);
		if (buildingPart != null)
		{
			SetParent(buildingPart.gameObject);
		}
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			planeStorage.LoadBuildingPart(reader, this).SetParent(base.gameObject);
		}
		GetComponentInChildren<PlanePart>().Load(reader);
		if (reader.version > 7)
		{
			SetColor(reader.ReadColor(), apply: true);
		}
		if (reader.version > 18)
		{
			int num2 = reader.ReadInt();
			for (int j = 0; j < num2; j++)
			{
				planeStorage.LoadDecal(reader).SetParent(this);
			}
		}
	}
}
