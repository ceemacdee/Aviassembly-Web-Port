using TMPro;
using UnityEngine;

public class AirportMoneyIndicator : MonoBehaviour
{
	public TMP_Text moneyIndicator;

	public TMP_Text scrapIndicator;

	public TMP_Text advancedScrapIndicator;

	public RectTransform moneyBackground;

	private CanvasGroup canvasGroup;

	private float alphaVelocity;

	private float targetAlpha;

	private void Start()
	{
		canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Update()
	{
		moneyIndicator.text = ((int)Singleton<MoneyManager>.Instance.money).ToString();
		moneyBackground.sizeDelta = new Vector2(115f + moneyIndicator.preferredWidth, moneyBackground.sizeDelta.y);
		scrapIndicator.text = Singleton<ResearchManager>.Instance.researchPoints.ToString();
		advancedScrapIndicator.text = Singleton<ResearchManager>.Instance.advancedResearchPoints.ToString();
		canvasGroup.alpha = Mathf.SmoothDamp(canvasGroup.alpha, targetAlpha, ref alphaVelocity, 0.1f);
		if (canvasGroup.alpha < 0.02f)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	public void SetEnable(bool value, bool lockUI)
	{
		if (lockUI)
		{
			canvasGroup.alpha = 0f;
			return;
		}
		if (Singleton<GameManager>.Instance.gameModeData.creativeMode)
		{
			value = false;
		}
		targetAlpha = (value ? 1f : 0f);
		if (value)
		{
			base.gameObject.SetActive(value: true);
		}
	}
}
