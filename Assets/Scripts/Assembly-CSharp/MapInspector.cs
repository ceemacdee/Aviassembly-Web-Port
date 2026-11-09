using UnityEngine;

public class MapInspector : MonoBehaviour
{
	private CanvasGroup canvasGroup;

	public ContractPanel contractPanel;

	public bool hidden;

	public int showCount;

	private float t;

	private float tVelo;

	private void Start()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		canvasGroup.alpha = 1f;
		((RectTransform)base.transform).anchoredPosition = new Vector3(50f, 400f, 0f);
		Disable();
	}

	private void Update()
	{
		RectTransform obj = (RectTransform)base.transform;
		t = Mathf.SmoothDamp(t, (!hidden) ? 1 : 0, ref tVelo, 0.035f);
		obj.anchoredPosition = Vector3.Lerp(new Vector3(-50f, 400f, 0f), new Vector3(445f, 400f, 0f), t);
	}

	public void Disable()
	{
		hidden = true;
	}

	public void SelectAirport(Airport airport)
	{
		showCount++;
		contractPanel.SelectAirport(airport);
		hidden = false;
	}
}
