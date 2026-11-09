using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WorldAsset : ScriptableObject
{
	public int versionStartingFrom;

	public List<ContinentType> continents = new List<ContinentType>();

	public List<AirportData> airports = new List<AirportData>();

	public List<CargoType> cargoTypes = new List<CargoType>();
}
