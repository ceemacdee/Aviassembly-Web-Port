using TMPro;
using UnityEngine;

public class ResearchPanel : MonoBehaviour
{
	public TMP_Text researchPoints;

	public TMP_Text advancedResearchPoints;

	public CanvasGroup researchButtonCanvasGroup;

	public TitleAnimator reseachButtonAnimator;

	private ResearchButton selectedButton;

	private ResearchButton[] buttons;

	private void Start()
	{
		buttons = GetComponentsInChildren<ResearchButton>();
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].researchPanel = this;
		}
	}

	public void SelectButton(ResearchButton selectedButton)
	{
		this.selectedButton = selectedButton;
	}

	private void Update()
	{
		researchPoints.text = Singleton<ResearchManager>.Instance.researchPoints.ToString();
		advancedResearchPoints.text = Singleton<ResearchManager>.Instance.advancedResearchPoints.ToString();
		ResearchManager instance = Singleton<ResearchManager>.Instance;
		ResearchCost researchCost = ((selectedButton == null) ? new ResearchCost(0, 0) : Singleton<ResearchManager>.Instance.GetResearchCost(selectedButton.part.gameObject));
		bool flag = selectedButton != null && instance.researchPoints >= researchCost.scrap && instance.advancedResearchPoints >= researchCost.advancedScrap && !instance.unlockedParts.Contains(selectedButton.part);
		researchButtonCanvasGroup.alpha = (flag ? 1f : 0.5f);
		reseachButtonAnimator.enabled = flag;
	}

	public void Unlock()
	{
		if (!(selectedButton == null))
		{
			Singleton<ResearchManager>.Instance.UnlockPart(selectedButton.part);
		}
	}

	public void DeselectAllButtons()
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].Deselect();
		}
	}
}
