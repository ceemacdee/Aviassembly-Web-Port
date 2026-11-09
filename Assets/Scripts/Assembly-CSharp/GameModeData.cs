using UnityEngine;

public class GameModeData : MonoBehaviour
{
	public bool disableAirports;

	public bool infiniteFuel;

	public bool creativeMode;

	public bool unlockAll;

	public MapType mapType;

	public Difficulty currentDifficulty;

	[Header("Map Settings")]
	[Space(10f)]
	public MapSettings forest;

	public MapSettings snow;

	public MapSettings desert;

	public MapSettings flat;

	public MapSettings hills;

	public MapSettings ocean;

	public void Save(GameDataWriter writer)
	{
		writer.Write(disableAirports);
		writer.Write(infiniteFuel);
		writer.Write(creativeMode);
		writer.Write(unlockAll);
		writer.Write((int)mapType);
		writer.Write((int)currentDifficulty);
	}

	public void Load(GameDataReader reader)
	{
		disableAirports = reader.ReadBool();
		infiniteFuel = reader.ReadBool();
		creativeMode = reader.ReadBool();
		unlockAll = reader.ReadBool();
		mapType = (MapType)reader.ReadInt();
		if (reader.version > 17)
		{
			currentDifficulty = (Difficulty)reader.ReadInt();
		}
		else
		{
			currentDifficulty = Difficulty.Normal;
		}
	}
}
