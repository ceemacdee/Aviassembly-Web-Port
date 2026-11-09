using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AirportInspector : MonoBehaviour
{
	private struct InspectorButtonData
	{
		public Transform button;

		public bool enabled;

		public float size;

		public ButtonHighlight highlight;
	}

	[Header("Buttons")]
	public Transform compass;

	public GameObject cargobutton;

	public GameObject editPlaneButton;

	public GameObject ratingButton;

	public Button refuelButton;

	private InspectorButtonData[] allButtons;

	public RawImage airportIconImage;

	public Texture airportIcon;

	public Texture baseIcon;

	public CanvasGroup refuelButtonCanvasGroup;

	public TooltipTrigger refuelTooltip;

	public GameObject newQuestAddedHighlighter;

	public GameObject researchButton;

	public GameObject buttonPrompt;

	[Header("Misc")]
	public TMP_Text title;

	public RectTransform airportNameBackground;

	public GameObject cargoHider;

	[Space(10f)]
	public AnimationCurve animCurve;

	public AnimationCurve fadeOutCurve;

	public float animSpeed;

	public Transform highlighter;

	private bool buttonsEnabled;

	private bool UILocked;

	private void Start()
	{
		allButtons = new InspectorButtonData[7];
		allButtons[0].button = compass;
		allButtons[0].enabled = false;
		allButtons[1].button = airportNameBackground.transform;
		allButtons[1].enabled = true;
		allButtons[2].button = editPlaneButton.transform;
		allButtons[2].highlight = editPlaneButton.gameObject.GetComponent<ButtonHighlight>();
		allButtons[3].button = cargobutton.transform;
		allButtons[3].highlight = cargobutton.gameObject.GetComponent<ButtonHighlight>();
		allButtons[4].button = researchButton.transform;
		allButtons[4].highlight = researchButton.gameObject.GetComponent<ButtonHighlight>();
		allButtons[5].button = ratingButton.transform;
		allButtons[5].highlight = ratingButton.gameObject.GetComponent<ButtonHighlight>();
		allButtons[6].button = refuelButton.transform;
		allButtons[6].highlight = refuelButton.gameObject.GetComponent<ButtonHighlight>();
	}

	private void Update()
	{
		for (int i = 0; i < allButtons.Length; i++)
		{
			if (allButtons[i].highlight != null)
			{
				allButtons[i].highlight.isHighlighted = false;
			}
		}
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(Singleton<PlaneContainer>.Instance.transform.position);
		allButtons[0].enabled = !buttonsEnabled && !UILocked;
		for (int j = 0; j < allButtons.Length; j++)
		{
			int num = (buttonsEnabled ? j : (allButtons.Length - j - 1));
			float num2 = ((allButtons[num].enabled && (buttonsEnabled || num == 0)) ? 1 : (-1));
			float num3 = ((num2 > 0f) ? animSpeed : (animSpeed * 1.65f));
			allButtons[num].size += Time.unscaledDeltaTime * num2 * num3;
			allButtons[num].size = Mathf.Clamp01(allButtons[num].size);
			if (!Mathf.Approximately(allButtons[num].size, Mathf.Clamp01(num2)))
			{
				break;
			}
		}
		for (int k = 0; k < allButtons.Length; k++)
		{
			allButtons[k].button.gameObject.SetActive(allButtons[k].size > 0.01f);
			float num4 = (((float)((allButtons[k].enabled && buttonsEnabled) ? 1 : (-1)) > 0f) ? animCurve.Evaluate(allButtons[k].size) : fadeOutCurve.Evaluate(allButtons[k].size));
			allButtons[k].button.localScale = Vector3.one * num4;
		}
		bool flag = Singleton<CargoInventory>.Instance.CurrentVolume == 0f;
		cargobutton.GetComponent<Button>().interactable = closestAirport.CargoUnlocked();
		cargoHider.SetActive(!closestAirport.CargoUnlocked());
		if (newQuestAddedHighlighter.activeInHierarchy)
		{
			return;
		}
		if (Singleton<PlaneContainer>.Instance.fuel < 0.8f && refuelButton.gameObject.activeInHierarchy)
		{
			allButtons[5].highlight.isHighlighted = true;
			return;
		}
		if (flag && cargobutton.activeInHierarchy && closestAirport.CargoUnlocked())
		{
			allButtons[3].highlight.isHighlighted = true;
			return;
		}
		if (editPlaneButton.activeInHierarchy && Singleton<MoneyManager>.Instance.money > 200f && !Singleton<CurrentGameData>.Instance.editPlanePressed)
		{
			allButtons[2].highlight.isHighlighted = true;
		}
		if (Singleton<ResearchManager>.Instance.researchPoints > 0 && !Singleton<ResearchManager>.Instance.UsedResearch())
		{
			allButtons[4].highlight.isHighlighted = true;
		}
	}

	public void SetEnabled(bool enabled, bool lockUI)
	{
		buttonsEnabled = enabled && !lockUI;
		UILocked = lockUI;
	}

	public void SetAirport(Airport airport)
	{
		if (buttonsEnabled)
		{
			airportIconImage.texture = (airport.IsBaseAirport ? baseIcon : airportIcon);
			PlaneContainer instance = Singleton<PlaneContainer>.Instance;
			bool flag = instance.fuel > instance.fuelCapacity * 0.98f;
			bool flag2 = instance.electricity > instance.electricityStorageCapacity * 0.98f;
			allButtons[2].enabled = airport.data.baseAirport;
			allButtons[3].enabled = airport.cargoType != null && airport.cargoType.Length != 0;
			allButtons[4].enabled = airport.data.baseAirport;
			allButtons[5].enabled = false;
			allButtons[6].enabled = !buttonPrompt.activeInHierarchy || ((!flag || !flag2) && !instance.IsAccelerating);
			bool flag3 = airport.data == null || airport.data.refuelAvailable;
			refuelButtonCanvasGroup.alpha = (flag3 ? 1f : 0.7f);
			refuelButton.interactable = flag3;
			refuelTooltip.enabled = !flag3;
			title.text = airport.airportName;
			((RectTransform)title.transform).sizeDelta = new Vector2(title.preferredWidth + 3f, ((RectTransform)title.transform).sizeDelta.y);
			airportNameBackground.sizeDelta = new Vector2(title.preferredWidth + 100f, 70f);
		}
	}

	public void EditPlane()
	{
		Singleton<GameManager>.Instance.StartBuildMode(force: true);
		Singleton<CurrentGameData>.Instance.editPlanePressed = true;
	}
}
