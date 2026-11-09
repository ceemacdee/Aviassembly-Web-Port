using UnityEngine;

public class EngineKeybindings : PartSettingsBase
{
	private Engine engine;

	public GameObject gamepadBindings;

	public GameObject joystickBindings;

	public override void SelectPart(PlanePart part)
	{
		engine = (Engine)part;
		RebindButtonBase[] componentsInChildren = GetComponentsInChildren<RebindButtonBase>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetAction(engine.throttleInputOverride);
		}
	}

	private void Update()
	{
		gamepadBindings.SetActive(InputManager.HasGamepad);
		joystickBindings.SetActive(InputManager.HasJoystick);
	}
}
