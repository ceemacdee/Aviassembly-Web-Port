public class Contract
{
	public CargoType cargoType { get; private set; }

	public int amount { get; private set; }

	public float reward { get; private set; }

	public bool completed { get; set; }

	public bool CanComplete()
	{
		return Singleton<CargoInventory>.Instance.GetCargoCount(cargoType) >= amount;
	}

	public bool CompleteContract()
	{
		if (!CanComplete())
		{
			return false;
		}
		Singleton<CargoInventory>.Instance.RemoveCargo(cargoType, amount);
		Singleton<MoneyManager>.Instance.ChangeMoneyAmount(reward);
		completed = true;
		return true;
	}

	public string GetContractText(bool red)
	{
		string text = ((cargoType.cargoName == "" || cargoType.cargoName == null) ? cargoType.name : cargoType.cargoName);
		if (red)
		{
			return "Sell <color=#CF0000>" + amount + "<color=#000000> " + text;
		}
		return "Sell " + amount + " " + cargoType.name;
	}

	public Contract(CargoType cargoType, int amount, float reward)
	{
		this.cargoType = cargoType;
		this.amount = amount;
		this.reward = reward;
	}
}
