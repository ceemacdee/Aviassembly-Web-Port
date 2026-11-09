using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnboundKeysWarning : MonoBehaviour
{
	public GameObject[] keybindingSettingsPanel;

	public GameObject unboundKeyWarning;

	private RebindButtonBase[] rebindButtons;

	public TMP_Text descriptor;

	private void Awake()
	{
		List<RebindButtonBase> list = new List<RebindButtonBase>();
		for (int i = 0; i < keybindingSettingsPanel.Length; i++)
		{
			list.AddRange(keybindingSettingsPanel[i].GetComponentsInChildren<RebindButtonBase>());
		}
		rebindButtons = list.ToArray();
	}

	private void Update()
	{
		bool active = false;
		string text = "";
		for (int i = 0; i < rebindButtons.Length; i++)
		{
			if (!rebindButtons[i].IsBound() && HasInputDevice(rebindButtons[i]) && Singleton<GameManager>.Instance.inMenu)
			{
				active = true;
				text = text + rebindButtons[i].actionReference.action.name + " - " + GetControlScheme(rebindButtons[i]) + "\n";
			}
		}
		descriptor.text = text;
		descriptor.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
		unboundKeyWarning.SetActive(active);
	}

	public bool HasInputDevice(RebindButtonBase rebindButtonBase)
	{
		return GetControlScheme(rebindButtonBase) switch
		{
			"keyboard" => InputManager.HasKeyboard, 
			"gamepad" => InputManager.HasGamepad, 
			"joystick" => InputManager.HasJoystick, 
			_ => false, 
		};
	}

	private string GetControlScheme(RebindButtonBase rebindButtonBase)
	{
		if (rebindButtonBase.GetType() == typeof(RebindButton))
		{
			return InputUtility.GetGroups(rebindButtonBase.actionReference.action.bindings[InputUtility.FindBindingIndex(rebindButtonBase.actionReference, ((RebindButton)rebindButtonBase).bindingId)])[0];
		}
		if (rebindButtonBase.GetType() == typeof(RebindAxisButton))
		{
			return InputUtility.GetGroups(rebindButtonBase.actionReference.action.bindings[InputUtility.FindBindingIndex(rebindButtonBase.actionReference, ((RebindAxisButton)rebindButtonBase).bindingIdFullAxis)])[0];
		}
		return "";
	}
}
