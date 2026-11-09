using UnityEngine;

public class SolarPanel : PlanePart
{
	public float rechargeSpeed;

	public override void UpdatePart(PlaneContainer container)
	{
	}

	private void Update()
	{
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		instance.electricity = Mathf.Clamp(instance.electricity + Time.deltaTime * rechargeSpeed, 0f, instance.electricityStorageCapacity);
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = new PartStat[1];
		array[0].statName = "Recharge speed";
		array[0].SetValue(rechargeSpeed * 100f);
		return array;
	}
}
