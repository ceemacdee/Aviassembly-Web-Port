using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CargoInventorySlot : MonoBehaviour
{
	public RawImage icon;

	public TMP_Text text;

	private int amount;

	public void Init(Texture icon, float value)
	{
		base.gameObject.SetActive(value > 0.1f);
		this.icon.texture = icon;
		text.text = Mathf.RoundToInt(value).ToString();
		if ((int)value != amount)
		{
			base.transform.localScale = Vector3.one * 1.25f;
		}
		amount = (int)value;
	}

	public void Update()
	{
		base.transform.localScale -= Vector3.one * Time.deltaTime * 2f;
		if (base.transform.localScale.x < 1f)
		{
			base.transform.localScale = Vector3.one;
		}
	}
}
