using System;

public struct PartStat
{
	public string statName;

	public string statValue;

	public void SetValue(float value)
	{
		statValue = Math.Round(value, 2).ToString();
	}

	public void SetValue(string value)
	{
		statValue = value;
	}
}
