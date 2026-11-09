using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class LoadPlanePanel : MonoBehaviour
{
	public TMP_Text removeText;

	public TMP_Text removeConfirmationText;

	public LoadPanelOpenButton loadPanelOpenButton;

	public PlaneLoadLocalToggleButton localButton;

	public PlaneLoadLocalToggleButton workshopButton;

	public float fadeSpeed;

	public GameObject panel;

	public Transform buttonParent;

	public GameObject buttonPrefab;

	public GameObject confirmationPanel;

	[HideInInspector]
	public string selectedFile;

	private CanvasGroup canvasGroup;

	private int alphaDirection;

	public bool local;

	private List<LoadPlaneButton> loadButtons = new List<LoadPlaneButton>();

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		canvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: false);
		confirmationPanel.gameObject.SetActive(value: false);
		local = true;
		SteamworksManager instance = Singleton<SteamworksManager>.Instance;
		instance.PlanesUpdated = (Action)Delegate.Combine(instance.PlanesUpdated, new Action(RefreshButtons));
	}

	private void Start()
	{
		localButton.SetSelected(local);
		workshopButton.SetSelected(!local);
	}

	public void SwitchLocal(bool local)
	{
		this.local = local;
		RefreshButtons();
		localButton.SetSelected(local);
		workshopButton.SetSelected(!local);
	}

	private void RefreshButtonsEnabled()
	{
		List<string> planeDirectories = Singleton<SteamworksManager>.Instance.GetPlaneDirectories();
		int num = Directory.GetFiles(Path.Combine(Application.persistentDataPath, "Plane Designs"), "*.planedesign", SearchOption.AllDirectories).Length;
		int num2 = 0;
		for (int i = 0; i < planeDirectories.Count; i++)
		{
			num2 += Directory.GetFiles(planeDirectories[i], "*.planedesign", SearchOption.AllDirectories).Length;
		}
		localButton.SetButtonEnabled(num > 0);
		workshopButton.SetButtonEnabled(num2 > 0);
		if (num2 == 0)
		{
			workshopButton.SetSelected(selected: false);
			localButton.SetSelected(selected: true);
			local = true;
		}
		if (num == 0)
		{
			localButton.SetSelected(selected: false);
		}
		if (num2 == 0 && num == 0)
		{
			Close();
		}
	}

	public void Open()
	{
		alphaDirection = 1;
		base.gameObject.SetActive(value: true);
		RefreshButtons();
	}

	public void Close()
	{
		alphaDirection = -1;
	}

	public void Update()
	{
		canvasGroup.alpha += fadeSpeed * (float)alphaDirection * Time.deltaTime;
		panel.gameObject.SetActive(canvasGroup.alpha > 0.75f);
		if (alphaDirection < 0 && canvasGroup.alpha < 0.05f)
		{
			base.gameObject.SetActive(value: false);
		}
		if ((!Singleton<MouseInput>.Instance.PointerIsOverUI && MouseInput.GetMouseButtonDown(0)) || MouseInput.GetMouseButton(1))
		{
			Close();
		}
	}

	public void SelectFile(string filename, bool isWorkshopFile)
	{
		selectedFile = filename;
		removeText.text = (isWorkshopFile ? "Unsubscribe" : "Remove");
		removeConfirmationText.text = (isWorkshopFile ? "Are you sure you want to unsubscribe?" : "Are you sure you want to delete this file?");
	}

	public void LoadFile()
	{
		if (!string.IsNullOrEmpty(selectedFile) && File.Exists(selectedFile))
		{
			Singleton<PlaneStorage>.Instance.LoadFromFile(selectedFile);
			Close();
		}
	}

	private void RefreshButtons()
	{
		RefreshButtonsEnabled();
		for (int num = loadButtons.Count - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(loadButtons[num].gameObject);
			loadButtons.RemoveAt(num);
		}
		string path = Path.Combine(Application.persistentDataPath, "Plane Designs");
		List<string> planeDirectories = Singleton<SteamworksManager>.Instance.GetPlaneDirectories();
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (local)
		{
			list2.AddRange(Directory.GetFiles(path, "*.planedesign", SearchOption.AllDirectories));
		}
		else
		{
			for (int i = 0; i < planeDirectories.Count; i++)
			{
				list2.AddRange(Directory.GetFiles(planeDirectories[i], "*.planedesign", SearchOption.AllDirectories));
				list.AddRange(Directory.GetFiles(planeDirectories[i], "*.planedesign", SearchOption.AllDirectories));
			}
		}
		string[] array = list2.ToArray();
		for (int j = 0; j < array.Length; j++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(buttonPrefab);
			gameObject.transform.SetParent(buttonParent, worldPositionStays: true);
			gameObject.transform.localScale = Vector3.one;
			try
			{
				gameObject.GetComponent<LoadPlaneButton>().Init(array[j], this, Singleton<PlaneStorage>.Instance.IsLocked(array[j]), (int)Singleton<PlaneStorage>.Instance.GetSaveFileCost(array[j]), list.Contains(array[j]));
				loadButtons.Add(gameObject.GetComponent<LoadPlaneButton>());
			}
			catch (Exception)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
		loadPanelOpenButton.Refresh();
	}

	public void ToggleConfirmationPanel()
	{
		if ((selectedFile != null && selectedFile != "") || confirmationPanel.gameObject.activeInHierarchy)
		{
			confirmationPanel.gameObject.SetActive(!confirmationPanel.gameObject.activeInHierarchy);
		}
	}

	public void Delete()
	{
		if (selectedFile != null && selectedFile != "")
		{
			File.Delete(selectedFile);
			string text = selectedFile;
			File.Delete(text.Substring(0, text.Length - 12) + ".jpg");
			Singleton<SteamworksManager>.Instance.UnsubscribeFromItem(selectedFile);
			RefreshButtons();
			DeleteEmptyDirectories(Path.Combine(Application.persistentDataPath, "Plane Designs"));
			WebGLFileSync.Flush();
		}
		confirmationPanel.gameObject.SetActive(value: false);
		if (!loadPanelOpenButton.Refresh())
		{
			Close();
		}
	}

	private static void DeleteEmptyDirectories(string root)
	{
		string[] directories = Directory.GetDirectories(root);
		foreach (string path in directories)
		{
			if (!Directory.EnumerateFileSystemEntries(path).Any())
			{
				Directory.Delete(path);
			}
		}
	}

	private void OnDestroy()
	{
		if (Singleton<SteamworksManager>.Instance != null)
		{
			SteamworksManager instance = Singleton<SteamworksManager>.Instance;
			instance.PlanesUpdated = (Action)Delegate.Remove(instance.PlanesUpdated, new Action(RefreshButtons));
		}
	}
}
