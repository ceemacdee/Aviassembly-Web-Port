using TMPro;
using UnityEngine;

public class PhotoModeManager : MonoBehaviour
{
	public GameObject photoModeSettingsWindow;

	public CameraController cameraController;

	public Canvas regularCanvas;

	public Canvas highlighterCanvas;

	public Canvas photomodeCanvas;

	public TMP_Text pauseText;

	private bool photoModeEnabled;

	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.F11))
		{
			if (!photoModeEnabled)
			{
				photoModeSettingsWindow.SetActive(value: true);
			}
			photoModeEnabled = !photoModeEnabled;
		}
		if (Input.GetKeyDown(KeyCode.F12))
		{
			photoModeEnabled = !photoModeEnabled;
			photoModeSettingsWindow.SetActive(photoModeEnabled);
		}
		regularCanvas.enabled = !photoModeEnabled;
		highlighterCanvas.enabled = !photoModeEnabled;
		photomodeCanvas.enabled = photoModeEnabled;
		if (Time.timeScale > 0f)
		{
			pauseText.text = "Pause";
		}
		else
		{
			pauseText.text = "Unpause";
		}
	}

	public void PauseGame()
	{
		if (Time.timeScale > 0f)
		{
			Time.timeScale = 0f;
		}
		else
		{
			Time.timeScale = 1f;
		}
	}

	private void OnDestroy()
	{
		Time.timeScale = 1f;
	}

	public void SetCameraMode(int mode)
	{
		if (mode == 2)
		{
			Time.timeScale = 0f;
		}
		cameraController.SetCameraMode(mode);
	}
}
