using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestAdditionUI : Singleton<QuestAdditionUI>
{
	public AudioDef writingSound;

	public AudioDef rewardSound;

	public TextWriter text;

	public FlyingUIController flyingUIController;

	public AudioClip regularRadioSound;

	public AudioClip staticRadioSound;

	public GameObject questAddedPopup;

	public GameObject questCompletedPopup;

	public float popupDisplayTime;

	private float questAddedTimer;

	private float questCompletedTimer;

	public CanvasGroup canvasGroup;

	public QuestAddedHighlighter questAddedHighlighter;

	public QuestAddedHighlighter editPlanePopup;

	public GameObject basePopup;

	public GameObject map;

	public StoryManager storyManager;

	public Action questAdded;

	private bool questCompleted;

	public int prevMoney;

	public int prevScrap;

	public List<CompletedPopupInfo> completedPopupInfos = new List<CompletedPopupInfo>();

	private void Start()
	{
		editPlanePopup.gameObject.SetActive(value: false);
	}

	private void ShowQuestAddedPopup()
	{
		Singleton<AudioManager>.Instance.PlaySound(writingSound);
		questAddedTimer = popupDisplayTime;
		questAddedPopup.gameObject.SetActive(value: true);
		text.EnablePause(value: true);
		Singleton<QuestFeedbackManager>.Instance.questsAvailable = true;
		Singleton<StoryState>.Instance.selectNewQuestTimer = 0f;
	}

	private void ShowQuestCompletedPopup()
	{
		if (completedPopupInfos.Count != 0)
		{
			CompletedPopupInfo completedPopupInfo = completedPopupInfos[0];
			completedPopupInfos.RemoveAt(0);
			if (completedPopupInfo.completed)
			{
				Singleton<AudioManager>.Instance.PlaySound(rewardSound);
			}
			questCompletedTimer = popupDisplayTime;
			questCompletedPopup.gameObject.GetComponent<QuestCompletedPopup>().Trigger(completedPopupInfo.prevMoney, completedPopupInfo.prevScrap, completedPopupInfo.prevAdvancedScrap, completedPopupInfo.targetMoney, completedPopupInfo.targetScrap, completedPopupInfo.targetAdvancedScrap, completedPopupInfo.completed);
			text.EnablePause(value: true);
			questCompleted = true;
		}
	}

	private void Update()
	{
		if (text.IsDone() && !questCompletedPopup.activeInHierarchy && !questAddedPopup.activeInHierarchy)
		{
			Singleton<StoryState>.Instance.selectNewQuestTimer += Time.deltaTime;
		}
		editPlanePopup.gameObject.SetActive((!Singleton<CurrentGameData>.Instance.editPlanePressed || !Singleton<ResearchManager>.Instance.UsedResearch()) && questCompleted && Singleton<ResearchManager>.Instance.researchPoints >= 10 && !basePopup.activeInHierarchy);
		questAddedHighlighter.gameObject.SetActive(Singleton<QuestFeedbackManager>.Instance.currentQuest == null && Singleton<QuestFeedbackManager>.Instance.questsAvailable && !map.gameObject.activeInHierarchy && !editPlanePopup.gameObject.activeInHierarchy && Singleton<StoryState>.Instance.selectNewQuestTimer < 15f);
		questAddedTimer -= Time.deltaTime;
		if (questAddedTimer < 0f)
		{
			questAddedPopup.gameObject.SetActive(value: false);
		}
		questCompletedTimer -= Time.deltaTime;
		if (questCompletedTimer < 0f && questAddedTimer < 0f && !questCompletedPopup.gameObject.activeInHierarchy && !storyManager.FinalCutSceneIsPlaying())
		{
			text.EnablePause(value: false);
		}
		text.UpdateTextBox();
		if (IsOpen())
		{
			flyingUIController.OpenWindow(canvasGroup, enableBlackBackground: false);
			flyingUIController.LockUI();
		}
		else
		{
			flyingUIController.CloseWindow(canvasGroup);
		}
	}

	public void DisplayText(string text, MessageType messageType, Action<bool> callback = null)
	{
		if (!string.IsNullOrEmpty(text))
		{
			Action onTextReadCallback = messageType switch
			{
				MessageType.QuestCompleted => ShowQuestCompletedPopup, 
				MessageType.QuestAdded => ShowQuestAddedPopup, 
				_ => null, 
			};
			QueuedText queuedText = this.text.QueueText(text, onTextReadCallback);
			if (messageType == MessageType.Mystery)
			{
				this.text.audioSource.clip = staticRadioSound;
			}
			else
			{
				this.text.audioSource.clip = regularRadioSound;
			}
			if (callback != null)
			{
				queuedText.callback = (Action<bool>)Delegate.Combine(queuedText.callback, callback);
			}
		}
	}

	public bool IsDispayingText()
	{
		return text.IsDisplayingText();
	}

	public bool IsProcessingQuests()
	{
		if (!questAddedPopup.activeInHierarchy && !questCompletedPopup.activeInHierarchy)
		{
			return text.IsProcessingQuests();
		}
		return true;
	}

	public bool IsOpen()
	{
		if (!IsDispayingText() && !questAddedPopup.gameObject.activeInHierarchy)
		{
			return questCompletedPopup.gameObject.activeInHierarchy;
		}
		return true;
	}
}
