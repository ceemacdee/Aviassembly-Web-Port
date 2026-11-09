using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadButton : MonoBehaviour
{
	public TMP_Text header;

	public TMP_Text dateText;

	public TMP_Text playtimeText;

	public Image outline;

	public int version;

	private string filename;

	private LoadPanel loadPanel;

	public void Init(string filename, string date, LoadPanel loadPanel)
	{
		header.text = filename;
		dateText.text = date;
		SaveFileHeader saveFileHeader = PersistentStorage.GetHeader(filename);
		if (header != null)
		{
			float playtime = saveFileHeader.playtime;
			playtimeText.text = Mathf.RoundToInt(playtime / 60f) + " Minutes";
		}
		this.filename = filename;
		this.loadPanel = loadPanel;
		version = saveFileHeader.version;
	}

	public void Reset()
	{
		outline.color = new Color(0f, 0f, 0f, 0f);
	}

	public void Press()
	{
		loadPanel.SelectFile(filename);
		outline.color = Color.black;
	}
}
