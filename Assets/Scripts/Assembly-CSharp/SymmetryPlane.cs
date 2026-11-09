using UnityEngine;

public class SymmetryPlane
{
	public PartPlacer placer;

	private Vector3 planeNormal;

	private Vector3 planePosition;

	private Plane plane;

	public SymmetryPlane(PartPlacer placer)
	{
		this.placer = placer;
	}

	public void UpdatePlanePosition(PlaneContainer container)
	{
		BuildingPart basePart = placer.partContainer.GetBasePart();
		planePosition = ((basePart != null) ? CustomMath.GetObjectCenter(basePart.gameObject) : Vector3.zero);
		planeNormal = ((basePart != null) ? basePart.transform.right : Vector3.forward);
		plane = new Plane(planeNormal, planePosition);
	}

	public float DistanceFromPlane(Vector3 position)
	{
		return plane.GetDistanceToPoint(position);
	}

	public Vector3 GetMirroredPosition(Vector3 position)
	{
		Vector3 vector = plane.ClosestPointOnPlane(position);
		float distanceToPoint = plane.GetDistanceToPoint(position);
		return vector + planeNormal * (0f - distanceToPoint);
	}

	public Quaternion GetMirroredRotation(Transform part)
	{
		Vector3 position = part.transform.position + part.transform.forward * 0.01f;
		Vector3 position2 = part.transform.position + part.transform.up * 0.01f;
		Vector3 mirroredPosition = GetMirroredPosition(position);
		Vector3 mirroredPosition2 = GetMirroredPosition(position2);
		Vector3 mirroredPosition3 = GetMirroredPosition(part.transform.position);
		Vector3 normalized = (mirroredPosition - mirroredPosition3).normalized;
		Vector3 normalized2 = (mirroredPosition2 - mirroredPosition3).normalized;
		return Quaternion.LookRotation(normalized, -normalized2);
	}

	public Vector3 GetMirroredScale(Vector3 scale)
	{
		scale.y = 0f - scale.y;
		return scale;
	}
}
