using UnityEngine;

public class DecoupleSettings : PartSettingsBase
{
	private Decoupler decoupler;

	public GameObject gamepadBindings;

	public GameObject joystickBindings;

	public override void SelectPart(PlanePart part)
	{
		decoupler = (Decoupler)part;
		RebindButtonBase[] componentsInChildren = GetComponentsInChildren<RebindButtonBase>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetAction(decoupler.inputOverride);
		}
	}

	private void Update()
	{
		gamepadBindings.SetActive(InputManager.HasGamepad);
		joystickBindings.SetActive(InputManager.HasJoystick);
	}
}
