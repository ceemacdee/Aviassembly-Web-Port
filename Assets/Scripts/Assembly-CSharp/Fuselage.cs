public class Fuselage : PlanePart
{
	public float cargoVolume;

	public override void UpdatePart(PlaneContainer container)
	{
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = new PartStat[1];
		array[0].statName = "Cargo";
		array[0].SetValue(cargoVolume);
		return array;
	}
}
