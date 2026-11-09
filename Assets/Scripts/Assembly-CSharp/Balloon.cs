using UnityEngine;

public class Balloon : PlanePart
{
	public float lift;

	public override void UpdatePart(PlaneContainer container)
	{
		Vector3 position = Vector3.Lerp(container.transform.TransformPoint(rb.centerOfMass), base.transform.position, 0f);
		rb.AddForceAtPosition(lift * Vector3.up, position);
		container.currentLift += Vector3.Dot(container.transform.up, lift * Vector3.up);
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = new PartStat[1];
		array[0].statName = "Lift";
		array[0].SetValue(lift);
		return array;
	}
}
