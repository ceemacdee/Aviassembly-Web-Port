using TMPro;
using UnityEngine;

public class KeyUI : MonoBehaviour
{
	public RectTransform keyBackground;

	public TMP_Text keyText;

	public KeyIcon icon;

	public float offset;

	private void LateUpdate()
	{
		if (keyText.gameObject.activeInHierarchy)
		{
			keyBackground.sizeDelta = new Vector2(((RectTransform)keyText.transform).sizeDelta.x, keyBackground.sizeDelta.y) + new Vector2(offset, 0f);
		}
		else
		{
			keyBackground.sizeDelta = new Vector2(70f, 95f);
		}
		keyBackground.sizeDelta = new Vector2(Mathf.Max(keyBackground.sizeDelta.x, keyBackground.sizeDelta.y), keyBackground.sizeDelta.y);
	}

	public void SetKey(string keyPath)
	{
		bool flag = keyPath != "";
		base.gameObject.SetActive(flag);
		if (flag)
		{
			icon.SetValue(keyPath);
		}
	}
}
