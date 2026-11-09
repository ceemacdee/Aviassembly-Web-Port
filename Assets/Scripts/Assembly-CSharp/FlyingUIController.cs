using System.Collections.Generic;
using UnityEngine;

public class FlyingUIController : MonoBehaviour
{
	private class WindowData
	{
		public CanvasGroup canvasGroup;

		public float targetAlpha;

		public float alphaVelocity;

		public WindowData(CanvasGroup group)
		{
			canvasGroup = group;
		}
	}

	[HideInInspector]
	public bool UIOpen;

	public CanvasGroup[] windows;

	[Space(10f)]
	public CanvasGroup HUD;

	public CanvasGroup Map;

	public CanvasGroup CargoUI;

	public CanvasGroup Backround;

	public CanvasGroup Buttons;

	public CargoInventoryUI cargoInventoryUI;

	public float fadeSpeed;

	public Map map;

	private Dictionary<CanvasGroup, WindowData> windowData = new Dictionary<CanvasGroup, WindowData>();

	private float HUDTargetAlpha;

	private float HUDTargetAlphaVelocity;

	private float backgroundTargetAlpha;

	private float backgroundAlphaVelocity;

	private float buttonsAlhpaVeloctity;

	private int lastLockfFrame;

	public bool lockUI { get; private set; }

	private void Awake()
	{
		float num = fadeSpeed;
		fadeSpeed = 0f;
		CloseAllWindows();
		fadeSpeed = num;
		for (int i = 0; i < windows.Length; i++)
		{
			windowData.Add(windows[i], new WindowData(windows[i]));
		}
	}

	public void ToggleCargoUI(bool airport)
	{
		cargoInventoryUI.OpenAirportInventory(airport);
		ToggleWindow(CargoUI);
	}

	public void ToggleWindow(CanvasGroup window)
	{
		ToggleWindow(window, enableBackround: true);
	}

	public void ToggleWindow(CanvasGroup window, bool enableBackround)
	{
		if (!lockUI)
		{
			WindowData windowData = this.windowData[window];
			bool flag = windowData.targetAlpha > 0.1f;
			CloseAllWindows();
			windowData.targetAlpha = ((!flag) ? 1 : 0);
			HUDTargetAlpha = 1f - windowData.targetAlpha;
			backgroundTargetAlpha = windowData.targetAlpha;
			if (!enableBackround)
			{
				backgroundTargetAlpha = 0f;
			}
		}
	}

	public void OpenWindow(CanvasGroup window, bool enableBlackBackground)
	{
		lockUI = false;
		if (!(windowData[window].targetAlpha > 0.1f))
		{
			ToggleWindow(window, enableBlackBackground);
		}
	}

	public void CloseWindow(CanvasGroup window)
	{
		if (windowData[window].targetAlpha > 0.1f)
		{
			ToggleWindow(window);
		}
	}

	public void CloseAllWindows()
	{
		if (lockUI)
		{
			return;
		}
		foreach (KeyValuePair<CanvasGroup, WindowData> windowDatum in windowData)
		{
			windowDatum.Value.targetAlpha = 0f;
		}
		HUDTargetAlpha = 1f;
		backgroundTargetAlpha = 0f;
	}

	public void LockUI()
	{
		lockUI = true;
		lastLockfFrame = Time.frameCount;
	}

	private void LateUpdate()
	{
		if (Time.frameCount > lastLockfFrame)
		{
			lockUI = false;
		}
	}

	private void Update()
	{
		Singleton<GameManager>.Instance.modalsOpen = false;
		UIOpen = false;
		foreach (KeyValuePair<CanvasGroup, WindowData> windowDatum in windowData)
		{
			CanvasGroup canvasGroup = windowDatum.Value.canvasGroup;
			float targetAlpha = windowDatum.Value.targetAlpha;
			canvasGroup.alpha = Mathf.SmoothDamp(canvasGroup.alpha, windowDatum.Value.targetAlpha, ref windowDatum.Value.alphaVelocity, fadeSpeed, float.MaxValue, Time.unscaledDeltaTime);
			canvasGroup.gameObject.SetActive(canvasGroup.alpha > 0.05f);
			if ((double)targetAlpha > 0.9)
			{
				Singleton<GameManager>.Instance.modalsOpen = true;
				UIOpen = true;
			}
		}
		if (lockUI)
		{
			Singleton<GameManager>.Instance.modalsOpen = false;
			HUDTargetAlpha = 0f;
		}
		HUD.gameObject.SetActive(value: true);
		HUD.alpha = Mathf.SmoothDamp(HUD.alpha, HUDTargetAlpha, ref HUDTargetAlphaVelocity, fadeSpeed, float.MaxValue, Time.unscaledDeltaTime);
		Buttons.alpha = Mathf.SmoothDamp(Buttons.alpha, (!lockUI) ? 1 : 0, ref buttonsAlhpaVeloctity, fadeSpeed, float.MaxValue, Time.unscaledDeltaTime);
		if (HUD.alpha < 0.05f)
		{
			HUD.gameObject.SetActive(value: false);
		}
		Backround.alpha = Mathf.SmoothDamp(Backround.alpha, backgroundTargetAlpha, ref backgroundAlphaVelocity, fadeSpeed, float.MaxValue, Time.unscaledDeltaTime);
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CloseAllWindows();
		}
		if (!Singleton<MouseInput>.Instance.PointerIsOverUI && MouseInput.GetMouseButtonDown(0) && !lockUI)
		{
			CloseAllWindows();
		}
		if (Singleton<InputManager>.Instance.openMapAction.WasPressedThisFrame() && Singleton<InputManager>.Instance.currentInputBlocker == null)
		{
			ToggleWindow(Map);
			map.OpenMap();
		}
	}
}
