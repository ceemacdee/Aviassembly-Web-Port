using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Outline))]
public class ButtonHighlight : MonoBehaviour
{
	public bool isHighlighted;

	private Outline outline;

	private void Start()
	{
		outline = GetComponent<Outline>();
	}

	private void LateUpdate()
	{
		outline.enabled = isHighlighted;
		if (isHighlighted)
		{
			outline.effectDistance = Vector2.one * (Mathf.Sin(Time.time * 10f + MathF.PI) + 1f) * 1.5f;
			float num = Mathf.Min(base.transform.localScale.x + 0.04f, 1f + Mathf.Sin(Time.time * 10f) * 0.04f);
			base.transform.localScale = Vector3.one * num;
		}
		else
		{
			float num2 = Mathf.Min(base.transform.localScale.x, 1f);
			base.transform.localScale = Vector3.one * num2;
		}
	}
}
