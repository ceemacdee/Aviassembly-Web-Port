using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuDialog : Singleton<MenuDialog>
{
	public GameObject menuDialog;

	public GameObject quitDialog;

	public GameObject saveDialog;

	public GameObject settingsPanel;

	[Space(25f)]
	public Button saveButton;

	public Button saveAsButton;

	public GameObject saveButtonHider;

	public GameObject saveAsButtonHider;

	[Space(25f)]
	public TMP_InputField inputField;

	public RawImage dialogBackground;

	public TMP_Text saveButtonText;

	private GameManager gameManager;

	private float currentAlpha;

	private float alphaVelocity;

	private const float MaxAlpha = 0.92f;

	private void Start()
	{
		CloseMenuDialog();
		settingsPanel.gameObject.SetActive(value: false);
		gameManager = Singleton<GameManager>.Instance;
	}

	public void CloseMenuDialog()
	{
		menuDialog.SetActive(value: false);
		quitDialog.SetActive(value: false);
		saveDialog.SetActive(value: false);
		Time.timeScale = 1f;
	}

	public void SaveCurrentFile()
	{
		if (Singleton<GameManager>.Instance.currentSaveFile != "")
		{
			Singleton<PersistentStorage>.Instance.Save(Singleton<GameManager>.Instance.currentSaveFile);
		}
		else
		{
			EnableSaveDialog(enabled: true);
		}
	}

	public void EnableSaveDialog(bool enabled)
	{
		saveDialog.SetActive(enabled);
		menuDialog.SetActive(!enabled);
	}

	public void EnableSettingsPanel()
	{
		settingsPanel.gameObject.SetActive(value: true);
	}

	public void EnableQuitDialog(bool enabled)
	{
		quitDialog.SetActive(enabled);
		menuDialog.SetActive(!enabled);
	}

	public void Save()
	{
		string text = FileNameUtility.Sanitize(inputField.text);
		if (FileNameUtility.IsValidFilename(text))
		{
			Singleton<PersistentStorage>.Instance.Save(text);
			Singleton<GameManager>.Instance.currentSaveFile = text;
			Singleton<PersistentStorage>.Instance.autoSave.SetAutoSaveName();
		}
	}

	private void Update()
	{
		bool flag = menuDialog.activeInHierarchy || saveDialog.activeInHierarchy || quitDialog.activeInHierarchy;
		if ((Input.GetKeyDown(KeyCode.Escape) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)) && !gameManager.inMenu && !gameManager.modalsOpen)
		{
			if (flag || settingsPanel.activeInHierarchy)
			{
				CloseMenuDialog();
				settingsPanel.gameObject.SetActive(value: false);
				flag = false;
			}
			else
			{
				menuDialog.SetActive(value: true);
				Time.timeScale = 0f;
			}
		}
		if (Singleton<GameManager>.Instance.inMenu)
		{
			CloseMenuDialog();
		}
		float target = (flag ? 0.92f : 0f);
		currentAlpha = Mathf.SmoothDamp(currentAlpha, target, ref alphaVelocity, 0.2f, float.MaxValue, Time.unscaledDeltaTime);
		dialogBackground.color = new Color(0f, 0f, 0f, currentAlpha);
		dialogBackground.gameObject.SetActive(currentAlpha > 0.01f);
		if (!gameManager.inMenu && Singleton<PlaneContainer>.Instance != null)
		{
			bool flag2 = Singleton<PlaneContainer>.Instance.GetVelocityMagintude() < 10f && !Singleton<PlaneContainer>.Instance.GetComponent<PlaneController>().Exploded;
			saveButton.interactable = flag2;
			saveAsButton.interactable = flag2;
			saveButtonHider.SetActive(!flag2);
			saveAsButtonHider.SetActive(!flag2);
			saveButton.GetComponent<TooltipTrigger>().enabled = !flag2;
			saveAsButton.GetComponent<TooltipTrigger>().enabled = !flag2;
		}
	}
}
