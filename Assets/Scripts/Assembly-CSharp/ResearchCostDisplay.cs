using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResearchCostDisplay : MonoBehaviour
{
	public ResearchCost researchCost;

	public Color redColor;

	public Color blackColor;

	public TMP_Text scrapCostText;

	public TMP_Text advancedScrapCostText;

	public RawImage scrapIcon;

	public RawImage advancedScrapIcon;

	public GameObject scrapCost;

	public GameObject advancedScrapCost;

	private TitleAnimator titleAnimator;

	public void Init(ResearchCost researchCost)
	{
		this.researchCost = researchCost;
		titleAnimator = GetComponent<TitleAnimator>();
		scrapCost.gameObject.SetActive(researchCost.scrap > 0);
		advancedScrapCost.gameObject.SetActive(researchCost.advancedScrap > 0);
		scrapCostText.text = researchCost.scrap.ToString();
		advancedScrapCostText.text = researchCost.advancedScrap.ToString();
	}

	private void Update()
	{
		ResearchCost currentScrap = Singleton<ResearchManager>.Instance.GetCurrentScrap();
		scrapCostText.color = ((researchCost.scrap <= currentScrap.scrap) ? blackColor : redColor);
		advancedScrapCostText.color = ((researchCost.advancedScrap <= currentScrap.advancedScrap) ? blackColor : redColor);
		scrapIcon.color = scrapCostText.color;
		advancedScrapIcon.color = advancedScrapCostText.color;
		titleAnimator.enabled = researchCost.scrap <= currentScrap.scrap && researchCost.advancedScrap <= currentScrap.advancedScrap;
		((RectTransform)base.transform).sizeDelta = new Vector2(50f + Mathf.Max(scrapCostText.preferredWidth, advancedScrapCostText.preferredWidth), 83f);
		((RectTransform)base.transform).anchoredPosition = Vector3.zero;
	}
}
