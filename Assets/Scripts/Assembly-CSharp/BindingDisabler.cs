using UnityEngine;

public class BindingDisabler : MonoBehaviour
{
	public GameObject[] virtualCursorSettings;

	public GameObject[] gamepadSettings;

	public GameObject[] joystickSettings;

	private void Update()
	{
		for (int i = 0; i < virtualCursorSettings.Length; i++)
		{
			virtualCursorSettings[i].SetActive(InputManager.HasGamepad || InputManager.HasJoystick);
		}
		for (int j = 0; j < gamepadSettings.Length; j++)
		{
			gamepadSettings[j].SetActive(InputManager.HasGamepad);
		}
		for (int k = 0; k < joystickSettings.Length; k++)
		{
			joystickSettings[k].SetActive(InputManager.HasJoystick);
		}
	}
}
