using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CargoInventory : Singleton<CargoInventory>
{
	public AudioDef glassBreaking;

	public AudioDef addItem;

	public float breakageCooldown;

	private Dictionary<CargoType, int> currentCargo = new Dictionary<CargoType, int>();

	private PlaneContainer planeContainer;

	private float expirationTimer;

	private float currentExpirationDuration;

	private Vector3 prevVelocity;

	private float lastBreakTime;

	public float minFragileBreakTreshhold;

	public float maxFragileBreakTreshhold;

	public float MaxVolume { get; private set; }

	public float CurrentVolume { get; private set; }

	public event Action<CargoType> ExpirableCargoCleared;

	public event Action<CargoType, int> FragileCargoCleared;

	private void Start()
	{
		planeContainer = Singleton<PlaneContainer>.Instance;
	}

	private void Update()
	{
		MaxVolume = planeContainer.GetCargoVolume();
		expirationTimer -= Time.deltaTime;
		if (ContainsExpirableCargo() && expirationTimer <= 0f)
		{
			ClearExpirableCargo();
		}
		expirationTimer = Mathf.Max(expirationTimer, 0f);
	}

	private void FixedUpdate()
	{
		float value = Vector3.Distance(planeContainer.GetVelocity(), prevVelocity);
		float breakPercentage = Mathf.Clamp01(Mathf.InverseLerp(minFragileBreakTreshhold, maxFragileBreakTreshhold, value));
		ClearFragileCargo(breakPercentage);
		prevVelocity = planeContainer.GetVelocity();
	}

	public void RecalculateCargo()
	{
		float num = planeContainer.GetCargoVolume() - CurrentVolume;
		for (int num2 = currentCargo.Count - 1; num2 >= 0; num2--)
		{
			KeyValuePair<CargoType, int> keyValuePair = currentCargo.ElementAt(num2);
			CargoType key = keyValuePair.Key;
			int value = keyValuePair.Value;
			if (num >= 0f)
			{
				break;
			}
			int b = Mathf.CeilToInt(Mathf.Abs(num) / (float)key.cargoSpace);
			b = Mathf.Min(value, b);
			num += (float)(b * key.cargoSpace);
			RemoveCargo(key, b);
		}
	}

	public void ApplyCargo()
	{
		for (int i = 0; i < currentCargo.Count; i++)
		{
			KeyValuePair<CargoType, int> keyValuePair = currentCargo.ElementAt(i);
			CargoType key = keyValuePair.Key;
			int value = keyValuePair.Value;
			planeContainer.ChangeMass(key.weight * (float)value);
		}
	}

	public bool EnoughSpace(CargoType cargoType)
	{
		if (CurrentVolume + (float)cargoType.cargoSpace > (float)Mathf.RoundToInt(MaxVolume))
		{
			return false;
		}
		return true;
	}

	public void AddCargo(CargoType cargoType)
	{
		if (Singleton<AirportManager>.Instance.GetClosestAirport(planeContainer.transform.position).CargoUnlocked() && !(CurrentVolume + (float)cargoType.cargoSpace > (float)Mathf.RoundToInt(MaxVolume)))
		{
			if (!currentCargo.ContainsKey(cargoType))
			{
				currentCargo.Add(cargoType, 0);
			}
			currentCargo[cargoType]++;
			CurrentVolume += cargoType.cargoSpace;
			planeContainer.ChangeMass(cargoType.weight);
			if (cargoType.expires && Singleton<GameManager>.Instance.gameModeData.currentDifficulty != Difficulty.Relaxed)
			{
				expirationTimer = cargoType.expirationTime;
				currentExpirationDuration = cargoType.expirationTime;
			}
			Singleton<AudioManager>.Instance.PlaySound(addItem);
		}
	}

	public void RemoveCargo(CargoType cargoType, int amount)
	{
		if (currentCargo.ContainsKey(cargoType) && currentCargo[cargoType] >= amount)
		{
			currentCargo[cargoType] -= amount;
			CurrentVolume -= cargoType.cargoSpace * amount;
			planeContainer.ChangeMass(0f - cargoType.weight * (float)amount);
		}
	}

	public int GetCargoCount(CargoType cargoType)
	{
		if (cargoType == null)
		{
			return 0;
		}
		if (!currentCargo.ContainsKey(cargoType))
		{
			return 0;
		}
		return currentCargo[cargoType];
	}

	public float GetExpirationProgress()
	{
		if (currentExpirationDuration == 0f)
		{
			return 0f;
		}
		return expirationTimer / currentExpirationDuration;
	}

	public Texture GetExpirationIcon()
	{
		foreach (KeyValuePair<CargoType, int> item in currentCargo)
		{
			if (item.Key.expires && item.Value > 0)
			{
				return item.Key.icon;
			}
		}
		return null;
	}

	public bool ContainsExpirableCargo()
	{
		foreach (KeyValuePair<CargoType, int> item in currentCargo)
		{
			if (item.Key.expires && item.Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool ContainsFragileCargo()
	{
		foreach (KeyValuePair<CargoType, int> item in currentCargo)
		{
			if (item.Key.fragile && item.Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	public float GetCargoMass()
	{
		float num = 0f;
		foreach (KeyValuePair<CargoType, int> item in currentCargo)
		{
			num += item.Key.weight * (float)item.Value;
		}
		return num;
	}

	public void ClearInventory()
	{
		for (int num = currentCargo.Count - 1; num >= 0; num--)
		{
			KeyValuePair<CargoType, int> keyValuePair = currentCargo.ElementAt(num);
			CargoType key = keyValuePair.Key;
			int value = keyValuePair.Value;
			RemoveCargo(key, value);
			currentCargo.Remove(key);
		}
		CurrentVolume = 0f;
		currentCargo.Clear();
	}

	public void ClearExpirableCargo()
	{
		if (Singleton<GameManager>.Instance.gameModeData.currentDifficulty == Difficulty.Relaxed)
		{
			return;
		}
		CargoType cargoType = null;
		for (int num = currentCargo.Count - 1; num >= 0; num--)
		{
			KeyValuePair<CargoType, int> keyValuePair = currentCargo.ElementAt(num);
			CargoType key = keyValuePair.Key;
			int value = keyValuePair.Value;
			if (key.expires)
			{
				if (value > 0)
				{
					cargoType = key;
				}
				RemoveCargo(key, value);
				currentCargo.Remove(key);
			}
		}
		if (cargoType != null && ExpirableCargoCleared != null)
		{
			ExpirableCargoCleared(cargoType);
		}
	}

	public void ClearFragileCargo(float breakPercentage)
	{
		if (Singleton<GameManager>.Instance.gameModeData.currentDifficulty == Difficulty.Relaxed)
		{
			return;
		}
		if (Time.time - lastBreakTime < breakageCooldown)
		{
			breakPercentage = 0f;
		}
		CargoType cargoType = null;
		int num = 0;
		for (int num2 = currentCargo.Count - 1; num2 >= 0; num2--)
		{
			KeyValuePair<CargoType, int> keyValuePair = currentCargo.ElementAt(num2);
			if (keyValuePair.Key.fragile && keyValuePair.Value > 0)
			{
				num += keyValuePair.Value;
			}
		}
		int num3 = Mathf.RoundToInt(breakPercentage * (float)num);
		int b = num3;
		if (num3 > 0)
		{
			lastBreakTime = Time.time;
		}
		for (int num4 = currentCargo.Count - 1; num4 >= 0; num4--)
		{
			KeyValuePair<CargoType, int> keyValuePair2 = currentCargo.ElementAt(num4);
			CargoType key = keyValuePair2.Key;
			int value = keyValuePair2.Value;
			if (key.fragile && value > 0)
			{
				int num5 = Mathf.Min(value, b);
				RemoveCargo(key, num5);
				if (num5 == value)
				{
					currentCargo.Remove(key);
				}
				cargoType = key;
			}
		}
		if (cargoType != null && num3 > 0)
		{
			Singleton<AudioManager>.Instance.PlaySound(glassBreaking);
			FragileCargoCleared(cargoType, num3);
		}
	}

	public void ClearAll()
	{
		for (int num = currentCargo.Count - 1; num >= 0; num--)
		{
			KeyValuePair<CargoType, int> keyValuePair = currentCargo.ElementAt(num);
			CargoType key = keyValuePair.Key;
			int value = keyValuePair.Value;
			RemoveCargo(key, value);
			currentCargo.Remove(key);
		}
	}

	public Dictionary<CargoType, int> GetCurrentCargo()
	{
		return new Dictionary<CargoType, int>(currentCargo);
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(currentCargo.Count);
		writer.Write(CurrentVolume);
		for (int i = 0; i < currentCargo.Count; i++)
		{
			KeyValuePair<CargoType, int> keyValuePair = currentCargo.ElementAt(i);
			CargoType key = keyValuePair.Key;
			int value = keyValuePair.Value;
			writer.Write(Singleton<AirportManager>.Instance.CargoTypeToIndex(key));
			writer.Write(value);
		}
	}

	public void Load(GameDataReader reader)
	{
		int num = reader.ReadInt();
		CurrentVolume = reader.ReadFloat();
		for (int i = 0; i < num; i++)
		{
			int num2 = reader.ReadInt();
			int value = reader.ReadInt();
			if (num2 != -1)
			{
				CargoType key = Singleton<AirportManager>.Instance.allCargoTypes[num2];
				currentCargo.Add(key, value);
			}
		}
	}
}
