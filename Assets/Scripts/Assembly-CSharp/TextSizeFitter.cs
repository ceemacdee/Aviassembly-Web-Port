using TMPro;
using UnityEngine;

public class TextSizeFitter : MonoBehaviour
{
	public TMP_Text text;

	public float offset;

	private void Update()
	{
		((RectTransform)base.transform).sizeDelta = new Vector2(CustomMath.SnapToIncrement(offset + text.preferredWidth, 10f) + 5f, ((RectTransform)base.transform).sizeDelta.y);
	}
}
