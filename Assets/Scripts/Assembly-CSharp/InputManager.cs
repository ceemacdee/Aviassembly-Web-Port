using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
	public PlayerInput playerInput;

	public InputBlocker currentInputBlocker;

	[Header("Read every frame")]
	public float throttleInput;

	public float rollInput;

	public float yawInput;

	public float pitchInput;

	public bool wheelRetraction;

	public AudioDef rebindSound;

	private static readonly Dictionary<Keys, (string action, string part)> keyBindings = new Dictionary<Keys, (string, string)>
	{
		{
			Keys.PitchUp,
			("Pitch", "Negative")
		},
		{
			Keys.PitchDown,
			("Pitch", "Positive")
		},
		{
			Keys.RollLeft,
			("Roll", "Negative")
		},
		{
			Keys.RollRight,
			("Roll", "Positive")
		},
		{
			Keys.YawLeft,
			("Yaw", "Negative")
		},
		{
			Keys.YawRight,
			("Yaw", "Positive")
		},
		{
			Keys.Break,
			("Throttle", "Negative")
		},
		{
			Keys.Forward,
			("Throttle", "Positive")
		},
		{
			Keys.RetractGear,
			("WheelRetraction", null)
		}
	};

	private InputAction pitchAction;

	private InputAction rollAction;

	private InputAction yawAction;

	private InputAction throttleAction;

	private InputAction wheelRetractionAction;

	public InputAction openMapAction;

	public static bool HasGamepad => Gamepad.all.Count > 0;

	public static bool HasKeyboard => Keyboard.current != null;

	public static bool HasJoystick => Joystick.all.Count > 0;

	public ControlScheme CurrentControlScheme { get; private set; }

	private static string KeybindsPath => Path.Combine(Application.persistentDataPath, "keybinds.json");

	public event Action ControlSchemeChanged;

	public event Action KeybindsChanged;

	protected override void Awake()
	{
		base.Awake();
		InputActionAsset actions = playerInput.actions;
		pitchAction = actions["Pitch"];
		rollAction = actions["Roll"];
		yawAction = actions["Yaw"];
		throttleAction = actions["Throttle"];
		wheelRetractionAction = actions["WheelRetraction"];
		openMapAction = actions["OpenMap"];
		LoadKeybinds();
		TryParseScheme(playerInput.currentControlScheme, out var scheme);
		CurrentControlScheme = scheme;
		playerInput.onControlsChanged += OnControlsChanged;
	}

	private void Update()
	{
		pitchInput = pitchAction.ReadValue<float>();
		rollInput = rollAction.ReadValue<float>();
		yawInput = yawAction.ReadValue<float>();
		throttleInput = throttleAction.ReadValue<float>();
		wheelRetraction = wheelRetractionAction.WasPressedThisFrame();
	}

	private void OnControlsChanged(PlayerInput input)
	{
		if (TryParseScheme(input.currentControlScheme, out var scheme))
		{
			CurrentControlScheme = scheme;
		}
		ControlSchemeChanged?.Invoke();
	}

	public static string GroupOf(ControlScheme scheme)
	{
		return scheme switch
		{
			ControlScheme.Gamepad => "gamepad", 
			ControlScheme.Joystick => "joystick", 
			_ => "keyboard", 
		};
	}

	public static bool TryParseScheme(string schemeName, out ControlScheme scheme)
	{
		ControlScheme[] array = (ControlScheme[])Enum.GetValues(typeof(ControlScheme));
		foreach (ControlScheme controlScheme in array)
		{
			if (string.Equals(GroupOf(controlScheme), schemeName, StringComparison.OrdinalIgnoreCase))
			{
				scheme = controlScheme;
				return true;
			}
		}
		scheme = ControlScheme.KeyboardAndMouse;
		return false;
	}

	public string GetKeyPath(Keys key, ControlScheme scheme)
	{
		if (!keyBindings.TryGetValue(key, out (string, string) value))
		{
			return "";
		}
		return GetKeyPath(value.Item1, scheme, value.Item2);
	}

	public string GetKeyPath(Keys key)
	{
		return GetKeyPath(key, CurrentControlScheme);
	}

	public string GetKeyPath(string actionName, ControlScheme scheme, string part = null)
	{
		InputAction action = playerInput.actions.FindAction(actionName);
		return GetKeyPath(action, scheme, part);
	}

	public string GetKeyPath(InputAction action, ControlScheme scheme, string part = null)
	{
		if (action == null)
		{
			return "";
		}
		string text = GroupOf(scheme);
		string text2 = PathAt(action, InputActionCopy.FindBindingIndex(action, text, part));
		if (string.IsNullOrEmpty(text2) && part != null)
		{
			text2 = PathAt(action, InputActionCopy.FindBindingIndex(action, text));
		}
		return text2 ?? "";
	}

	private static string PathAt(InputAction action, int index)
	{
		if (index < 0)
		{
			return null;
		}
		return action.bindings[index].effectivePath;
	}

	public void SaveKeybinds()
	{
		try
		{
			File.WriteAllText(KeybindsPath, playerInput.actions.SaveBindingOverridesAsJson());
			WebGLFileSync.Flush();
		}
		catch (Exception ex)
		{
			Debug.LogError("Couldn't save keybinds to " + KeybindsPath + ": " + ex.Message);
		}
		KeybindsChanged?.Invoke();
	}

	private void LoadKeybinds()
	{
		if (File.Exists(KeybindsPath))
		{
			try
			{
				playerInput.actions.LoadBindingOverridesFromJson(File.ReadAllText(KeybindsPath));
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Couldn't load keybinds from " + KeybindsPath + ", using defaults: " + ex.Message);
			}
			KeybindsChanged?.Invoke();
		}
	}

	public void ResetAllKeybinds()
	{
		playerInput.actions.RemoveAllBindingOverrides();
		SaveKeybinds();
	}
}
