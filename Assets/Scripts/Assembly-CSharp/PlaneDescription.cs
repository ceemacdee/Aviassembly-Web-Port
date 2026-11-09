using System.Collections.Generic;

public struct PlaneDescription
{
	public float value;

	public float mass;

	public float linearDamping;

	public Engine[] engines;

	public Wing[] wings;

	public FuelTank[] fuelTanks;

	public PlaneDescription(float value, float mass, float linearDamping, Engine[] engines, Wing[] wings, FuelTank[] fuelTanks)
	{
		this.value = value;
		this.mass = mass;
		this.linearDamping = linearDamping;
		this.engines = engines;
		this.wings = wings;
		this.fuelTanks = fuelTanks;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(value);
		writer.Write(mass);
		writer.Write(linearDamping);
		writer.Write(engines.Length);
		for (int i = 0; i < engines.Length; i++)
		{
			writer.Write(engines[i].name);
		}
		writer.Write(wings.Length);
		for (int j = 0; j < wings.Length; j++)
		{
			writer.Write(wings[j].name);
		}
		writer.Write(fuelTanks.Length);
		for (int k = 0; k < fuelTanks.Length; k++)
		{
			writer.Write(fuelTanks[k].name);
		}
	}

	public static PlaneDescription Load(GameDataReader reader)
	{
		PlaneDescription result = default;
		List<Engine> list = new List<Engine>();
		List<Wing> list2 = new List<Wing>();
		List<FuelTank> list3 = new List<FuelTank>();
		result.value = reader.ReadFloat();
		result.mass = reader.ReadFloat();
		result.linearDamping = reader.ReadFloat();
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			list.Add(PartPrefabs.GetPartPrefab(reader.ReadString()).GetComponent<Engine>());
		}
		int num2 = reader.ReadInt();
		for (int j = 0; j < num2; j++)
		{
			list2.Add(PartPrefabs.GetPartPrefab(reader.ReadString()).GetComponent<Wing>());
		}
		int num3 = reader.ReadInt();
		for (int k = 0; k < num3; k++)
		{
			list3.Add(PartPrefabs.GetPartPrefab(reader.ReadString()).GetComponent<FuelTank>());
		}
		result.engines = list.ToArray();
		result.wings = list2.ToArray();
		result.fuelTanks = list3.ToArray();
		return result;
	}
}
