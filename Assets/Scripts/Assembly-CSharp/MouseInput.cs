using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

public class MouseInput : Singleton<MouseInput>
{
	public const float mouseSensitivity = 7.5f;

	private const float ReferenceHeight = 1080f;

	public Texture2D normalCursor;

	public Texture2D brushCursor;

	public Texture2D colorPickCursor;

	[Header("Virtual Cursor")]
	public PlayerInput playerInput;

	public RectTransform virtualCursorTransform;

	public RectTransform canvas;

	public float cursorSpeed;

	public float mouseSlowdown;

	public GameObject deviceChangedPopup;

	[Space(25f)]
	public InputActionReference moveCursorAction;

	public InputActionReference zoomAction;

	public InputActionReference leftButtonAction;

	public InputActionReference rightButtonAction;

	public InputActionReference slowCursorAction;

	private bool prevLeftMouseState;

	private bool prevRightMouseState;

	private float t;

	private string prevControlSceme;

	private const string gamepadSceme = "gamepad";

	private const string mouseSceme = "keyboard";

	private const string joystickSceme = "joystick";

	private VirtualCursorSettings settings = new VirtualCursorSettings();

	private Mouse virtualMouse;

	private Mouse current;

	public static Mouse currentMouse;

	public bool PointerIsOverUI { get; private set; }

	private bool UsingVirtualCursor
	{
		get
		{
			if (!(prevControlSceme == "gamepad"))
			{
				return prevControlSceme == "joystick";
			}
			return true;
		}
	}

	public static Vector3 GetMousePosition()
	{
		return currentMouse.position.ReadValue();
	}

	public static Vector2 GetMouseDelta()
	{
		return currentMouse.delta.ReadValue() * 0.075f;
	}

	public static bool GetMouseButton(int button)
	{
		switch (button)
		{
		case 0:
			if (!currentMouse.leftButton.isPressed)
			{
				return Input.GetMouseButton(0);
			}
			return true;
		case 1:
			if (!currentMouse.rightButton.isPressed)
			{
				return Input.GetMouseButton(1);
			}
			return true;
		default:
			return false;
		}
	}

	public static bool GetMouseButtonDown(int button)
	{
		switch (button)
		{
		case 0:
			if (!currentMouse.leftButton.wasPressedThisFrame)
			{
				return Input.GetMouseButtonDown(0);
			}
			return true;
		case 1:
			if (!currentMouse.rightButton.wasPressedThisFrame)
			{
				return Input.GetMouseButtonDown(1);
			}
			return true;
		default:
			return false;
		}
	}

	public static bool GetMouseButtonUp(int button)
	{
		switch (button)
		{
		case 0:
			if (!currentMouse.leftButton.wasReleasedThisFrame)
			{
				return Input.GetMouseButtonUp(0);
			}
			return true;
		case 1:
			if (!currentMouse.rightButton.wasReleasedThisFrame)
			{
				return Input.GetMouseButtonUp(1);
			}
			return true;
		default:
			return false;
		}
	}

	public static float GetDeltaScroll()
	{
		return currentMouse.scroll.ReadValue().y;
	}

	protected override void Awake()
	{
		base.Awake();
		current = Mouse.current;
		currentMouse = Mouse.current;
		SetCursorActionsEnabled(enabled: true);
		cursorSpeed = 1000f;
		mouseSlowdown = 0.5f;
		settings.LoadKeybinds();
		settings.ApplySettings(this);
	}

	private void SetCursorActionsEnabled(bool enabled)
	{
		InputActionReference[] array = new InputActionReference[5] { moveCursorAction, zoomAction, leftButtonAction, rightButtonAction, slowCursorAction };
		foreach (InputActionReference inputActionReference in array)
		{
			if (!(inputActionReference == null))
			{
				if (enabled)
				{
					inputActionReference.action.Enable();
				}
				else
				{
					inputActionReference.action.Disable();
				}
			}
		}
	}

	public void SaveSettings()
	{
		settings.GetCurrentSettings(this);
		settings.SaveKeybinds();
	}

	private void OnEnable()
	{
		if (virtualMouse == null)
		{
			virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
		}
		else if (!virtualMouse.added)
		{
			InputSystem.AddDevice(virtualMouse);
		}
		InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);
		if (virtualCursorTransform != null)
		{
			Vector2 anchoredPosition = virtualCursorTransform.anchoredPosition;
			InputState.Change(virtualMouse.position, anchoredPosition);
		}
		InputSystem.onAfterUpdate += UpdateMotion;
		playerInput.onControlsChanged += OnControlsChanged;
		virtualCursorTransform.gameObject.SetActive(value: false);
		Cursor.visible = true;
		prevControlSceme = "keyboard";
		currentMouse = (Mouse)InputSystem.GetDevice("Mouse");
	}

	private void OnDisable()
	{
		InputSystem.onAfterUpdate -= UpdateMotion;
		InputSystem.RemoveDevice(virtualMouse);
	}

	private void OnControlsChanged(PlayerInput input)
	{
		if (input.currentControlScheme == "keyboard" && prevControlSceme != "keyboard")
		{
			virtualCursorTransform.gameObject.SetActive(value: false);
			Cursor.visible = true;
			current.WarpCursorPosition(virtualMouse.position.ReadValue());
			prevControlSceme = "keyboard";
			currentMouse = (Mouse)InputSystem.GetDevice("Mouse");
			t = 3f;
		}
		string currentControlScheme = input.currentControlScheme;
		if ((currentControlScheme == "gamepad" || currentControlScheme == "joystick") && prevControlSceme != currentControlScheme)
		{
			virtualCursorTransform.gameObject.SetActive(value: true);
			Cursor.visible = false;
			InputState.Change(virtualMouse.position, current.position.ReadValue());
			AnchorCursor(current.position.ReadValue());
			prevControlSceme = currentControlScheme;
			currentMouse = (Mouse)InputSystem.GetDevice("VirtualMouse");
			t = 3f;
		}
	}

	private void UpdateMotion()
	{
		if (virtualMouse != null && UsingVirtualCursor)
		{
			float num = (slowCursorAction.action.IsPressed() ? mouseSlowdown : 1f);
			Vector2 value = moveCursorAction.action.ReadValue<Vector2>();
			float num2 = (float)Screen.height / 1080f;
			value = CustomMath.Pow(value, 5f) * cursorSpeed * num2 * Time.unscaledDeltaTime * num;
			Vector2 vector = virtualMouse.position.ReadValue() + value;
			vector.x = Mathf.Clamp(vector.x, 0f, Screen.width);
			vector.y = Mathf.Clamp(vector.y, 0f, Screen.height);
			InputState.Change(virtualMouse.position, vector);
			InputState.Change(virtualMouse.delta, value);
			float y = zoomAction.action.ReadValue<float>() * 0.2f;
			InputState.Change(virtualMouse.scroll, new Vector2(0f, y));
			bool flag = leftButtonAction.action.IsPressed();
			bool flag2 = rightButtonAction.action.IsPressed();
			if (prevLeftMouseState != flag || prevRightMouseState != flag2)
			{
				virtualMouse.CopyState<MouseState>(out var state);
				state.WithButton(MouseButton.Right, flag2);
				state.WithButton(MouseButton.Left, flag);
				InputState.Change(virtualMouse, state);
				prevLeftMouseState = flag;
				prevRightMouseState = flag2;
			}
			AnchorCursor(vector);
		}
	}

	private void AnchorCursor(Vector2 position)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, position, null, out var localPoint);
		virtualCursorTransform.anchoredPosition = localPoint;
	}

	public bool GetPointerIsOverUI(List<GameObject> ignoreObjects = null)
	{
		bool flag = EventSystem.current.IsPointerOverGameObject();
		if (!flag)
		{
			return flag;
		}
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = GetMousePosition();
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		if (list.Count > 0)
		{
			foreach (RaycastResult item in list)
			{
				if (item.gameObject.tag == "IgnorePointer")
				{
					flag = false;
				}
				if (ignoreObjects != null && ignoreObjects.Contains(item.gameObject))
				{
					flag = false;
				}
			}
		}
		return flag;
	}

	private void Update()
	{
		PointerIsOverUI = GetPointerIsOverUI();
		bool flag = false;
		if (GameManager.gameMode == GameMode.Flying && !Singleton<GameManager>.Instance.inMenu && Singleton<PlaneContainer>.Instance != null)
		{
			flag = Singleton<PlaneContainer>.Instance.GetVelocityMagintude() > 25f && !Singleton<PlaneContainer>.Instance.GetComponent<PlaneController>().Exploded;
		}
		virtualCursorTransform.gameObject.SetActive(UsingVirtualCursor && (!flag || Time.timeScale < 0.5f));
		t -= Time.unscaledDeltaTime;
		deviceChangedPopup.gameObject.SetActive(t > 0f);
	}

	public void SetCursor(CursorTypes type)
	{
		switch (type)
		{
		case CursorTypes.Paint:
			Cursor.SetCursor(brushCursor, Vector2.zero, CursorMode.Auto);
			break;
		case CursorTypes.Normal:
			Cursor.SetCursor(normalCursor, Vector2.zero, CursorMode.Auto);
			break;
		case CursorTypes.ColorPick:
			Cursor.SetCursor(colorPickCursor, new Vector2(0f, 32f), CursorMode.Auto);
			break;
		}
	}
}
