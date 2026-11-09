using UnityEngine;

public class Credits : MonoBehaviour
{
	public CanvasGroup menuCanvas;

	public CanvasGroup creditsCanvas;

	private float menuCanvasTargetAlpha;

	private float menuCanvasAlphaVeloctiy;

	private void Awake()
	{
		menuCanvasTargetAlpha = 1f;
	}

	public void OpenCredits()
	{
		creditsCanvas.gameObject.SetActive(value: true);
		menuCanvasTargetAlpha = 0f;
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0) && menuCanvasTargetAlpha < 0.5f)
		{
			menuCanvasTargetAlpha = 1f;
			menuCanvas.gameObject.SetActive(value: true);
		}
		if ((double)menuCanvasTargetAlpha > 0.5 && menuCanvas.alpha > 0.96f)
		{
			creditsCanvas.gameObject.SetActive(value: false);
		}
		if ((double)menuCanvasTargetAlpha < 0.5 && menuCanvas.alpha < 0.04f)
		{
			menuCanvas.gameObject.SetActive(value: false);
		}
		menuCanvas.alpha = Mathf.SmoothDamp(menuCanvas.alpha, menuCanvasTargetAlpha, ref menuCanvasAlphaVeloctiy, 0.1f);
		creditsCanvas.alpha = 1f - menuCanvas.alpha;
	}
}
