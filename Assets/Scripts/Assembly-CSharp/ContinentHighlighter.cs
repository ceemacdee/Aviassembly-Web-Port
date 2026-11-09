using TMPro;
using UnityEngine;

public class ContinentHighlighter : MonoBehaviour
{
	public TMP_Text continentText;

	private AirportHighlighter highligter;

	private void Awake()
	{
		highligter = GetComponent<AirportHighlighter>();
	}

	private void Update()
	{
		continentText.text = Singleton<ContinentManager>.Instance.GetClosestContinent(highligter.airportPostition).continentType.continentName;
	}
}
