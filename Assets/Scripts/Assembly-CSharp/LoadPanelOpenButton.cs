using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LoadPanelOpenButton : MonoBehaviour
{
	private Button button;

	private void Start()
	{
		button = GetComponent<Button>();
		Refresh();
	}

	public bool Refresh()
	{
		string path = Path.Combine(Application.persistentDataPath, "Plane Designs");
		List<string> planeDirectories = Singleton<SteamworksManager>.Instance.GetPlaneDirectories();
		List<string> list = new List<string>();
		if (Directory.Exists(path))
		{
			list.AddRange(Directory.GetFiles(path, "*.planedesign", SearchOption.AllDirectories));
		}
		for (int i = 0; i < planeDirectories.Count; i++)
		{
			if (Directory.Exists(planeDirectories[i]))
			{
				list.AddRange(Directory.GetFiles(planeDirectories[i], "*.planedesign", SearchOption.AllDirectories));
			}
		}
		button.interactable = list.Count > 0;
		return button.interactable;
	}
}
