using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaneLoadLocalToggleButton : MonoBehaviour
{
	public Image background;

	public Shadow shadow;

	public GameObject hider;

	public Color defaultColor;

	public Color selectedColor;

	public Color shadowColor;

	public Color selectedShadowColor;

	private TMP_Text text;

	private Button button;

	private void Awake()
	{
		button = GetComponent<Button>();
		text = GetComponentInChildren<TMP_Text>();
	}

	public void SetButtonEnabled(bool value)
	{
		button.interactable = value;
		hider.SetActive(!value);
	}

	public void SetSelected(bool selected)
	{
		if (!selected)
		{
			background.color = defaultColor;
			text.color = Color.black;
			shadow.effectColor = shadowColor;
		}
		else
		{
			background.color = selectedColor;
			text.color = Color.white;
			shadow.effectColor = selectedShadowColor;
		}
	}
}
