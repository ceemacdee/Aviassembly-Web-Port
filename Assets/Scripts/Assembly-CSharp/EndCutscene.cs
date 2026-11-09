using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndCutscene : MonoBehaviour
{
	public AirportData[] airports;

	public string[] sentences;

	public TMP_Text text;

	public GameObject continueButton;

	public GameObject background;

	public CameraController cameraController;

	public FlyingUIController uIController;

	public Canvas cutSceneCanvas;

	public CanvasGroup fader;

	public Vector2 offset;

	public Texture2D randomIcon;

	public bool cutScene;

	public bool cutSceneStarted;

	private bool cutScenePlayed;

	private bool messageDisplayed;

	private void Start()
	{
		cutSceneCanvas.gameObject.SetActive(value: false);
		text.gameObject.SetActive(value: false);
		background.gameObject.SetActive(value: false);
		List<AirportData> list = new List<AirportData>();
		for (int num = Singleton<AirportManager>.Instance.airports.Count - 1; num >= 0; num--)
		{
			AirportData data = Singleton<AirportManager>.Instance.airports[num].data;
			if (data.airportName.Contains("Waste Storage"))
			{
				list.Add(data);
			}
			if (data.airportName.Contains("Explosion Site"))
			{
				list.Add(data);
			}
			if (data.airportName.Contains("The Hospital"))
			{
				list.Add(data);
			}
		}
		airports = list.ToArray();
	}

	private void Update()
	{
		if (cutScenePlayed && !cutSceneStarted && !Singleton<QuestAdditionUI>.Instance.IsOpen() && !messageDisplayed)
		{
			Singleton<FlightWarningManager>.Instance.ShowWarning("Random quests unlocked", "You can now get random quests at every airport", randomIcon, 300, 20f, warning: false);
			messageDisplayed = true;
		}
		if (cutSceneStarted && Time.timeScale < 0.1f)
		{
			uIController.CloseAllWindows();
			uIController.LockUI();
			cameraController.transform.position -= cameraController.transform.forward * Time.unscaledDeltaTime * 10f;
		}
	}

	public void StartCutscene()
	{
		if (!cutScenePlayed)
		{
			cutScenePlayed = true;
			fader.alpha = 0f;
			cutSceneStarted = true;
			Singleton<QuestAdditionUI>.Instance.text.EnablePause(value: true);
			cutSceneCanvas.gameObject.SetActive(value: true);
			StartCoroutine(CutScene());
		}
	}

	public void StopCutScene()
	{
		StartCoroutine(ReturnToGame());
		Singleton<QuestAdditionUI>.Instance.text.EnablePause(value: false);
	}

	private void SelectNewAirport(AirportData data)
	{
		cameraController.cameraMode = CameraMode.Disabled;
		Airport airport = Singleton<AirportManager>.Instance.GetAirport(data);
		cameraController.transform.position = airport.position + Vector3.up * offset.x + Vector3.forward * offset.y;
		cameraController.transform.LookAt(airport.position);
	}

	private void OnDestoy()
	{
		Singleton<AudioManager>.Instance.SetEnginesMutes(muteEngines: false);
		Time.timeScale = 1f;
	}

	private IEnumerator ReturnToGame()
	{
		while (fader.alpha < 1f)
		{
			fader.alpha += Time.unscaledDeltaTime;
			fader.alpha = Mathf.Clamp01(fader.alpha);
			yield return null;
		}
		Time.timeScale = 1f;
		cameraController.cameraMode = CameraMode.Normal;
		cutSceneStarted = false;
		for (int i = 0; i < cutSceneCanvas.transform.childCount; i++)
		{
			if (cutSceneCanvas.transform.GetChild(i).gameObject != fader.gameObject)
			{
				cutSceneCanvas.transform.GetChild(i).gameObject.SetActive(value: false);
			}
		}
		yield return new WaitForSecondsRealtime(2f);
		while (fader.alpha > 0f)
		{
			fader.alpha -= Time.unscaledDeltaTime;
			fader.alpha = Mathf.Clamp01(fader.alpha);
			yield return null;
		}
		cutSceneCanvas.gameObject.SetActive(value: false);
		Singleton<AudioManager>.Instance.SetEnginesMutes(muteEngines: false);
	}

	private IEnumerator CutScene()
	{
		for (int i = 0; i < airports.Length; i++)
		{
			while (fader.alpha < 1f)
			{
				fader.alpha += Time.unscaledDeltaTime;
				fader.alpha = Mathf.Clamp01(fader.alpha);
				yield return null;
			}
			Time.timeScale = 0f;
			text.gameObject.SetActive(value: true);
			text.text = sentences[i];
			background.gameObject.SetActive(value: true);
			cameraController.cameraMode = CameraMode.Disabled;
			Singleton<AudioManager>.Instance.SetEnginesMutes(muteEngines: true);
			SelectNewAirport(airports[i]);
			if (i == airports.Length - 1)
			{
				continueButton.gameObject.SetActive(value: true);
			}
			yield return new WaitForSecondsRealtime(2f);
			while (fader.alpha > 0f)
			{
				fader.alpha -= Time.unscaledDeltaTime;
				fader.alpha = Mathf.Clamp01(fader.alpha);
				yield return null;
			}
			yield return new WaitForSecondsRealtime(4f);
		}
	}
}
