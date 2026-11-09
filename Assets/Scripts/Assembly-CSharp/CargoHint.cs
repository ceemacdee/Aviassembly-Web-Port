using UnityEngine;

public class CargoHint : MonoBehaviour
{
	public float bounceAmount;

	public float bounceSpeed;

	public PlaneStats stats;

	private Vector3 startPos;

	private CanvasGroup canvasGroup;

	private void Start()
	{
		startPos = ((RectTransform)base.transform).anchoredPosition;
		canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Update()
	{
		((RectTransform)base.transform).anchoredPosition = startPos + Vector3.down * (Mathf.Sin(Time.unscaledTime * bounceSpeed) + 1f) * 0.5f * bounceAmount;
		float num = Singleton<PlaneStorage>.Instance.GetPlaneCost() + Singleton<MoneyManager>.Instance.money;
		float cargoSpace = stats.cargoSpace;
		bool flag = Singleton<MoneyManager>.Instance.money > 300f;
		canvasGroup.alpha = ((num > 700f && cargoSpace < 45f && num < 1500f && !flag) ? 1f : 0f);
	}
}
