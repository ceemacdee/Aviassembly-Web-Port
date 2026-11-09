using System.Collections.Generic;
using UnityEngine;

public class PartContainer
{
	public const float RequiredSnapPointDistance = 0.05f;

	public Color red;

	private List<BuildingPart> parts = new List<BuildingPart>();

	private List<Transform> snapPoints = new List<Transform>();

	private List<Transform> usedSnapPoints = new List<Transform>();

	public void AddPart(BuildingPart part)
	{
		if (parts.Contains(part))
		{
			Debug.LogError("Part was already added");
			return;
		}
		for (int i = 0; i < snapPoints.Count; i++)
		{
			for (int j = 0; j < part.snapPoints.Length; j++)
			{
				if (Vector3.Distance(snapPoints[i].position, part.snapPoints[j].position) < 0.05f && !usedSnapPoints.Contains(snapPoints[i]))
				{
					usedSnapPoints.Add(snapPoints[i]);
					usedSnapPoints.Add(part.snapPoints[j]);
				}
			}
		}
		snapPoints.AddRange(part.snapPoints);
		parts.Add(part);
	}

	public void RemovePart(BuildingPart part)
	{
		if (parts.Contains(part))
		{
			RemoveSnapPoints(part);
			parts.Remove(part);
		}
		UpdateUsedSnapPoints();
	}

	public Transform GetSnapPoint(int index)
	{
		return snapPoints[index];
	}

	public int SnapPointCount()
	{
		return snapPoints.Count;
	}

	public bool SnapPointAttatched(Transform snapPoint)
	{
		return usedSnapPoints.Contains(snapPoint);
	}

	public BuildingPart SnappingPointToPart(Transform snappingPoint)
	{
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i].snapPoints == null)
			{
				continue;
			}
			for (int j = 0; j < parts[i].snapPoints.Length; j++)
			{
				if (parts[i].snapPoints[j] == snappingPoint)
				{
					return parts[i];
				}
			}
		}
		return null;
	}

	public BuildingPart SnappingPointToPart(Vector3 snappingPointPosition, BuildingPart ignorePart = null)
	{
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i].snapPoints == null || parts[i] == ignorePart)
			{
				continue;
			}
			for (int j = 0; j < parts[i].snapPoints.Length; j++)
			{
				if ((parts[i].snapPoints[j].position - snappingPointPosition).magnitude < 0.001f)
				{
					return parts[i];
				}
			}
		}
		return null;
	}

	public BuildingPart GetNeighbourPart(Transform snappingPoint)
	{
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i].snapPoints == null)
			{
				continue;
			}
			for (int j = 0; j < parts[i].snapPoints.Length; j++)
			{
				if (!(parts[i].snapPoints[j] == snappingPoint) && Vector3.Distance(parts[i].snapPoints[j].transform.position, snappingPoint.transform.position) < 0.001f)
				{
					return parts[i];
				}
			}
		}
		return null;
	}

	public BuildingPart GetMirrorPart(BuildingPart originalPart, PartPlacer placer)
	{
		float num = 0.5f;
		Vector3 mirroredPosition = placer.symmetryPlane.GetMirroredPosition(CustomMath.GetObjectCenter(originalPart.gameObject));
		float f = placer.symmetryPlane.DistanceFromPlane(mirroredPosition);
		ProceduralFuselage component = originalPart.GetComponent<ProceduralFuselage>();
		float num2 = float.MaxValue;
		BuildingPart result = null;
		for (int i = 0; i < parts.Count; i++)
		{
			Vector3 objectCenter = CustomMath.GetObjectCenter(parts[i].gameObject);
			float f2 = placer.symmetryPlane.DistanceFromPlane(objectCenter);
			if (parts[i] == originalPart || Mathf.Sign(f2) != Mathf.Sign(f) || Mathf.Abs(f2) < 0.01f)
			{
				continue;
			}
			if (component != null)
			{
				ProceduralFuselage component2 = parts[i].GetComponent<ProceduralFuselage>();
				if (component2 != null && (!component2.AppliedTransform.RoughlyEquals(component.AppliedTransform) || Quaternion.Angle(component2.transform.rotation, placer.symmetryPlane.GetMirroredRotation(originalPart.transform)) > 5f))
				{
					continue;
				}
			}
			float num3 = Vector3.Distance(objectCenter, mirroredPosition);
			if (num3 < num && num3 < num2 && parts[i].gameObject.name == originalPart.gameObject.name)
			{
				num2 = num3;
				result = parts[i];
			}
		}
		return result;
	}

	public void ResetContainer()
	{
		parts = new List<BuildingPart>();
		snapPoints = new List<Transform>();
		usedSnapPoints = new List<Transform>();
	}

	public void UpdateUsedSnapPoints()
	{
		for (int i = 0; i < snapPoints.Count; i++)
		{
			float num = float.MaxValue;
			for (int j = 0; j < snapPoints.Count; j++)
			{
				if (i != j)
				{
					float num2 = Vector3.Distance(snapPoints[i].position, snapPoints[j].position);
					if (num2 < num)
					{
						num = num2;
					}
				}
			}
			if (num > 0.05f && usedSnapPoints.Contains(snapPoints[i]))
			{
				usedSnapPoints.Remove(snapPoints[i]);
			}
			if (num < 0.05f && !usedSnapPoints.Contains(snapPoints[i]))
			{
				usedSnapPoints.Add(snapPoints[i]);
			}
		}
	}

	public void UpdateAttachment(BuildingPart part)
	{
		if (!part.isBasePart && part.hasBeenPlaced)
		{
			BuildingPart parent = part.parent;
			part.SetParent(null);
			BuildingPart buildingPart = part.FindOverlappingPartRecursive(this, parent);
			part.SetParent((buildingPart == null) ? null : buildingPart.gameObject);
		}
	}

	private bool CheckOverlap(BuildingPart originalPart, BuildingPart checkPart, Collider[] originalPartColliders)
	{
		if (checkPart == null || checkPart.gameObject == originalPart.gameObject || originalPart.IsChild(checkPart))
		{
			return false;
		}
		Collider[] componentsInChildren = checkPart.gameObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < originalPartColliders.Length; i++)
		{
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				Debug.DrawLine(componentsInChildren[j].bounds.center, originalPartColliders[i].bounds.center);
				Vector3 vector = (componentsInChildren[j].bounds.center - originalPartColliders[i].bounds.center).normalized * 0.033f;
				if (Physics.ComputePenetration(originalPartColliders[i], originalPartColliders[i].transform.position + vector, originalPartColliders[i].transform.rotation, componentsInChildren[j], componentsInChildren[j].transform.position, componentsInChildren[j].transform.rotation, out var _, out var _) && checkPart.IsAttatched())
				{
					return true;
				}
			}
		}
		return false;
	}

	public BuildingPart FindOverlappingPart(BuildingPart part, BuildingPart originalParent)
	{
		Collider[] componentsInChildren = part.gameObject.GetComponentsInChildren<Collider>();
		if (CheckOverlap(part, originalParent, componentsInChildren))
		{
			return originalParent;
		}
		for (int i = 0; i < parts.Count; i++)
		{
			if (CheckOverlap(part, parts[i], componentsInChildren))
			{
				return parts[i];
			}
		}
		return null;
	}

	private void RemoveSnapPoints(BuildingPart part)
	{
		for (int num = usedSnapPoints.Count - 1; num >= 0; num--)
		{
			for (int i = 0; i < part.snapPoints.Length; i++)
			{
				if ((usedSnapPoints[num].transform.position - part.snapPoints[i].transform.position).magnitude < 0.01f)
				{
					usedSnapPoints.RemoveAt(num);
					break;
				}
			}
		}
		for (int j = 0; j < part.snapPoints.Length; j++)
		{
			snapPoints.Remove(part.snapPoints[j]);
		}
	}

	public bool UpdateAttachmentFeedback()
	{
		bool result = false;
		for (int i = 0; i < parts.Count; i++)
		{
			if (!parts[i].IsAttatched())
			{
				Singleton<HighlightRenderer>.Instance.HighlightBuildingPart(parts[i], outline: false, highlight: true, red, 0.4f);
				result = true;
			}
		}
		return result;
	}

	public void UpdateEngineBacksides(BuildingPart currentMovingPart)
	{
		if (currentMovingPart != null)
		{
			TempAddPart(currentMovingPart);
		}
		for (int i = 0; i < parts.Count; i++)
		{
			PropellerVisual componentInChildren = parts[i].GetComponentInChildren<PropellerVisual>(includeInactive: true);
			if (componentInChildren != null && componentInChildren.backside != null)
			{
				componentInChildren.backside.SetActive(GetNeighbourPart(parts[i].snapPoints[0]) == null);
			}
		}
		if (currentMovingPart != null)
		{
			TempRemovePart(currentMovingPart);
		}
	}

	public int GetBasePartCount()
	{
		int num = 0;
		for (int i = 0; i < parts.Count; i++)
		{
			if (parts[i].isBasePart)
			{
				num++;
			}
		}
		return num;
	}

	public BuildingPart GetBasePart()
	{
		if (parts.Count == 0)
		{
			return null;
		}
		BuildingPart part = parts[0];
		return GetBasePartInternal(part);
	}

	private BuildingPart GetBasePartInternal(BuildingPart part)
	{
		if (part.parent == null)
		{
			return part;
		}
		return GetBasePartInternal(part.parent);
	}

	private void TempAddPart(BuildingPart part)
	{
		parts.Add(part);
		for (int i = 0; i < part.children.Count; i++)
		{
			TempAddPart(part.children[i]);
		}
	}

	private void TempRemovePart(BuildingPart part)
	{
		parts.Remove(part);
		for (int i = 0; i < part.children.Count; i++)
		{
			TempRemovePart(part.children[i]);
		}
	}
}
