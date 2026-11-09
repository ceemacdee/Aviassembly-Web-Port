using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadPlaneButton : MonoBehaviour
{
	public TMP_Text header;

	public TMP_Text cost;

	public RawImage coinIcon;

	public GameObject workshopIcon;

	public Color normal;

	public Color red;

	public GameObject lockObject;

	public GameObject outline;

	private string filePath;

	private bool isWorkshopFile;

	private int costValue;

	private string[] requiredParts;

	private LoadPlanePanel panel;

	public void Init(string filePath, LoadPlanePanel loadPanel, bool locked, int cost, bool workshopItem)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
		header.text = fileNameWithoutExtension;
		this.filePath = filePath;
		panel = loadPanel;
		bool flag = Singleton<PlaneStorage>.Instance.CanAffordPlane(cost);
		bool flag2 = Singleton<PlaneStorage>.Instance.ResearchUnlocked(filePath);
		lockObject.SetActive(!flag2);
		GetComponent<TooltipTrigger>().enabled = !flag2;
		this.cost.text = cost.ToString();
		this.cost.color = (flag ? normal : red);
		this.cost.gameObject.SetActive(flag2);
		coinIcon.color = this.cost.color;
		outline.gameObject.SetActive(value: false);
		workshopIcon.SetActive(value: false);
		isWorkshopFile = workshopItem;
		costValue = cost;
		requiredParts = Singleton<PlaneStorage>.Instance.GetSaveFileRequiredParts(filePath);
	}

	private void Update()
	{
		outline.gameObject.SetActive(panel.selectedFile == filePath);
		bool flag = Singleton<PlaneStorage>.Instance.ResearchUnlocked(requiredParts);
		cost.color = (Singleton<PlaneStorage>.Instance.CanAffordPlane(costValue) ? normal : red);
		lockObject.SetActive(!flag);
		GetComponent<TooltipTrigger>().enabled = !flag;
		cost.gameObject.SetActive(flag);
	}

	public void Press()
	{
		panel.SelectFile(filePath, isWorkshopFile);
	}
}
