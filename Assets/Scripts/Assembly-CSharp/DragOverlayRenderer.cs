using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragOverlayRenderer : MonoBehaviour
{
	public Shader overlayReplacement;

	public Image image;

	public TMP_Text buttonText;

	public RawImage icon;

	public Color normalColor;

	public Color selectedColor;

	public float fadeDuration;

	private float mainTexVisbility;

	private float targetVisibility;

	private float visibilityVelocity;

	private bool showOverlay;

	private Camera cam;

	private void Awake()
	{
		cam = GetComponent<Camera>();
		image.color = normalColor;
	}

	private void Update()
	{
		mainTexVisbility = Mathf.SmoothDamp(mainTexVisbility, targetVisibility, ref visibilityVelocity, fadeDuration);
		mainTexVisbility = Mathf.Clamp01(mainTexVisbility);
		if (mainTexVisbility > 0.9f && targetVisibility > 0.9f)
		{
			cam.ResetReplacementShader();
		}
		Shader.SetGlobalFloat("_DragMainTexVisibility", mainTexVisbility);
	}

	public void ToggleOverlay()
	{
		showOverlay = !showOverlay;
		image.color = (showOverlay ? selectedColor : normalColor);
		buttonText.color = (showOverlay ? Color.white : Color.black);
		icon.color = buttonText.color;
		if (showOverlay)
		{
			cam.SetReplacementShader(overlayReplacement, "Replacement");
			targetVisibility = 0f;
			mainTexVisbility = 1f;
		}
		else
		{
			targetVisibility = 1f;
		}
	}
}
