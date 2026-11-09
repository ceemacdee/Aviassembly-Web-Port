using UnityEngine;
using UnityEngine.InputSystem;

public class Rotator : PlanePart
{
	public InputActionReference sourceInputAction;

	public float speed;

	public InputAction inputOverride;

	public Transform rotatorBase;

	public Vector2 angleClamp;

	public bool returnToStart;

	private BuildingPart part;

	public float rotation;

	private void Awake()
	{
		part = GetComponent<BuildingPart>();
		angleClamp = new Vector2(-180f, 180f);
		inputOverride = InputActionCopy.FromAsset(sourceInputAction);
		inputOverride.Enable();
	}

	private void OnDestroy()
	{
		inputOverride.Dispose();
	}

	public override void Activate()
	{
		AddRotation(0f - rotation);
		rotatorBase.localEulerAngles = new Vector3(0f, 0f, 0f);
		rotation = 0f;
	}

	public override void Deactivate()
	{
		AddRotation(0f - rotation);
		rotatorBase.localEulerAngles = new Vector3(0f, 0f, 0f);
		rotation = 0f;
	}

	public override void UpdatePart(PlaneContainer container)
	{
		float num = inputOverride.ReadValue<float>();
		float num2 = CustomMath.SnapToIncrement(Time.deltaTime * num * speed, 0.1f);
		if (Mathf.Approximately(num, 0f) && returnToStart)
		{
			num2 = (0f - rotation) * Time.fixedDeltaTime * 10f;
		}
		rotation += num2;
		rotatorBase.localEulerAngles = new Vector3(0f, 0f, 0f - rotation);
		AddRotation(num2);
		rotation = ((rotation + 180f) % 360f + 360f) % 360f - 180f;
		float num3 = Mathf.Max(angleClamp.x, angleClamp.y);
		float num4 = Mathf.Min(angleClamp.x, angleClamp.y);
		if (rotation > num3)
		{
			AddRotation(num3 - rotation);
			rotation = num3;
			rotatorBase.localEulerAngles = new Vector3(0f, 0f, 0f - rotation);
		}
		if (rotation < num4)
		{
			AddRotation(num4 - rotation);
			rotation = num4;
			rotatorBase.localEulerAngles = new Vector3(0f, 0f, 0f - rotation);
		}
	}

	private void AddRotation(float rotation)
	{
		if (!Mathf.Approximately(rotation, 0f))
		{
			part.SetRotationRotator(Quaternion.AngleAxis(rotation, base.transform.forward * Mathf.Sign(base.transform.localScale.y)) * base.transform.rotation);
		}
	}

	public override PartStat[] GetPartStats()
	{
		return new PartStat[0];
	}

	public override void Save(GameDataWriter writer)
	{
		writer.Write(rotation);
		writer.Write(speed);
		writer.Write(angleClamp);
		writer.Write(returnToStart);
		writer.Write(inputOverride.SaveBindingOverridesAsJson());
	}

	public override void Load(GameDataReader reader)
	{
		rotation = reader.ReadFloat();
		if (reader.version < 30)
		{
			string text = reader.ReadString();
			string text2 = reader.ReadString();
			if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
			{
				inputOverride.ApplyBindingOverride(1, text);
			}
			if (!string.IsNullOrEmpty(text2) && !string.IsNullOrWhiteSpace(text2))
			{
				inputOverride.ApplyBindingOverride(2, text2);
			}
		}
		if (reader.version > 27)
		{
			speed = reader.ReadFloat();
			angleClamp = reader.ReadVector2();
			returnToStart = reader.ReadBool();
		}
		if (reader.version > 28 && reader.version < 30)
		{
			string text3 = reader.ReadString();
			string text4 = reader.ReadString();
			if (!string.IsNullOrEmpty(text3) && !string.IsNullOrWhiteSpace(text3))
			{
				inputOverride.ApplyBindingOverride(4, text3);
			}
			if (!string.IsNullOrEmpty(text4) && !string.IsNullOrWhiteSpace(text4))
			{
				inputOverride.ApplyBindingOverride(5, text4);
			}
		}
		if (reader.version >= 30)
		{
			inputOverride.LoadBindingOverridesFromJson(reader.ReadString());
		}
	}
}
