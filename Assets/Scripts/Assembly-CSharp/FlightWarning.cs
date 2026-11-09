using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FlightWarning : MonoBehaviour
{
	public TMP_Text header;

	public TMP_Text subtitle;

	public Image background;

	public Image iconBackground;

	public RawImage icon;

	public RawImage iconDropshadow;

	public Color red;

	public Color darkRed;

	public Color white;

	public Color black;

	private RectTransform rectTransform;

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
	}

	private void Update()
	{
		float num = Mathf.Max(header.preferredWidth, subtitle.preferredWidth);
		rectTransform.sizeDelta = new Vector2(num + rectTransform.sizeDelta.y + 30f, rectTransform.sizeDelta.y);
	}

	public void SetContent(string title, string subtitle, Texture icon, bool warning)
	{
		header.text = title;
		this.subtitle.text = subtitle;
		this.icon.texture = icon;
		iconDropshadow.texture = icon;
		if (warning)
		{
			background.color = red;
			iconBackground.color = darkRed;
			header.color = white;
			this.subtitle.color = white;
		}
		else
		{
			background.color = white;
			iconBackground.color = black;
			header.color = black;
			this.subtitle.color = black;
		}
	}
}
