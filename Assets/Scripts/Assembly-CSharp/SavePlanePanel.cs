using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavePlanePanel : MonoBehaviour
{
	public float fadeSpeed;

	public GameObject panel;

	public GameObject invalidFilenameText;

	public GameObject saveButtons;

	public GameObject confirmUploadPanel;

	public GameObject tagButton;

	public Button uploadToWorkshop;

	public GameObject uploadToWorkshopBlocker;

	public GameObject background;

	public LoadPanelOpenButton loadPanelOpenButton;

	public WorkshopUploadFeedbackPanel workshopUploadFeedbackPanel;

	public TMP_InputField nameField;

	public TMP_InputField desciptionField;

	public TMP_Dropdown tagDropdown;

	private CanvasGroup canvasGroup;

	private int alphaDirection;

	private PlaneThumbnailGenerator planeThumbnailGenerator;

	private PlaneStorage planeStorage;

	private SteamworksManager steamworksManager;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		steamworksManager = Singleton<SteamworksManager>.Instance;
		canvasGroup.alpha = 0f;
		base.gameObject.SetActive(value: false);
	}

	private void Start()
	{
		planeStorage = Singleton<PlaneStorage>.Instance;
		planeThumbnailGenerator = Singleton<PlaneThumbnailGenerator>.Instance;
	}

	public void Open()
	{
		alphaDirection = 1;
		base.gameObject.SetActive(value: true);
		if (confirmUploadPanel.activeInHierarchy)
		{
			ToggleConfirmationPanel();
		}
	}

	public void ToggleConfirmationPanel()
	{
		confirmUploadPanel.SetActive(!confirmUploadPanel.activeInHierarchy);
		background.transform.SetSiblingIndex(confirmUploadPanel.activeInHierarchy ? 1 : 0);
	}

	public void Close()
	{
		nameField.SetTextWithoutNotify("");
		desciptionField.SetTextWithoutNotify("");
		tagDropdown.SetValueWithoutNotify(0);
		alphaDirection = -1;
	}

	public void SavePlane()
	{
		string filename = FileNameUtility.Sanitize(nameField.text);
		if (FileNameUtility.IsValidFilename(filename))
		{
			Singleton<PlaneStorage>.Instance.SaveToFile(filename);
			loadPanelOpenButton.Refresh();
			Close();
		}
	}

	public void UploadPlane()
	{
		string text = FileNameUtility.Sanitize(nameField.text);
		string text2 = desciptionField.text;
		if (FileNameUtility.IsValidFilename(text))
		{
			string planeFilePath = planeStorage.SaveToFile(text);
			string previewPath = planeThumbnailGenerator.GenerateThumbnail(text);
			ProgressClass progressClass = new ProgressClass();
			workshopUploadFeedbackPanel.Open(progressClass);
			Singleton<SteamworksManager>.Instance.UploadPlane(planeFilePath, previewPath, text, text2, tagDropdown.captionText.text, progressClass);
			loadPanelOpenButton.Refresh();
			Close();
		}
	}

	public void Update()
	{
		bool flag = FileNameUtility.IsValidFilename(FileNameUtility.Sanitize(nameField.text));
		saveButtons.SetActive(flag);
		invalidFilenameText.SetActive(!flag);
		bool flag2 = steamworksManager.steamworksInitialized && desciptionField.text.Length > 0;
		uploadToWorkshop.interactable = flag2;
		uploadToWorkshopBlocker.SetActive(!flag2);
		tagButton.SetActive(flag2);
		canvasGroup.alpha += fadeSpeed * (float)alphaDirection * Time.deltaTime;
		panel.gameObject.SetActive(canvasGroup.alpha > 0.75f);
		if (alphaDirection < 0 && canvasGroup.alpha < 0.05f)
		{
			base.gameObject.SetActive(value: false);
		}
		if ((!Singleton<MouseInput>.Instance.GetPointerIsOverUI() && MouseInput.GetMouseButtonDown(0)) || MouseInput.GetMouseButton(1))
		{
			Close();
		}
	}
}
