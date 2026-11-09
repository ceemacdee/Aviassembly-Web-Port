using UnityEngine;

public class AirTurbine : PlanePart
{
	public float capacity;

	public Transform rotor;

	public override void UpdatePart(PlaneContainer container)
	{
		container.electricity = Mathf.Clamp(container.electricity + container.GetVelocityMagintude() / 200f * Time.fixedDeltaTime * capacity, 0f, container.electricityStorageCapacity);
	}

	private void Update()
	{
		if (rotor != null)
		{
			rotor.rotation *= Quaternion.Euler(Singleton<PlaneContainer>.Instance.GetVelocityMagintude() * Time.deltaTime * 10f, 0f, 0f);
		}
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = new PartStat[1];
		array[0].statName = "Electricity Generation";
		array[0].SetValue(capacity * 100f);
		return array;
	}
}
