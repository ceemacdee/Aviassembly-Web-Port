using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AirportMapIcon : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[HideInInspector]
	public Airport airport;

	[HideInInspector]
	public MapInspector panel;

	public RectTransform outline;

	public RawImage icon;

	public RawImage highlighter;

	public float highlighterBlinkSpeed;

	public Texture defaultIcon;

	public Texture heliIcon;

	public Texture baseIcon;

	public GameObject completedCheckmark;

	public static AirportTooltipData tooltipData = new AirportTooltipData();

	private Button button;

	private bool hover;

	private void Awake()
	{
		button = GetComponent<Button>();
		outline.gameObject.SetActive(value: false);
	}

	public void Update()
	{
		completedCheckmark.SetActive(value: false);
		outline.gameObject.SetActive(Singleton<QuestFeedbackManager>.Instance.currentQuest != null && Singleton<QuestFeedbackManager>.Instance.currentQuest == airport.currentQuest);
		if (button != null)
		{
			button.enabled = !completedCheckmark.activeInHierarchy;
		}
		if (airport.cargoType != null && airport.cargoType.Length != 0)
		{
			icon.texture = airport.cargoType[0].icon;
		}
		else if (airport.data != null && airport.data.offshoreAirport)
		{
			icon.texture = heliIcon;
		}
		else
		{
			icon.texture = defaultIcon;
		}
		if (airport.data.baseAirport)
		{
			icon.texture = baseIcon;
		}
		highlighter.gameObject.SetActive(airport.currentQuest != null && airport.questInitialized && !Singleton<AirportManager>.Instance.hoveredQuests.Contains(airport.currentQuest));
		Color color = highlighter.color;
		color.a = (Mathf.Sin(Time.time * highlighterBlinkSpeed) + 1f) * 0.5f;
		highlighter.color = color;
	}

	public void OnPointerEnter(PointerEventData data)
	{
		if (!completedCheckmark.activeInHierarchy)
		{
			AirportManager.airportHover = true;
			panel.SelectAirport(airport);
			hover = true;
			if (!Singleton<AirportManager>.Instance.hoveredQuests.Contains(airport.currentQuest))
			{
				Singleton<AirportManager>.Instance.hoveredQuests.Add(airport.currentQuest);
			}
		}
	}

	public void OnPointerExit(PointerEventData data)
	{
		AirportManager.airportHover = false;
		panel.Disable();
		hover = false;
	}

	public void Select()
	{
		if (airport.questInitialized && airport.currentQuest != null && !airport.currentQuest.completed)
		{
			Singleton<QuestFeedbackManager>.Instance.SelectQuest(airport.currentQuest);
		}
	}
}
