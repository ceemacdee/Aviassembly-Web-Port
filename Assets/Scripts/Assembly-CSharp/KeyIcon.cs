using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyIcon : MonoBehaviour
{
	public TMP_Text text;

	public RawImage icon;

	private string currentValue;

	public void SetValue(string keyPath)
	{
		if (!(currentValue == keyPath))
		{
			Texture texture = Singleton<ControllerIconManager>.Instance.GetIcon(keyPath);
			if (texture != null)
			{
				text.gameObject.SetActive(value: false);
				icon.gameObject.SetActive(value: true);
				icon.texture = texture;
			}
			if (texture == null)
			{
				text.gameObject.SetActive(value: true);
				icon.gameObject.SetActive(value: false);
				text.text = InputUtility.DisplayName(keyPath);
			}
			currentValue = keyPath;
		}
	}

	public void Wait()
	{
		text.gameObject.SetActive(value: true);
		icon.gameObject.SetActive(value: false);
		text.text = "-";
		currentValue = "null";
	}
}
