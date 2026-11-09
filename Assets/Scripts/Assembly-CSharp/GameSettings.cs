using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameSettings
{
	public float masterVolume;

	public float musicVolume;

	public float clockVolume;

	public bool fullscreen;

	public Vector2Int resolution;

	public bool autoSteer;

	public bool autoSave;

	public bool vSync = true;

	public bool shadows = true;

	public float drawDistance = 3500f;

	public bool ssao = true;

	public bool antiAliasing = true;

	public int raceProgress;

	public void SaveSettings()
	{
		if (!File.Exists(Application.persistentDataPath + "/gameSettings.json"))
		{
			using (File.CreateText(Application.persistentDataPath + "/gameSettings.json"))
			{
			}
		}
		File.WriteAllText(Application.persistentDataPath + "/gameSettings.json", JsonUtility.ToJson(this));
		WebGLFileSync.Flush();
	}

	public void LoadSettings()
	{
		try
		{
			if (!File.Exists(Application.persistentDataPath + "/gameSettings.json"))
			{
				FallbackToDefault();
				return;
			}
			string text = File.ReadAllText(Application.persistentDataPath + "/gameSettings.json");
			bool flag = text.Contains("autoSave");
			GameSettings gameSettings = JsonUtility.FromJson<GameSettings>(text);
			if (!flag)
			{
				gameSettings.autoSave = true;
			}
			if (!text.Contains("clockVolume"))
			{
				gameSettings.clockVolume = 1f;
			}
			if (gameSettings == null)
			{
				FallbackToDefault();
				return;
			}
			masterVolume = gameSettings.masterVolume;
			musicVolume = gameSettings.musicVolume;
			clockVolume = gameSettings.clockVolume;
			fullscreen = gameSettings.fullscreen;
			resolution = gameSettings.resolution;
			autoSave = gameSettings.autoSave;
			antiAliasing = gameSettings.antiAliasing;
			raceProgress = gameSettings.raceProgress;
			ssao = gameSettings.ssao;
			vSync = gameSettings.vSync;
			shadows = gameSettings.shadows;
			drawDistance = gameSettings.drawDistance;
			autoSteer = gameSettings.autoSteer;
			if (!text.Contains("autoSteer"))
			{
				autoSteer = true;
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message);
			FallbackToDefault();
		}
	}

	private void FallbackToDefault()
	{
		masterVolume = 0.5f;
		musicVolume = 1f;
		clockVolume = 1f;
		autoSteer = true;
		fullscreen = true;
		autoSave = true;
		ssao = true;
		vSync = true;
		shadows = true;
		drawDistance = 3500f;
		antiAliasing = true;
		raceProgress = 0;
		Resolution[] resolutions = Screen.resolutions;
		// WebGL port: browsers may report no fullscreen resolutions; fall back to the canvas size.
		resolution = (resolutions.Length > 0) ? new Vector2Int(resolutions[^1].width, resolutions[^1].height) : new Vector2Int(Screen.width, Screen.height);
	}
}
