using UnityEngine;

public class ProceduralGizmoManager : MonoBehaviour
{
	public Color black;

	public Color white;

	public PartPlacer partPlacer;

	public ProceduralGizmo gizmo1;

	public ProceduralGizmo gizmo2;

	[HideInInspector]
	public Vector3 gizmoPosition;

	[HideInInspector]
	public ProceduralFuselageTransform currentTransform;

	public GizmoMode currentGizmoMode;

	private void Start()
	{
		gizmo1.gameObject.SetActive(value: false);
		gizmo2.gameObject.SetActive(value: false);
		gizmo1.SetGizmoManager(this);
		gizmo2.SetGizmoManager(this);
	}

	public void UpdateGizmoAnimations()
	{
		gizmo1.UpdateAnimation();
		gizmo2.UpdateAnimation();
	}

	private void LateUpdate()
	{
		if (partPlacer.currentSelectedPart == null || (MouseInput.GetMouseButton(0) && !GizmoHover()))
		{
			currentGizmoMode = GizmoMode.Default;
		}
		if (partPlacer.currentMovingPart != null || partPlacer.GizmoActive())
		{
			gizmo1.gameObject.SetActive(value: false);
			gizmo2.gameObject.SetActive(value: false);
		}
	}

	public bool GizmoHover()
	{
		if (!gizmo1.IsHighlighted())
		{
			return gizmo2.IsHighlighted();
		}
		return true;
	}

	public void UpdateGizmos(BuildingPart selectedPart, Transform selectedSnapPoint, ProceduralFuselageTransform currentTransform)
	{
		gizmo1.SetActive(value: true);
		gizmo2.SetActive(value: true);
		gizmo1.UpdateMode(currentGizmoMode);
		gizmo2.UpdateMode(currentGizmoMode);
		Vector3 forward = selectedPart.snapPoints[0].forward;
		Vector3 baseOrigin = currentTransform.GetBaseOrigin1(selectedPart.transform);
		if (currentGizmoMode == GizmoMode.Default && (selectedSnapPoint != selectedPart.snapPoints[0] || !selectedPart.IsAttatched()))
		{
			gizmo1.SetActive(value: false);
		}
		if (currentGizmoMode == GizmoMode.Move && partPlacer.partContainer.SnappingPointToPart(baseOrigin, selectedPart) != null)
		{
			gizmo1.SetActive(value: false);
		}
		gizmo1.transform.position = baseOrigin + forward * 0.2f;
		gizmo1.transform.rotation = Quaternion.LookRotation(forward, selectedPart.snapPoints[0].up);
		gizmo1.targetScale = new Vector3(1f, Mathf.Sign(selectedPart.transform.localScale.y), 1f) * 0.6f;
		forward = selectedPart.snapPoints[1].forward;
		Vector3 baseOrigin2 = currentTransform.GetBaseOrigin2(selectedPart.transform);
		if (currentGizmoMode == GizmoMode.Default && (selectedSnapPoint != selectedPart.snapPoints[1] || !selectedPart.IsAttatched()))
		{
			gizmo2.SetActive(value: false);
		}
		if (currentGizmoMode == GizmoMode.Move && partPlacer.partContainer.SnappingPointToPart(baseOrigin2, selectedPart) != null)
		{
			gizmo2.SetActive(value: false);
		}
		gizmo2.transform.position = baseOrigin2 + forward * 0.2f;
		gizmo2.transform.rotation = Quaternion.LookRotation(forward, selectedPart.snapPoints[1].up);
		gizmo2.targetScale = new Vector3(-1f, Mathf.Sign(selectedPart.transform.localScale.y), 1f) * 0.6f;
		gizmo1.UpdateTransformDirection(selectedPart.transform.right, selectedPart.transform.up * Mathf.Sign(selectedPart.transform.localScale.y), currentTransform.side1);
		gizmo2.UpdateTransformDirection(selectedPart.transform.right, selectedPart.transform.up * Mathf.Sign(selectedPart.transform.localScale.y), currentTransform.side2);
		this.currentTransform.side1 = gizmo1.GetUpdatedFuselageSide(currentTransform.side1, currentTransform.side2);
		this.currentTransform.side2 = gizmo2.GetUpdatedFuselageSide(currentTransform.side2, currentTransform.side1);
	}

	public void SelectNewPart(ProceduralFuselage fuselage)
	{
		currentTransform = fuselage.AppliedTransform;
		gizmo1.gameObject.SetActive(value: true);
		gizmo2.gameObject.SetActive(value: true);
		gizmo1.SelectNewPart(fuselage.AppliedTransform.side1, fuselage.transform);
		gizmo2.SelectNewPart(fuselage.AppliedTransform.side2, fuselage.transform);
	}

	public void DeselectPart()
	{
		gizmo1.SetActive(value: false);
		gizmo2.SetActive(value: false);
	}

	public void ResetGizmos(ProceduralFuselage fuselage)
	{
		gizmo1.ResetGizmo(fuselage.AppliedTransform.side1);
		gizmo2.ResetGizmo(fuselage.AppliedTransform.side2);
		currentTransform = fuselage.AppliedTransform;
	}

	public void SetGizmoSelectionMode(int mode)
	{
		currentGizmoMode = (GizmoMode)mode;
	}
}
