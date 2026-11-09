public class FuelTank : PlanePart
{
	public float volume;

	public override void UpdatePart(PlaneContainer container)
	{
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = new PartStat[2];
		array[0].statName = "Fuel";
		array[0].SetValue(volume);
		array[1].statName = "Fuel Weight";
		array[1].SetValue(volume * Singleton<PlaneContainer>.Instance.fuelWeight);
		return array;
	}
}
