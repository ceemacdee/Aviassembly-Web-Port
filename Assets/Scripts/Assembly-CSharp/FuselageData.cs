public struct FuselageData
{
	public float cargoVolume;

	public float mass;

	public float price;

	public float volume;

	public FuselageData(float cargoVolume, float mass, float price, float volume)
	{
		this.cargoVolume = cargoVolume;
		this.mass = mass;
		this.price = price;
		this.volume = volume;
	}

	public FuselageData ChangeVolume(float newVolume)
	{
		float num = cargoVolume / volume * newVolume;
		float num2 = mass / volume * newVolume;
		float num3 = price / volume * newVolume;
		return new FuselageData(num, num2, num3, newVolume);
	}
}
