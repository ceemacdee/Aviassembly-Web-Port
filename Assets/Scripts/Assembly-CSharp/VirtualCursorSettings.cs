using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class VirtualCursorSettings
{
	public string moveCursorOverride;

	public string zoomOverride;

	public string leftButtonOverride;

	public string rightButtonOverride;

	public string slowCursorOverride;

	public float cursorSpeed;

	public float slowdownSpeed;

	public void GetCurrentSettings(MouseInput input)
	{
		moveCursorOverride = input.moveCursorAction.action.SaveBindingOverridesAsJson();
		zoomOverride = input.zoomAction.action.SaveBindingOverridesAsJson();
		leftButtonOverride = input.leftButtonAction.action.SaveBindingOverridesAsJson();
		rightButtonOverride = input.rightButtonAction.action.SaveBindingOverridesAsJson();
		slowCursorOverride = input.slowCursorAction.action.SaveBindingOverridesAsJson();
		cursorSpeed = input.cursorSpeed;
		slowdownSpeed = input.mouseSlowdown;
	}

	public void ApplySettings(MouseInput input)
	{
		input.moveCursorAction.action.LoadBindingOverridesFromJson(moveCursorOverride);
		input.zoomAction.action.LoadBindingOverridesFromJson(zoomOverride);
		input.leftButtonAction.action.LoadBindingOverridesFromJson(leftButtonOverride);
		input.rightButtonAction.action.LoadBindingOverridesFromJson(rightButtonOverride);
		input.slowCursorAction.action.LoadBindingOverridesFromJson(slowCursorOverride);
		input.cursorSpeed = cursorSpeed;
		input.mouseSlowdown = slowdownSpeed;
	}

	public void SaveKeybinds()
	{
		if (!File.Exists(Application.persistentDataPath + "/virtualCursorSettings.json"))
		{
			using (File.CreateText(Application.persistentDataPath + "/virtualCursorSettings.json"))
			{
			}
		}
		string contents = JsonUtility.ToJson(this);
		File.WriteAllText(Application.persistentDataPath + "/virtualCursorSettings.json", contents);
		WebGLFileSync.Flush();
	}

	public void LoadKeybinds()
	{
		if (!File.Exists(Application.persistentDataPath + "/virtualCursorSettings.json"))
		{
			cursorSpeed = 1000f;
			slowdownSpeed = 0.5f;
			return;
		}
		VirtualCursorSettings virtualCursorSettings = JsonUtility.FromJson<VirtualCursorSettings>(File.ReadAllText(Application.persistentDataPath + "/virtualCursorSettings.json"));
		moveCursorOverride = virtualCursorSettings.moveCursorOverride;
		zoomOverride = virtualCursorSettings.zoomOverride;
		leftButtonOverride = virtualCursorSettings.leftButtonOverride;
		rightButtonOverride = virtualCursorSettings.rightButtonOverride;
		slowCursorOverride = virtualCursorSettings.slowCursorOverride;
		cursorSpeed = virtualCursorSettings.cursorSpeed;
		slowdownSpeed = virtualCursorSettings.slowdownSpeed;
	}
}
