using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PartButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private PartPlacer placer;

	private GameObject prefab;

	private BuildingPart part;

	private PartUIData data;

	private CustomButton buttonComponent;

	private TooltipTrigger tooltipTrigger;

	public GameObject unlockedTooltip;

	public GameObject lockedTooltip;

	public RawImage icon;

	public Image background;

	public GameObject lockObject;

	public Color normalColor;

	public Color hoverColor;

	private float price;

	private Texture iconTexture;

	private bool initialized;

	private bool buttonDisabled;

	public void Init(PartPlacer partPlacer, GameObject prefab, IconGenerator iconGenerator)
	{
		placer = partPlacer;
		this.prefab = prefab;
		part = prefab.GetComponent<BuildingPart>();
		buttonComponent = GetComponent<CustomButton>();
		tooltipTrigger = GetComponent<TooltipTrigger>();
		string partName = ((part.partName == "") ? SplitCamelCase(prefab.name) : part.partName);
		price = part.price;
		iconTexture = iconGenerator.GenerateIcon(prefab, part.framing);
		icon.texture = iconTexture;
		data = new PartUIData();
		data.part = part;
		data.icon = icon.texture;
		data.partName = partName;
		data.showPrice = true;
		initialized = true;
	}

	private void OnEnable()
	{
		if (initialized)
		{
			Update();
		}
	}

	private void Update()
	{
		buttonDisabled = placer.partContainer.GetBasePartCount() == 0 && !part.basePart && (placer.currentMovingPart == null || !placer.currentMovingPart.isBasePart);
		buttonComponent.interactable = !buttonDisabled;
		if (lockObject.activeInHierarchy)
		{
			buttonComponent.interactable = false;
		}
		tooltipTrigger.enabled = !buttonDisabled;
		bool flag = !Singleton<ResearchManager>.Instance.IsPartUnlocked(part);
		lockObject.SetActive(flag && !buttonDisabled);
		icon.color = Color.white;
		if (flag || !Singleton<MoneyManager>.Instance.HasEnoughMoney(price))
		{
			icon.color = new Color(0.9f, 0.9f, 0.9f, 0.4f);
		}
		if (buttonDisabled)
		{
			icon.color = Color.black * 0.75f;
		}
		background.color = (buttonComponent.hover ? hoverColor : normalColor);
		tooltipTrigger.settings.prefab = (flag ? lockedTooltip : unlockedTooltip);
	}

	public void Click()
	{
		if (Singleton<MoneyManager>.Instance.HasEnoughMoney(price))
		{
			placer.InstantiatePart(prefab);
		}
	}

	public string SplitCamelCase(string input)
	{
		return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		BuildingPartTooltip.lastHoveredPart = data;
		BuildingPartTooltip.researchButton = false;
		if (!buttonDisabled)
		{
			Singleton<RotatingIconGeneration>.Instance.SetObject(part.gameObject, part.framing);
			icon.texture = Singleton<RotatingIconGeneration>.Instance.icon;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		icon.texture = iconTexture;
	}
}
