using UnityEngine;

[CreateAssetMenu]
public class CargoType : ScriptableObject
{
	[Header("Visual")]
	public string cargoName;

	public Texture2D icon;

	[Header("Cargo Stats")]
	public float basePrice;

	public float weight;

	public int cargoSpace;

	public bool fragile;

	[Space(15f)]
	public bool expires;

	public float expirationTime;

	public string GetCargoName()
	{
		if (!string.IsNullOrEmpty(cargoName))
		{
			return cargoName;
		}
		return base.name;
	}
}
