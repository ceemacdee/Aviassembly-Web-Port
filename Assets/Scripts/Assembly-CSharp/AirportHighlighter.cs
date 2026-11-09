using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class AirportHighlighter : MonoBehaviour
{
	public TMP_Text headerText;

	public TMP_Text distanceText;

	public Vector3 airportPostition;

	public RawImage icon;

	public Texture defaultIcon;

	public Texture helicopterIcon;

	private CanvasGroup canvasGroup;

	private void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
	}

	private void Start()
	{
		if (icon != null)
		{
			Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(airportPostition);
			icon.texture = ((closestAirport.data != null && closestAirport.data.offshoreAirport) ? helicopterIcon : defaultIcon);
		}
	}

	public void SetAlpha(float alpha)
	{
		canvasGroup.alpha = alpha;
	}

	private void Update()
	{
		float num = (int)Vector3.Distance(Singleton<PlaneContainer>.Instance.transform.position, airportPostition);
		int num2 = (int)(num / 1000f);
		int num3 = Mathf.RoundToInt(num % 1000f);
		string text = "";
		if (num2 > 0)
		{
			text = text + num2 + ".";
		}
		if (num3 < 100)
		{
			text += "0";
		}
		text += num3;
		distanceText.text = text;
	}
}
