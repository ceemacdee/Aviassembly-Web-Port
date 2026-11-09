using System;
using System.IO;
using UnityEngine;

public class PersistentStorage : Singleton<PersistentStorage>
{
	public SaveFileHeader currentHeader;

	public AutoSave autoSave;

	public const int SaveVersion = 30;

	private string savePath;

	public string SaveFilePath { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		SaveFilePath = Path.Combine(Application.persistentDataPath, "SaveGames");
		Directory.CreateDirectory(SaveFilePath);
		currentHeader = new SaveFileHeader();
	}

	public static void CreateSaveFolder()
	{
		Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "SaveGames"));
	}

	public void Save(string saveFileName)
	{
		savePath = Path.Combine(SaveFilePath, saveFileName + ".plane");
		using (BinaryWriter binaryWriter = new BinaryWriter(File.Open(savePath, FileMode.Create)))
		{
			GameDataWriter gameDataWriter = new GameDataWriter(binaryWriter);
			gameDataWriter.Write(30);
			currentHeader.Save(gameDataWriter);
			binaryWriter.Write((int)GameManager.gameMode);
			Singleton<GameManager>.Instance.Save(gameDataWriter);
			Singleton<PlaneContainer>.Instance.gameObject.GetComponent<PlaneStorage>().Save(gameDataWriter);
			Singleton<MoneyManager>.Instance.Save(gameDataWriter);
			Singleton<FogOfWar>.Instance.Save(gameDataWriter);
			Singleton<ResearchManager>.Instance.Save(gameDataWriter);
			Singleton<ScrapSpawner>.Instance.Save(gameDataWriter);
			Singleton<QuestGenerator>.Instance.Save(gameDataWriter);
			Singleton<AirportManager>.Instance.Save(gameDataWriter);
			Singleton<CargoInventory>.Instance.Save(gameDataWriter);
			if (GameManager.gameMode == GameMode.Flying)
			{
				gameDataWriter.Write(Singleton<PlaneContainer>.Instance.transform.position);
				gameDataWriter.Write(Singleton<PlaneContainer>.Instance.transform.rotation);
			}
			else
			{
				gameDataWriter.Write(GameManager.GetPlaneSpawnPoint());
				gameDataWriter.Write(Quaternion.identity);
			}
			autoSave.Save(gameDataWriter);
			Singleton<StoryState>.Instance.Save(gameDataWriter);
			Singleton<AchievementManager>.Instance.Save(gameDataWriter);
		}
#if UNITY_WEBGL
		try
		{
			File.SetCreationTime(savePath, DateTime.Now);
		}
		catch (Exception)
		{
		}
		WebGLFileSync.Flush();
#else
		File.SetCreationTime(savePath, DateTime.Now);
#endif
	}

	public void Load(string saveFileName)
	{
		savePath = Path.Combine(SaveFilePath, saveFileName + ".plane");
		using BinaryReader reader = new BinaryReader(File.Open(savePath, FileMode.Open));
		GameDataReader gameDataReader = new GameDataReader(reader);
		GetVersion(gameDataReader, savePath);
		currentHeader.Load(gameDataReader);
		if (gameDataReader.version > 6)
		{
			Singleton<GameManager>.Instance.savedGameMode = (GameMode)gameDataReader.ReadInt();
		}
		Singleton<GameManager>.Instance.Load(gameDataReader);
		Singleton<TerrainGenerationManager>.Instance.RefreshUseContinents();
		Singleton<PlaneContainer>.Instance.gameObject.GetComponent<PlaneStorage>().Load(gameDataReader);
		Singleton<MoneyManager>.Instance.Load(gameDataReader);
		Singleton<FogOfWar>.Instance.Load(gameDataReader);
		Singleton<ResearchManager>.Instance.Load(gameDataReader);
		Singleton<ScrapSpawner>.Instance.Load(gameDataReader);
		Singleton<AirportSpawner>.Instance.Load(gameDataReader);
		if (gameDataReader.version > 25)
		{
			Singleton<QuestGenerator>.Instance.Load(gameDataReader);
		}
		Singleton<AirportManager>.Instance.Load(gameDataReader);
		Singleton<CargoInventory>.Instance.Load(gameDataReader);
		Singleton<GameManager>.Instance.planePosistion = gameDataReader.ReadVector3();
		Singleton<GameManager>.Instance.planeRotatation = gameDataReader.ReadQuaternion();
		autoSave.Load(gameDataReader);
		Singleton<StoryState>.Instance.Load(gameDataReader);
		Singleton<AchievementManager>.Instance.Load(gameDataReader);
	}

	private static void GetVersion(GameDataReader reader, string savePath)
	{
		if (File.GetLastWriteTime(savePath).Date < new DateTime(2024, 9, 5))
		{
			reader.version = 0;
		}
		else
		{
			reader.version = reader.ReadInt();
		}
	}

	public static SaveFileHeader GetHeader(string saveFileName)
	{
		SaveFileHeader saveFileHeader = new SaveFileHeader();
		string path = Path.Combine(Path.Combine(Application.persistentDataPath, "SaveGames"), saveFileName + ".plane");
		using BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
		GameDataReader reader2 = new GameDataReader(reader);
		GetVersion(reader2, path);
		saveFileHeader.Load(reader2);
		return saveFileHeader;
	}
}
