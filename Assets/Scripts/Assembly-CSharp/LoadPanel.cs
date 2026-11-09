using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LoadPanel : MonoBehaviour
{
	public GameObject demoWarning;

	public GameObject prefab;

	public Transform parent;

	public GameObject deletionDialog;

	private List<LoadButton> loadButtons = new List<LoadButton>();

	private void Start()
	{
		PersistentStorage.CreateSaveFolder();
		RefreshButtons();
		SetDeleteDialogEnabled(value: false);
	}

	public void SelectFile(string filename)
	{
		for (int i = 0; i < loadButtons.Count; i++)
		{
			loadButtons[i].Reset();
		}
		Singleton<GameManager>.Instance.currentSaveFile = filename;
		SetDeleteDialogEnabled(value: false);
	}

	public void SetDeleteDialogEnabled(bool value)
	{
		deletionDialog.SetActive(value);
	}

	public void Delete()
	{
		string currentSaveFile = Singleton<GameManager>.Instance.currentSaveFile;
		if (currentSaveFile != null && currentSaveFile != "")
		{
			File.Delete(Path.Combine(Path.Combine(Application.persistentDataPath, "SaveGames"), currentSaveFile + ".plane"));
			WebGLFileSync.Flush();
			RefreshButtons();
		}
		SetDeleteDialogEnabled(value: false);
	}

	public void Load()
	{
		if (PersistentStorage.GetHeader(Singleton<GameManager>.Instance.currentSaveFile).version < 14)
		{
			demoWarning.gameObject.SetActive(value: true);
		}
		else
		{
			Singleton<GameManager>.Instance.StartGame();
		}
	}

	private void RefreshButtons()
	{
		for (int num = loadButtons.Count - 1; num >= 0; num--)
		{
			Object.Destroy(loadButtons[num].gameObject);
			loadButtons.RemoveAt(num);
		}
		string[] files = Directory.GetFiles(Path.Combine(Application.persistentDataPath, "SaveGames"), "*.plane");
		for (int i = 0; i < files.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(prefab);
			gameObject.transform.SetParent(parent, worldPositionStays: true);
			gameObject.transform.localScale = Vector3.one;
			string date = File.GetLastWriteTime(files[i]).ToString("dd  MMMM  yyyy");
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
			gameObject.GetComponent<LoadButton>().Init(fileNameWithoutExtension, date, this);
			loadButtons.Add(gameObject.GetComponent<LoadButton>());
		}
		if (loadButtons.Count > 0)
		{
			loadButtons[0].Press();
		}
	}
}
