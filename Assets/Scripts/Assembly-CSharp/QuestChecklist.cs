using System.Collections;
using TMPro;
using UnityEngine;

public class QuestChecklist : MonoBehaviour
{
	public TMP_Text text;

	public TMP_Text header;

	public CanvasGroup canvasGroup;

	public CanvasGroup completionOverlay;

	public float fadeSpeed;

	public float completionDuration;

	public Quest prevQuest;

	public int prevStage;

	private void Start()
	{
		completionOverlay.alpha = 0f;
	}

	private void Update()
	{
		if (Singleton<QuestFeedbackManager>.Instance.currentQuest == null)
		{
			prevQuest = null;
			canvasGroup.gameObject.SetActive(value: false);
			return;
		}
		canvasGroup.gameObject.SetActive(value: true);
		QuestStage currentStageDescription = Singleton<QuestFeedbackManager>.Instance.currentQuest.GetCurrentStageDescription();
		text.text = currentStageDescription.description;
		header.text = Singleton<QuestFeedbackManager>.Instance.currentQuest.questName;
		if (prevQuest == Singleton<QuestFeedbackManager>.Instance.currentQuest && currentStageDescription.stage > prevStage)
		{
			CompleteQuest();
		}
		prevQuest = Singleton<QuestFeedbackManager>.Instance.currentQuest;
		prevStage = currentStageDescription.stage;
	}

	private void CompleteQuest()
	{
		StartCoroutine(CompletionAnimtation());
	}

	private IEnumerator CompletionAnimtation()
	{
		while (completionOverlay.alpha < 0.98f)
		{
			completionOverlay.alpha += Time.deltaTime * fadeSpeed;
			yield return null;
		}
		completionOverlay.alpha = 1f;
		yield return new WaitForSeconds(completionDuration);
		while (completionOverlay.alpha > 0.02f)
		{
			completionOverlay.alpha -= Time.deltaTime * fadeSpeed;
			yield return null;
		}
		completionOverlay.alpha = 0f;
	}
}
