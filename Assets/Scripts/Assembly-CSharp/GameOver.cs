using System.Collections;
using TMPro;
using UnityEngine;

public class GameOver : MonoBehaviour
{
	public CanvasGroup deathScreen;

	public CanvasGroup content;

	public GameObject lastAirportButton;

	public float fadeSpeed;

	public TMP_Text text;

	public TMP_Text textShadow;

	private PlaneContainer planeContainer;

	private PlaneController planeController;

	private GameManager currentGameManager;

	private bool reloading;

	private float outOfFuelDelay;

	private void Start()
	{
		planeContainer = Singleton<PlaneContainer>.Instance;
		currentGameManager = Singleton<GameManager>.Instance;
		planeController = planeContainer.gameObject.GetComponent<PlaneController>();
		deathScreen.alpha = 0f;
		content.alpha = 0f;
		deathScreen.gameObject.SetActive(value: false);
		deathScreen.transform.SetAsLastSibling();
	}

	private void Update()
	{
		textShadow.text = text.text;
		bool flag = false;
		Rigidbody component = Singleton<PlaneContainer>.Instance.GetComponent<Rigidbody>();
		if (component != null)
		{
			Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(component.transform.position);
			flag = Singleton<PlaneContainer>.Instance.IsAtAirport(closestAirport) && (closestAirport.data == null || closestAirport.data.refuelAvailable);
		}
		bool exploded = planeController.Exploded;
		bool flag2 = false;
		if (planeContainer.GetVelocityMagintude() < 25f && !flag && !Singleton<QuestAdditionUI>.Instance.IsProcessingQuests())
		{
			flag2 = planeContainer.fuel < 0.001f && planeContainer.fuelCapacity > 0f;
		}
		if (flag2)
		{
			outOfFuelDelay += Time.deltaTime;
		}
		else
		{
			outOfFuelDelay = 0f;
		}
		Airport closestAirport2 = Singleton<AirportManager>.Instance.GetClosestAirport(Singleton<AirportManager>.Instance.LastAirport);
		if (!Singleton<GameManager>.Instance.Loading)
		{
			lastAirportButton.SetActive(!closestAirport2.data.baseAirport);
		}
		if (exploded || (flag2 && outOfFuelDelay > 6f))
		{
			if (exploded)
			{
				text.text = "You Crashed";
			}
			if (flag2)
			{
				text.text = "Out of Fuel";
			}
			StartReload();
		}
	}

	public void ToBase()
	{
		Singleton<AirportManager>.Instance.ResetAtLastAirport();
		currentGameManager.StartFlyMode(resetPlane: true, resetCargo: true);
	}

	public void ToLastAirport()
	{
		currentGameManager.StartFlyMode(resetPlane: true, resetCargo: true);
	}

	private void StartReload()
	{
		if (!reloading)
		{
			deathScreen.gameObject.SetActive(value: true);
			reloading = true;
			StartCoroutine(ReloadGame());
		}
	}

	private IEnumerator ReloadGame()
	{
		yield return new WaitForSeconds(1f);
		while (deathScreen.alpha < 1f)
		{
			deathScreen.alpha += Time.deltaTime * fadeSpeed;
			yield return null;
		}
		while (content.alpha < 1f)
		{
			content.alpha += Time.deltaTime * fadeSpeed;
			yield return null;
		}
		yield return new WaitForSeconds(1f);
	}
}
