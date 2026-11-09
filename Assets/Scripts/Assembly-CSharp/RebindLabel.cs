using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindLabel : MonoBehaviour
{
	public TMP_Text text;

	public RawImage icon;

	[Tooltip("Shown when nothing is bound.")]
	public string placeholderText = "-";

	private Button button;

	public Button Button
	{
		get
		{
			if (!(button != null))
			{
				return button = GetComponent<Button>();
			}
			return button;
		}
	}

	public void SetLabelValue(string keyPath)
	{
		if (string.IsNullOrEmpty(keyPath))
		{
			SetPlaceholderLabel();
		}
		else
		{
			Show(text, icon, keyPath);
		}
	}

	public void SetPlaceholderLabel()
	{
		ShowText(text, icon, placeholderText);
	}

	public void SetText(string value)
	{
		ShowText(text, icon, value);
	}

	public void SetVisible(bool visible)
	{
		if (base.gameObject.activeSelf != visible)
		{
			base.gameObject.SetActive(visible);
		}
	}

	public static void Show(TMP_Text text, RawImage icon, string keyPath)
	{
		if (string.IsNullOrEmpty(keyPath))
		{
			ShowText(text, icon, "-");
			return;
		}
		Texture texture = ((icon != null) ? Singleton<ControllerIconManager>.Instance.GetIcon(keyPath) : null);
		if (texture == null)
		{
			ShowText(text, icon, InputUtility.DisplayName(keyPath));
			return;
		}
		text.gameObject.SetActive(value: false);
		icon.gameObject.SetActive(value: true);
		icon.texture = texture;
		AspectRatioFitter component = icon.GetComponent<AspectRatioFitter>();
		if (component != null && texture.height > 0)
		{
			component.aspectRatio = (float)texture.width / (float)texture.height;
		}
	}

	public static void ShowText(TMP_Text text, RawImage icon, string value)
	{
		text.gameObject.SetActive(value: true);
		if (icon != null)
		{
			icon.gameObject.SetActive(value: false);
		}
		text.text = value;
	}
}
