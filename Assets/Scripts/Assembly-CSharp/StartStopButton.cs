using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartStopButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public RawImage buttonVisual;

	public RawImage icon;

	public GameObject warning;

	public GameObject thisAirportButton;

	public TMP_Text thisAirportText;

	private Color orangeHoverColor;

	private bool pointerEnter;

	private void Start()
	{
		warning.SetActive(value: false);
		orangeHoverColor = new Color(0.7843137f, 0.4823529f, 0.172549f);
	}

	public void OpenWarning()
	{
		warning.SetActive(!warning.activeInHierarchy);
	}

	private void Update()
	{
		if (Input.GetMouseButtonUp(0) && !Singleton<MouseInput>.Instance.PointerIsOverUI)
		{
			warning.SetActive(value: false);
		}
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(Singleton<AirportManager>.Instance.LastAirport);
		thisAirportButton.gameObject.SetActive(!closestAirport.data.baseAirport);
		thisAirportText.text = (Singleton<PlaneContainer>.Instance.IsAtAirport() ? "This airport" : "Last airport");
		buttonVisual.color = ((warning.activeInHierarchy || pointerEnter) ? orangeHoverColor : Color.white);
		icon.color = ((warning.activeInHierarchy || pointerEnter) ? Color.white : Color.black);
	}

	public void ReloadFlyMode()
	{
		Singleton<AirportManager>.Instance.ResetAtLastAirport();
		Singleton<GameManager>.Instance.StartFlyMode(resetPlane: true, resetCargo: true);
	}

	public void ResetAtLastAirport()
	{
		Singleton<GameManager>.Instance.StartFlyMode();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerEnter = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		pointerEnter = false;
	}
}
