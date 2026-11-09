using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartFlightButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public WarningSystem warningSystem;

	private Button button;

	public Color white;

	public Color orange;

	public Image background;

	public RawImage icon;

	private void Awake()
	{
		button = GetComponent<Button>();
	}

	private void Update()
	{
		if (Singleton<PlaneContainer>.Instance != null)
		{
			button.interactable = Singleton<PlaneContainer>.Instance.transform.childCount > 0 && PartPlacer.PlaneReady;
		}
	}

	public void StartFlight()
	{
		warningSystem.StartFlightMode();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		background.color = orange;
		icon.color = Color.white;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		background.color = white;
		icon.color = Color.black;
	}
}
