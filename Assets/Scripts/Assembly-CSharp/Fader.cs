using System.Collections;
using UnityEngine;

public class Fader : Singleton<Fader>
{
	public float fadeSpeed;

	private float currentAlpha;

	private int fadeDirection;

	private CanvasGroup canvasGroup;

	protected override void Awake()
	{
		base.Awake();
		canvasGroup = GetComponent<CanvasGroup>();
		currentAlpha = canvasGroup.alpha;
		fadeDirection = 1;
	}

	public void FadeIn()
	{
		fadeDirection = 1;
		StartCoroutine(Fade());
	}

	public void FadeOut()
	{
		fadeDirection = -1;
		StartCoroutine(Fade());
	}

	public bool FadeReady()
	{
		if ((fadeDirection == 1 && currentAlpha == 1f) || (fadeDirection == -1 && currentAlpha == 0f))
		{
			return true;
		}
		return false;
	}

	private IEnumerator Fade()
	{
		yield return null;
		while (!FadeReady())
		{
			currentAlpha += Time.unscaledDeltaTime * (float)fadeDirection * fadeSpeed;
			currentAlpha = Mathf.Clamp01(currentAlpha);
			canvasGroup.alpha = currentAlpha;
			yield return null;
		}
	}
}
