using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class TextWriter : MonoBehaviour
{
	public TMP_Text text;

	public TMP_Text continuePopup;

	[Space(10f)]
	public float timePerCharacter;

	public float maxCharacters;

	public float nextTextDelay;

	public float continueMessageBlinkSpeed;

	public AudioSource audioSource;

	public AudioDef radioClick;

	private float audioVolumeVelocity;

	private float targetVolume;

	private string currentTargetText;

	private QueuedText currentQueuedText;

	private float timer;

	private int characterIndex;

	private float continueMessageTime;

	private CanvasGroup canvasGroup;

	private float alphaVelocity;

	private float targetAlpha;

	private List<QueuedText> textQueue = new List<QueuedText>();

	private bool textboxDisabled;

	private bool mouseClick;

	public bool paused;

	public QueuedText QueueText(string text, Action onTextReadCallback = null)
	{
		if ((float)text.Length > maxCharacters)
		{
			string[] source = Regex.Split(text, "(?<=[.?!])");
			source = source.Where((string x) => !string.IsNullOrEmpty(x)).ToArray();
			List<string> list = new List<string>(source);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (string.IsNullOrWhiteSpace(list[num]))
				{
					list.RemoveAt(num);
				}
			}
			source = list.ToArray();
			for (int num2 = 0; num2 < source.Length; num2++)
			{
				QueuedText queuedText = new QueuedText(source[num2], (num2 == source.Length - 1) ? onTextReadCallback : null);
				textQueue.Add(queuedText);
				if (num2 == source.Length - 1)
				{
					return queuedText;
				}
			}
			UpdateTextBox();
			return null;
		}
		QueuedText queuedText2 = new QueuedText(text, onTextReadCallback);
		textQueue.Add(queuedText2);
		return queuedText2;
	}

	public bool IsDisplayingText()
	{
		return !textboxDisabled;
	}

	public bool IsDone()
	{
		if (string.IsNullOrEmpty(currentTargetText))
		{
			return true;
		}
		return characterIndex >= currentTargetText.Length;
	}

	public void EnablePause(bool value)
	{
		paused = value;
	}

	private void Start()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		textboxDisabled = true;
		text.text = "";
		audioSource.volume = 0f;
		targetVolume = 0f;
	}

	private void StopAudio()
	{
		if (targetVolume != 0f)
		{
			Singleton<AudioManager>.Instance.PlaySound(radioClick);
			targetVolume = 0f;
		}
	}

	private void StartAudio()
	{
		Singleton<AudioManager>.Instance.PlaySound(radioClick);
		audioSource.Play();
		targetVolume = 0.5f;
	}

	private void UpdateContinuePopup()
	{
		bool flag = IsDone();
		continuePopup.gameObject.SetActive(flag);
		continuePopup.color = new Color(1f, 1f, 1f, (Mathf.Sin(continueMessageTime * continueMessageBlinkSpeed + -MathF.PI / 2f) + 1f) * 0.5f);
		if (flag)
		{
			continueMessageTime += Time.deltaTime;
		}
		else
		{
			continueMessageTime = 0f;
		}
	}

	private void SetText(string text)
	{
		currentTargetText = text;
		characterIndex = -1;
		UpdateTextWriting(skipable: false);
		StartAudio();
	}

	private void UpdateTextWriting(bool skipable)
	{
		if (string.IsNullOrEmpty(currentTargetText))
		{
			return;
		}
		if ((mouseClick && characterIndex < currentTargetText.Length) & skipable)
		{
			this.text.text = currentTargetText;
			characterIndex = currentTargetText.Length;
			mouseClick = false;
		}
		if (IsDone())
		{
			StopAudio();
		}
		audioSource.volume = Mathf.SmoothDamp(audioSource.volume, targetVolume, ref audioVolumeVelocity, 0.15f);
		if (audioSource.volume < 0.01f && Mathf.Approximately(targetVolume, 0f))
		{
			audioSource.Stop();
		}
		timer -= Time.deltaTime;
		while (timer < 0f)
		{
			timer = timePerCharacter;
			characterIndex++;
			if (characterIndex < currentTargetText.Length && currentTargetText[characterIndex] == '<')
			{
				int num = 0;
				while (characterIndex < currentTargetText.Length && currentTargetText[characterIndex] != '>' && num < 50)
				{
					characterIndex++;
					num++;
				}
				characterIndex++;
			}
			if (characterIndex > currentTargetText.Length)
			{
				break;
			}
			string text = currentTargetText.Substring(0, characterIndex);
			text = text + "<color=#00000000>" + currentTargetText.Substring(characterIndex) + "</color>";
			this.text.text = text;
		}
	}

	public void UpdateTextBox()
	{
		mouseClick = MouseInput.GetMouseButtonDown(0);
		UpdateTextWriting(skipable: true);
		if (currentQueuedText != null && IsDone() && mouseClick)
		{
			if (currentQueuedText.additionUICallback != null)
			{
				currentQueuedText.additionUICallback();
				if (currentQueuedText.callback != null)
				{
					currentQueuedText.callback(obj: true);
				}
			}
			currentQueuedText = null;
		}
		if (IsDone() && textQueue.Count > 0 && (mouseClick || textboxDisabled) && !paused)
		{
			SetText(textQueue[0].text);
			currentQueuedText = textQueue[0];
			textQueue.RemoveAt(0);
			textboxDisabled = false;
			targetAlpha = 1f;
		}
		if (IsDone() && mouseClick)
		{
			textboxDisabled = true;
			targetAlpha = 0f;
		}
		UpdateContinuePopup();
		canvasGroup.alpha = Mathf.SmoothDamp(canvasGroup.alpha, targetAlpha, ref alphaVelocity, 0.1f);
	}

	public bool IsProcessingQuests()
	{
		if (IsDone())
		{
			return textQueue.Count != 0;
		}
		return true;
	}

	private void OnDestroy()
	{
		for (int i = 0; i < textQueue.Count; i++)
		{
			if (textQueue[i] != null && textQueue[i].callback != null)
			{
				textQueue[i].callback(obj: false);
			}
		}
		if (currentQueuedText != null && currentQueuedText.callback != null)
		{
			currentQueuedText.callback(obj: false);
		}
	}
}
