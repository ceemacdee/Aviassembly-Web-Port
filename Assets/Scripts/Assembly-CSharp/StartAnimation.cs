using UnityEngine;
using UnityEngine.UI;

public class StartAnimation : MonoBehaviour
{
	public bool playAnimation;

	private float amount;

	public float speed;

	public float defaultDelay;

	[Space(10f)]
	public Camera cam;

	public Camera planeCamera;

	public RawImage panel;

	public RawImage planeOverlay;

	public MenuBird bird;

	private float startDelay;

	private float t;

	private void Start()
	{
		startDelay = defaultDelay;
		t = 1f;
		cam.fieldOfView = 0f;
		bird.flight = 0f;
		if (Singleton<GameManager>.Instance.menuIntroPlayed || !playAnimation)
		{
			t = 0f;
			startDelay = 0f;
			panel.color = Color.clear;
		}
		Singleton<GameManager>.Instance.menuIntroPlayed = true;
	}

	private void Update()
	{
		startDelay -= Time.deltaTime;
		if (!(startDelay > 0f))
		{
			t -= Time.deltaTime * speed;
			t = Mathf.Clamp01(t);
			amount = 1f - Mathf.Pow(t, 3f);
			Color color = panel.color;
			color.a = Mathf.Pow(1f - amount, 5f);
			panel.color = color;
			Color color2 = planeOverlay.color;
			float num = Mathf.Clamp01(amount - 0.5f) * 2f;
			color2.a = Mathf.Pow(1f - num, 5f);
			planeOverlay.color = color2;
			cam.fieldOfView = Mathf.Lerp(0f, 60f, amount);
			planeCamera.fieldOfView = cam.fieldOfView;
			bird.flight = Mathf.Pow(amount, 3f);
		}
	}
}
