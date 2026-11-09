using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CargoTooltipElement : MonoBehaviour
{
	public TMP_Text value;

	public RawImage icon;

	public void Init(Texture icon, float value)
	{
		this.value.text = Mathf.RoundToInt(value).ToString();
		this.icon.texture = icon;
	}

	public void SetValue(float value)
	{
		this.value.text = Mathf.RoundToInt(value).ToString();
	}
}
