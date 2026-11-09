using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RotatorSettings : PartSettingsBase
{
	public TMP_Text minAngleText;

	public TMP_Text maxAngleText;

	public TMP_Text rotationSpeedText;

	public Slider minAngle;

	public Slider maxAngle;

	public Slider rotationSpeed;

	public Toggle returnToStart;

	private Rotator rotator;

	private void Update()
	{
		rotator.angleClamp.x = -180f * minAngle.value;
		rotator.angleClamp.y = 180f * maxAngle.value;
		rotator.speed = rotationSpeed.value * 200f;
		rotator.returnToStart = returnToStart.isOn;
		minAngleText.text = Mathf.RoundToInt(-180f * minAngle.value).ToString();
		maxAngleText.text = Mathf.RoundToInt(180f * maxAngle.value).ToString();
		rotationSpeedText.text = Mathf.RoundToInt(rotator.speed).ToString();
	}

	public override void SelectPart(PlanePart part)
	{
		rotator = (Rotator)part;
		minAngle.SetValueWithoutNotify(Mathf.InverseLerp(0f, -180f, rotator.angleClamp.x));
		maxAngle.SetValueWithoutNotify(Mathf.InverseLerp(0f, 180f, rotator.angleClamp.y));
		rotationSpeed.SetValueWithoutNotify(Mathf.InverseLerp(0f, 200f, rotator.speed));
		returnToStart.SetIsOnWithoutNotify(rotator.returnToStart);
	}
}
