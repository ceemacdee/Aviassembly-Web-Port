using UnityEngine;

public class Tooltip : MonoBehaviour
{
	public float showDelay;

	private GameObject currentPrefab;

	private TooltipLayout tooltipLayout;

	private CanvasGroup canvasGroup;

	private float openTime;

	private void Awake()
	{
		tooltipLayout = GetComponent<TooltipLayout>();
		canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Update()
	{
		canvasGroup.alpha = ((Time.unscaledTime > openTime + showDelay) ? 1f : 0f);
	}

	public void Show()
	{
		openTime = Time.unscaledTime;
	}

	public void Init(TooltipSettings settings)
	{
		Object.Destroy(currentPrefab);
		tooltipLayout.prefabContent = null;
		tooltipLayout.targetRectTransform = settings.targetRect;
		if (settings.prefab != null)
		{
			currentPrefab = Object.Instantiate(settings.prefab);
			currentPrefab.transform.SetParent(base.transform, worldPositionStays: true);
			currentPrefab.transform.localPosition = Vector3.zero;
			currentPrefab.transform.rotation = Quaternion.identity;
			currentPrefab.transform.localScale = Vector3.one;
			tooltipLayout.prefabContent = (RectTransform)currentPrefab.transform;
		}
		canvasGroup.alpha = 0f;
		tooltipLayout.Update();
	}
}
