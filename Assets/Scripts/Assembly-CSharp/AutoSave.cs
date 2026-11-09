using System;
using System.IO;
using UnityEngine;

public class AutoSave : MonoBehaviour
{
	public float intervalInMinutes;

	private float currentTime;

	private string autoSaveFileName;

	private void Start()
	{
		if (string.IsNullOrWhiteSpace(autoSaveFileName))
		{
			SetAutoSaveName();
		}
	}

	private void Update()
	{
		currentTime += Time.deltaTime;
		if (currentTime > intervalInMinutes * 60f && Singleton<PlaneContainer>.Instance.IsAtAirport() && Singleton<GameManager>.Instance.autoSave)
		{
			currentTime = 0f;
			CreateSave();
		}
	}

	public void CreateSave()
	{
		Singleton<PersistentStorage>.Instance.Save(autoSaveFileName);
	}

	public void SetAutoSaveName()
	{
		string text = AutoSaveFileCount().ToString();
		if (!string.IsNullOrWhiteSpace(Singleton<GameManager>.Instance.currentSaveFile))
		{
			text = "- " + Singleton<GameManager>.Instance.currentSaveFile;
		}
		autoSaveFileName = "AutoSave " + text;
	}

	public int AutoSaveFileCount()
	{
		int num = 0;
		string[] files = Directory.GetFiles(Path.Combine(Application.persistentDataPath, "SaveGames"));
		for (int i = 0; i < files.Length; i++)
		{
			if (Path.GetFileName(files[i]).Contains("AutoSave", StringComparison.OrdinalIgnoreCase))
			{
				num++;
			}
		}
		return num;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(autoSaveFileName);
	}

	public void Load(GameDataReader reader)
	{
		if (reader.version > 5)
		{
			autoSaveFileName = reader.ReadString();
		}
	}
}
