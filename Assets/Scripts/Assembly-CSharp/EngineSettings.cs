using UnityEngine.UI;

public class EngineSettings : PartSettingsBase
{
	public Toggle invertToggle;

	public Slider maxPower;

	private Engine engine;

	private void Update()
	{
		engine.invertDirection = invertToggle.isOn;
		engine.maxPower = maxPower.value;
	}

	public override void SelectPart(PlanePart part)
	{
		engine = (Engine)part;
		invertToggle.SetIsOnWithoutNotify(engine.invertDirection);
		maxPower.SetValueWithoutNotify(engine.maxPower);
	}
}
