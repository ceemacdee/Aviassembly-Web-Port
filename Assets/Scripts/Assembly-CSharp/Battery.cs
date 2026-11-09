public class Battery : PlanePart
{
	public float capacity;

	public override void UpdatePart(PlaneContainer container)
	{
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = new PartStat[1];
		array[0].statName = "Capacity";
		array[0].SetValue(capacity);
		return array;
	}
}
