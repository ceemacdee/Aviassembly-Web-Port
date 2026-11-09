using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResearchButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	[HideInInspector]
	public ResearchPanel researchPanel;

	public TitleAnimator titleAnimator;

	public GameObject prefab;

	public RawImage icon;

	public RawImage shadow;

	public Image background;

	public ResearchCostDisplay researchCostDisplay;

	public GameObject hider;

	public RawImage researchIcon;

	public List<RawImage> connectionLines = new List<RawImage>();

	public List<ResearchButton> parents = new List<ResearchButton>();

	public Color selectedColor;

	public Color deselectedColor;

	public Color redColor;

	private Button button;

	public BuildingPart part { get; private set; }

	private void Start()
	{
		part = prefab.GetComponent<BuildingPart>();
		icon.texture = Singleton<IconGenerator>.Instance.GenerateIcon(prefab, part.framing);
		shadow.texture = icon.texture;
		deselectedColor = background.color;
		button = GetComponent<Button>();
		researchCostDisplay.Init(Singleton<ResearchManager>.Instance.GetResearchCost(part.gameObject));
		Deselect();
	}

	private void Update()
	{
		bool flag = Singleton<ResearchManager>.Instance.IsPartUnlocked(part);
		bool flag2 = true;
		for (int i = 0; i < parents.Count; i++)
		{
			if (!Singleton<ResearchManager>.Instance.IsPartUnlocked(parents[i].part))
			{
				flag2 = false;
			}
		}
		hider.SetActive(!flag);
		researchCostDisplay.gameObject.SetActive(flag2 && !flag);
		button.interactable = flag2;
	}

	public void Select()
	{
		researchPanel.DeselectAllButtons();
		background.color = selectedColor;
		researchPanel.SelectButton(this);
		SelectLines();
	}

	public void SelectLines()
	{
		for (int i = 0; i < connectionLines.Count; i++)
		{
			connectionLines[i].color = selectedColor;
		}
		for (int j = 0; j < parents.Count; j++)
		{
			parents[j].SelectLines();
		}
	}

	public void Deselect()
	{
		background.color = deselectedColor;
		for (int i = 0; i < connectionLines.Count; i++)
		{
			connectionLines[i].color = deselectedColor;
		}
	}

	public void ResearchPart()
	{
		Singleton<ResearchManager>.Instance.UnlockPart(part);
	}

	public string SplitCamelCase(string input)
	{
		return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		BuildingPartTooltip.lastHoveredPart = new PartUIData
		{
			partName = ((part.partName == null || part.partName == "") ? SplitCamelCase(part.name) : part.partName),
			part = part,
			icon = icon.texture,
			showPrice = false
		};
		BuildingPartTooltip.researchButton = true;
	}
}
