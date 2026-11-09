using TMPro;
using UnityEngine;

public class PartStatUI : MonoBehaviour
{
	public TMP_Text statNameText;

	public TMP_Text statValueText;

	public void SetValue(string statName, string statValue)
	{
		statNameText.text = statName;
		statValueText.text = statValue;
	}
}
