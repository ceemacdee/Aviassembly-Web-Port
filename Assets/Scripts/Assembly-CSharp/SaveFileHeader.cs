using UnityEngine;

public class SaveFileHeader
{
	public float playtime;

	public float timeAtGameStart;

	public int version;

	public void Save(GameDataWriter writer)
	{
		writer.Write(playtime + (Time.unscaledTime - timeAtGameStart));
	}

	public void Load(GameDataReader reader)
	{
		version = reader.version;
		playtime = reader.ReadFloat();
	}
}
