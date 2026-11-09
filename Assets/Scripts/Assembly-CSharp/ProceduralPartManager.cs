using UnityEngine;

public class ProceduralPartManager : MonoBehaviour
{
	public PartPlacer partPlacer;

	public float selectionDistance;

	[HideInInspector]
	public ProceduralGizmoManager gizmoManager;

	private BuildingPart selectedPart;

	private ProceduralFuselage selectedFuselage;

	private Transform selectedSnapPoint;

	private ProceduralFuselageTransform currentTransform;

	private float costCumaltive;

	private void Start()
	{
		gizmoManager = GetComponent<ProceduralGizmoManager>();
	}

	private ProceduralFuselage GetMirrorFuselage()
	{
		if (selectedFuselage == null)
		{
			return null;
		}
		BuildingPart buildingPart = ((partPlacer.currentMirrorPart != null) ? partPlacer.currentMirrorPart : partPlacer.partContainer.GetMirrorPart(selectedPart, partPlacer));
		if (!(buildingPart != null) || !PartPlacer.mirrorMode)
		{
			return null;
		}
		return buildingPart.GetComponent<ProceduralFuselage>();
	}

	public void UpdateProcduralPartController()
	{
		gizmoManager.UpdateGizmoAnimations();
		ProceduralFuselage[] componentsInChildren = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<ProceduralFuselage>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].UpdateChildPosition();
		}
		if (partPlacer.currentMovingPart != null && !partPlacer.currentMovingPart.hasBeenPlaced)
		{
			CalculateOverrideRadius(partPlacer.currentMovingPart);
		}
		UpdateSelection();
		if (selectedFuselage == null)
		{
			return;
		}
		BuildingPart buildingPart = ((partPlacer.currentMirrorPart != null) ? partPlacer.currentMirrorPart : partPlacer.partContainer.GetMirrorPart(selectedPart, partPlacer));
		ProceduralFuselage proceduralFuselage = ((buildingPart != null && PartPlacer.mirrorMode) ? buildingPart.GetComponent<ProceduralFuselage>() : null);
		gizmoManager.UpdateGizmos(selectedPart, selectedSnapPoint, currentTransform);
		currentTransform = gizmoManager.currentTransform;
		PreviewTransfromation(proceduralFuselage);
		float num = GetTransformationCost(selectedFuselage, currentTransform) + GetTransformationCost(proceduralFuselage, currentTransform);
		bool flag = Singleton<MoneyManager>.Instance.HasEnoughMoney(num);
		float num2 = selectedFuselage.GetCargoSpace(selectedFuselage.AppliedTransform) - selectedFuselage.GetCargoSpace(currentTransform);
		costCumaltive += Mathf.Abs(num);
		if (MouseInput.GetMouseButton(0) && !Mathf.Approximately(costCumaltive, 0f) && selectedPart.hasBeenPlaced)
		{
			Vector3 position = gizmoManager.gizmoPosition + Vector3.up * 0.5f;
			Singleton<PriceFeedbackManager>.Instance.UpdatePersistentParticle(0f - num, 0f - num2, selectedFuselage.gameObject, position);
			if (proceduralFuselage != null)
			{
				Singleton<PriceFeedbackManager>.Instance.UpdatePersistentParticle(0f - num, 0f - num2, proceduralFuselage.gameObject, partPlacer.symmetryPlane.GetMirroredPosition(position));
			}
			if (!flag)
			{
				Singleton<PriceFeedbackManager>.Instance.ShowNotEnoughMoneyTooltip();
				Singleton<PriceFeedbackManager>.Instance.HighlightNotEnoughMoneyPart(selectedPart);
				if (proceduralFuselage != null)
				{
					Singleton<PriceFeedbackManager>.Instance.HighlightNotEnoughMoneyPart(buildingPart);
				}
			}
		}
		if (!MouseInput.GetMouseButtonUp(0))
		{
			return;
		}
		costCumaltive = 0f;
		if (flag)
		{
			ApplyTransformation(selectedFuselage, PartPlacer.mirrorMode ? proceduralFuselage : null, currentTransform);
		}
		else
		{
			ResetTransformation();
			Singleton<PriceFeedbackManager>.Instance.DestroyPersistentParticle(selectedFuselage.gameObject);
			if (buildingPart != null)
			{
				Singleton<PriceFeedbackManager>.Instance.DestroyPersistentParticle(buildingPart.gameObject);
			}
		}
		Singleton<PlaneStorage>.Instance.UpdateHistory();
		partPlacer.decalPlacer.RecalculateDecals((selectedFuselage != null) ? selectedFuselage.gameObject : null, (proceduralFuselage != null) ? proceduralFuselage.gameObject : null);
	}

	private void UpdateSelection()
	{
		if (gizmoManager.GizmoHover())
		{
			return;
		}
		selectedSnapPoint = null;
		if (selectedPart == null || partPlacer.currentSelectedPart == null || partPlacer.currentMovingPart != null)
		{
			selectedPart = null;
			selectedFuselage = null;
			gizmoManager.DeselectPart();
		}
		if (partPlacer.currentMovingPart != null || partPlacer.decalPlacer.selectedDecal != null)
		{
			return;
		}
		if (partPlacer.currentSelectedPart != null && (bool)partPlacer.currentSelectedPart.GetComponent<ProceduralFuselage>())
		{
			selectedPart = partPlacer.currentSelectedPart;
			ProceduralFuselage component = selectedPart.GetComponent<ProceduralFuselage>();
			if (!(component == selectedFuselage))
			{
				selectedFuselage = component;
				currentTransform = selectedFuselage.AppliedTransform;
				gizmoManager.SelectNewPart(selectedFuselage);
			}
			return;
		}
		Ray ray = BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition());
		Transform transform = null;
		float num = float.MaxValue;
		for (int i = 0; i < partPlacer.partContainer.SnapPointCount(); i++)
		{
			Transform snapPoint = partPlacer.partContainer.GetSnapPoint(i);
			if (partPlacer.partContainer.SnapPointAttatched(snapPoint))
			{
				continue;
			}
			ProceduralFuselage component2 = partPlacer.partContainer.SnappingPointToPart(snapPoint).GetComponent<ProceduralFuselage>();
			if (component2 != null)
			{
				float num2 = CustomMath.SignedDistanceToTransform(ray, snapPoint.position, includeDepth: false);
				float num3 = CustomMath.SignedDistanceToTransform(ray, snapPoint.position, includeDepth: true);
				ProceduralFuselageSide proceduralFuselageSide = ((Vector3.Distance(component2.AppliedTransform.GetBaseOrigin1(component2.transform), snapPoint.position) < 0.02f) ? component2.AppliedTransform.side1 : component2.AppliedTransform.side2);
				float num4 = selectionDistance * (Mathf.Max(Mathf.Max(proceduralFuselageSide.radius.x, proceduralFuselageSide.radius.y), 0.5f) / 0.5f);
				if (num2 < num4 && num2 > 0f && num3 < num)
				{
					num = num3;
					transform = snapPoint;
				}
			}
		}
		if (transform != null)
		{
			BuildingPart buildingPart = partPlacer.partContainer.SnappingPointToPart(transform);
			ProceduralFuselage component3 = buildingPart.GetComponent<ProceduralFuselage>();
			if (component3 != null)
			{
				selectedSnapPoint = transform;
				if (!(component3 == selectedFuselage))
				{
					selectedPart = buildingPart;
					selectedFuselage = component3;
					currentTransform = selectedFuselage.AppliedTransform;
					gizmoManager.SelectNewPart(selectedFuselage);
				}
			}
		}
		else
		{
			selectedPart = null;
			selectedFuselage = null;
		}
	}

	private Vector4 GetInvertedRoundness(Vector4 roudness)
	{
		return new Vector4(roudness.w, roudness.z, roudness.y, roudness.x);
	}

	private void CalculateOverrideRadius(BuildingPart part)
	{
		ProceduralFuselage component = part.GetComponent<ProceduralFuselage>();
		if (component == null)
		{
			return;
		}
		BuildingPart buildingPart = ((partPlacer.currentMirrorPart != null) ? partPlacer.currentMirrorPart : partPlacer.partContainer.GetMirrorPart(part, partPlacer));
		ProceduralFuselage proceduralFuselage = ((buildingPart != null) ? buildingPart.GetComponent<ProceduralFuselage>() : null);
		component.ResetToDefault();
		if (proceduralFuselage != null)
		{
			proceduralFuselage.ResetToDefault();
		}
		ProceduralFuselageTransform appliedTransform = component.AppliedTransform;
		BuildingPart buildingPart2 = partPlacer.partContainer.SnappingPointToPart(part.snapPoints[1].position, part);
		if (buildingPart2 != null && (bool)buildingPart2.GetComponent<ProceduralFuselage>())
		{
			ProceduralFuselage component2 = buildingPart2.GetComponent<ProceduralFuselage>();
			bool num = part.transform.forward == buildingPart2.transform.forward;
			Vector2 vector = (num ? component2.AppliedTransform.side1.radius : component2.AppliedTransform.side2.radius);
			Vector4 vector2 = (num ? component2.AppliedTransform.side1.roundness : component2.AppliedTransform.side2.roundness);
			if (Vector3.Distance(appliedTransform.side2.radius, vector) > Mathf.Epsilon && Vector3.Distance(appliedTransform.side1.radius, new Vector2(0.5f, 0.5f)) < Mathf.Epsilon)
			{
				appliedTransform.side1.radius = vector;
			}
			appliedTransform.side2.radius = vector;
			if (!num)
			{
				vector2 = GetInvertedRoundness(vector2);
			}
			appliedTransform.side2.roundness = vector2;
		}
		if (buildingPart2 != null && (bool)buildingPart2.GetComponent<ProceduralConnection>())
		{
			Vector2 radius = buildingPart2.GetComponent<ProceduralConnection>().radius;
			if (Vector3.Distance(appliedTransform.side2.radius, radius) > Mathf.Epsilon && Vector3.Distance(appliedTransform.side1.radius, new Vector2(0.5f, 0.5f)) < Mathf.Epsilon)
			{
				appliedTransform.side1.radius = radius;
			}
			appliedTransform.side2.radius = radius;
		}
		buildingPart2 = partPlacer.partContainer.SnappingPointToPart(part.snapPoints[0].position, part);
		if (buildingPart2 != null && (bool)buildingPart2.GetComponent<ProceduralFuselage>())
		{
			ProceduralFuselage component3 = buildingPart2.GetComponent<ProceduralFuselage>();
			bool num2 = part.transform.forward == buildingPart2.transform.forward;
			Vector2 vector3 = (num2 ? component3.AppliedTransform.side2.radius : component3.AppliedTransform.side1.radius);
			Vector4 vector4 = (num2 ? component3.AppliedTransform.side2.roundness : component3.AppliedTransform.side1.roundness);
			if (Vector3.Distance(appliedTransform.side1.radius, vector3) > Mathf.Epsilon && Vector3.Distance(appliedTransform.side2.radius, new Vector2(0.5f, 0.5f)) < Mathf.Epsilon)
			{
				appliedTransform.side2.radius = vector3;
			}
			appliedTransform.side1.radius = vector3;
			if (!num2)
			{
				vector4 = GetInvertedRoundness(vector4);
			}
			appliedTransform.side1.roundness = vector4;
		}
		if (buildingPart2 != null && (bool)buildingPart2.GetComponent<ProceduralConnection>())
		{
			Vector2 radius2 = buildingPart2.GetComponent<ProceduralConnection>().radius;
			if (Vector3.Distance(appliedTransform.side1.radius, radius2) > Mathf.Epsilon && Vector3.Distance(appliedTransform.side2.radius, new Vector2(0.5f, 0.5f)) < Mathf.Epsilon)
			{
				appliedTransform.side2.radius = radius2;
			}
			appliedTransform.side1.radius = radius2;
		}
		if (buildingPart2 != null && (bool)buildingPart2.GetComponent<ProceduralConnection>())
		{
			Vector4 roundness = buildingPart2.GetComponent<ProceduralConnection>().roundness;
			appliedTransform.side2.roundness = roundness;
			appliedTransform.side1.roundness = roundness;
		}
		if (buildingPart2 != null && (bool)buildingPart2.GetComponent<ProceduralFuselage>())
		{
			ProceduralFuselage component4 = buildingPart2.GetComponent<ProceduralFuselage>();
			bool num3 = part.transform.forward == buildingPart2.transform.forward;
			Vector4 vector5 = (num3 ? component4.AppliedTransform.side2.roundness : component4.AppliedTransform.side1.roundness);
			if (!num3)
			{
				vector5 = GetInvertedRoundness(vector5);
			}
			appliedTransform.side2.roundness = vector5;
			appliedTransform.side1.roundness = vector5;
		}
		float requiredAmount = GetTransformationCost(component, currentTransform) + GetTransformationCost(proceduralFuselage, currentTransform);
		if (Singleton<MoneyManager>.Instance.HasEnoughMoney(requiredAmount))
		{
			ApplyTransformation(component, proceduralFuselage, appliedTransform);
		}
	}

	public void ResetTransformation()
	{
		if (selectedFuselage != null)
		{
			currentTransform = selectedFuselage.AppliedTransform;
			gizmoManager.ResetGizmos(selectedFuselage);
			BuildingPart buildingPart = ((partPlacer.currentMirrorPart != null) ? partPlacer.currentMirrorPart : partPlacer.partContainer.GetMirrorPart(selectedPart, partPlacer));
			ProceduralFuselage mirrorFuselage = ((buildingPart != null) ? buildingPart.GetComponent<ProceduralFuselage>() : null);
			PreviewTransfromation(mirrorFuselage);
		}
	}

	private void PreviewTransfromation(ProceduralFuselage mirrorFuselage)
	{
		selectedFuselage.PreviewTransformation(currentTransform);
		if (mirrorFuselage != null && PartPlacer.mirrorMode)
		{
			mirrorFuselage.PreviewTransformation(currentTransform);
		}
	}

	private void ApplyTransformation(ProceduralFuselage proceduralFuselage, ProceduralFuselage mirrorFuselage, ProceduralFuselageTransform transform)
	{
		if (!(proceduralFuselage == null))
		{
			proceduralFuselage.ApplyTransformation(transform);
			if (mirrorFuselage != null)
			{
				mirrorFuselage.ApplyTransformation(transform);
			}
			ResetTransformation();
			partPlacer.partContainer.UpdateAttachment(PartPlacer.GetBuildingPartComponent(proceduralFuselage.gameObject));
			if (mirrorFuselage != null)
			{
				partPlacer.partContainer.UpdateAttachment(PartPlacer.GetBuildingPartComponent(mirrorFuselage.gameObject));
			}
		}
	}

	private float GetTransformationCost(ProceduralFuselage proceduralFuselage, ProceduralFuselageTransform transform)
	{
		if (proceduralFuselage == null)
		{
			return 0f;
		}
		float price = proceduralFuselage.GetPrice(proceduralFuselage.AppliedTransform);
		return proceduralFuselage.GetPrice(transform) - price;
	}
}
