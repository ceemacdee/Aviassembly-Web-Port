using UnityEngine;

public class MoneyManager : Singleton<MoneyManager>
{
	public float money;

	private void Start()
	{
		if (Singleton<GameManager>.Instance.gameModeData.creativeMode)
		{
			money += 2000000f;
		}
	}

	public void ChangeMoneyAmount(float change)
	{
		if (Singleton<GameManager>.Instance.gameModeData.creativeMode)
		{
			change = 0f;
		}
		money += change;
	}

	public bool HasEnoughMoney(float requiredAmount)
	{
		return (float)Mathf.RoundToInt(money) >= requiredAmount;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(money);
		writer.Write(Singleton<GameManager>.Instance.gameModeData.creativeMode);
	}

	public void Load(GameDataReader reader)
	{
		money = reader.ReadFloat();
		Singleton<GameManager>.Instance.gameModeData.creativeMode = reader.ReadBool();
	}
}
