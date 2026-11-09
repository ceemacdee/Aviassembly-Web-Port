using System;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopUploadFeedbackPanel : MonoBehaviour
{
	public TMP_Text resultText;

	public GameObject agreementPopup;

	public GameObject uploadResultFeedback;

	public UnityEngine.UI.Image uploadResultBackground;

	public GameObject button;

	public GameObject progressBarObject;

	public UnityEngine.UI.Image progressBar;

	public UnityEngine.Color green;

	public UnityEngine.Color red;

	public GameObject openWorkshopHider;

	public Button openWorkshopButton;

	private PublishedFileId fileID;

	private ProgressClass progress;

	public void Open(ProgressClass progressClass)
	{
		progress = progressClass;
		base.gameObject.SetActive(value: true);
		agreementPopup.SetActive(value: false);
		button.SetActive(value: false);
		uploadResultFeedback.SetActive(value: false);
		progressBarObject.SetActive(value: true);
	}

	private void Update()
	{
		if (progress != null)
		{
			progressBar.fillAmount = progress.progressValue;
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}

	private void ReceiveResult(PublishResult result)
	{
		progressBarObject.SetActive(value: false);
		agreementPopup.SetActive(result.NeedsWorkshopAgreement);
		button.SetActive(value: true);
		uploadResultFeedback.SetActive(value: true);
		uploadResultBackground.color = (result.Success ? green : red);
		openWorkshopButton.interactable = result.Success;
		openWorkshopHider.SetActive(!result.Success);
		if (result.Success)
		{
			fileID = result.FileId;
			resultText.text = "Workshop upload succeeded";
		}
		else
		{
			resultText.text = "Workshop upload failed";
		}
	}

	public void OpenAgreementPage()
	{
		string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={fileID}";
		if (SteamUtils.IsOverlayEnabled)
		{
			SteamFriends.OpenWebOverlay(url);
		}
		else
		{
			Application.OpenURL(url);
		}
	}

	private void OnDestroy()
	{
		if (Singleton<SteamworksManager>.Instance != null)
		{
			SteamworksManager instance = Singleton<SteamworksManager>.Instance;
			instance.ResultAvailable = (Action<PublishResult>)Delegate.Remove(instance.ResultAvailable, new Action<PublishResult>(ReceiveResult));
		}
	}

	private void Awake()
	{
		SteamworksManager instance = Singleton<SteamworksManager>.Instance;
		instance.ResultAvailable = (Action<PublishResult>)Delegate.Combine(instance.ResultAvailable, new Action<PublishResult>(ReceiveResult));
		base.gameObject.SetActive(value: false);
	}
}
