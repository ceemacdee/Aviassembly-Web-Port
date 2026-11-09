using UnityEngine;

public class RotatorKeybindings : PartSettingsBase
{
	private Rotator rotator;

	public GameObject gamepadBindings;

	public GameObject joystickBindings;

	public override void SelectPart(PlanePart part)
	{
		rotator = (Rotator)part;
		RebindButtonBase[] componentsInChildren = GetComponentsInChildren<RebindButtonBase>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetAction(rotator.inputOverride);
		}
	}

	private void Update()
	{
		gamepadBindings.SetActive(InputManager.HasGamepad);
		joystickBindings.SetActive(InputManager.HasJoystick);
	}
}
