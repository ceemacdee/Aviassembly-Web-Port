using UnityEngine;
using UnityEngine.InputSystem;

public class Engine : PlanePart
{
	public const float AfterburnerTreshhold = 0.2f;

	public float thrust;

	public bool helicopterRotor;

	public AnimationCurve maxSpeed;

	public float electricityGeneration;

	public bool electricEngine;

	public float throttleSpeed;

	public InputActionReference sourceThrottleInputAction;

	[HideInInspector]
	public InputAction throttleInputOverride;

	public PlaneController controller;

	public float fuelConsumption;

	private PropellerVisual visual;

	private EngineSound sound;

	[Space(20f)]
	[Header("Afterburner")]
	public bool useAfterburner;

	public float afterburnerThrust;

	public float afterburnerFuelConsumption;

	public float autoHoverTargetThrottle;

	[HideInInspector]
	public float maxPower;

	public float thrustHandicap;

	[HideInInspector]
	public bool invertDirection;

	private float currentPropellerSpeed;

	private float propellerSpeedVelocity;

	private EngineParticle particle;

	private GameManager gameManager;

	public float currentThrottle { get; private set; }

	private void Awake()
	{
		visual = GetComponent<PropellerVisual>();
		sound = GetComponent<EngineSound>();
		particle = GetComponent<EngineParticle>();
		gameManager = Singleton<GameManager>.Instance;
		controller = Singleton<PlaneContainer>.Instance.gameObject.GetComponent<PlaneController>();
		maxPower = 1f;
		throttleInputOverride = InputActionCopy.FromAsset(sourceThrottleInputAction);
		throttleInputOverride.Enable();
		Singleton<InputManager>.Instance.KeybindsChanged += () =>
		{
			InputActionCopy.RefreshDefaults(throttleInputOverride, sourceThrottleInputAction);
		};
	}

	private void OnDestroy()
	{
		throttleInputOverride.Dispose();
	}

	public override void Activate()
	{
		currentThrottle = 0f;
		if (particle != null)
		{
			particle.Activate();
		}
	}

	public override void Deactivate()
	{
		currentThrottle = 0f;
		currentPropellerSpeed = 0f;
		propellerSpeedVelocity = 0f;
		if (sound != null)
		{
			sound.UpdateEngineSound(0f);
		}
		if (particle != null)
		{
			particle.Deactivate();
		}
		if (visual != null)
		{
			visual.ResetPropeller();
		}
	}

	public override void UpdatePart(PlaneContainer container)
	{
		float num = throttleInputOverride.ReadValue<float>();
		currentThrottle += num * Time.deltaTime * controller.throttleSpeed;
		if (currentThrottle <= 0f)
		{
			currentThrottle = Mathf.MoveTowards(currentThrottle, num, Time.deltaTime * 10f);
		}
		if (!InputUtility.IsFromComposite(throttleInputOverride))
		{
			currentThrottle = num;
		}
		currentThrottle = Mathf.Clamp(currentThrottle, (container.IsGrounded() || helicopterRotor) ? (-0.75f) : 0f, 1f);
		if ((electricEngine ? container.electricity : container.fuel) <= 0f)
		{
			if (sound != null)
			{
				sound.UpdateEngineSound(0f);
			}
			if (particle != null)
			{
				particle.SetThrottle(0f);
			}
			return;
		}
		float num2 = thrust;
		float num3 = fuelConsumption;
		if (useAfterburner)
		{
			float num4 = Mathf.Clamp01(currentThrottle - 0.8f) * 5f;
			num2 = Mathf.Lerp(num2, afterburnerThrust, num4);
			num3 = Mathf.Lerp(num3, afterburnerFuelConsumption, num4);
			if (particle != null)
			{
				particle.SetThrottle(num4);
			}
		}
		Vector3 vector = container.transform.InverseTransformPoint(container.GetThrustOrigin());
		Vector3 position = new Vector3(vector.x, rb.centerOfMass.y, vector.z);
		Vector3 vector2 = base.transform.parent.TransformPoint(position);
		float num5 = Mathf.Clamp01(1f - thrustHandicap);
		if (maxSpeed.keys.Length > 2)
		{
			num2 *= maxSpeed.Evaluate(rb.linearVelocity.magnitude);
		}
		rb.AddForceAtPosition(GetDirection() * currentThrottle * num2 * maxPower * num5, vector2, ForceMode.Force);
		float num6 = ((!gameManager.gameModeData.infiniteFuel || !gameManager.gameModeData.creativeMode) ? 1 : 0);
		if (!electricEngine)
		{
			container.fuel -= Mathf.Abs(currentThrottle) * num3 * maxPower * Time.fixedDeltaTime * num6;
		}
		else
		{
			container.electricity -= Mathf.Abs(currentThrottle) * num3 * maxPower * Time.fixedDeltaTime * num6;
		}
		if (!electricEngine)
		{
			container.electricity += Time.fixedDeltaTime * Mathf.Abs(currentThrottle) * electricityGeneration;
			container.electricity = Mathf.Min(container.electricity, container.electricityStorageCapacity);
		}
		Debug.DrawRay(vector2, GetDirection() * currentThrottle * thrust, Color.black);
		float target = currentThrottle;
		currentPropellerSpeed = Mathf.SmoothDamp(currentPropellerSpeed, target, ref propellerSpeedVelocity, 1f);
		if (visual != null)
		{
			visual.UpdatePropeller(currentPropellerSpeed);
		}
		if (sound != null)
		{
			sound.UpdateEngineSound(currentPropellerSpeed);
		}
		if (particle != null && !useAfterburner)
		{
			particle.SetThrottle(currentThrottle);
		}
	}

	public Vector3 GetThrustPercentage()
	{
		float num = ((!(Singleton<PlaneContainer>.Instance.fuel <= 0f)) ? 1 : 0);
		return GetDirection() * currentThrottle * num;
	}

	public Vector3 GetDirection()
	{
		Vector3 obj = (helicopterRotor ? (base.transform.up * base.transform.localScale.y) : base.transform.right);
		int num = ((!invertDirection) ? 1 : (-1));
		return obj * num;
	}

	public string GetControlScemeID()
	{
		if (Singleton<InputManager>.Instance.CurrentControlScheme == ControlScheme.KeyboardAndMouse)
		{
			return throttleInputOverride.bindings[1].effectivePath + throttleInputOverride.bindings[2].effectivePath;
		}
		return throttleInputOverride.bindings[4].effectivePath + throttleInputOverride.bindings[5].effectivePath;
	}

	public string GetKey1Path()
	{
		return throttleInputOverride.bindings[1].effectivePath;
	}

	public string GetKey2Path()
	{
		return throttleInputOverride.bindings[2].effectivePath;
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = null;
		int num = 2;
		if (!electricEngine)
		{
			num++;
		}
		if (useAfterburner)
		{
			num++;
		}
		if (maxSpeed.keys.Length > 2)
		{
			num++;
		}
		array = new PartStat[num];
		array[0].statName = "Thrust";
		array[0].SetValue(thrust);
		if (!electricEngine)
		{
			array[1].statName = "Fuel Consumption";
			array[1].SetValue(fuelConsumption * 100f);
		}
		else
		{
			array[1].statName = "Electricity Consumption";
			array[1].SetValue(fuelConsumption * 100f);
		}
		if (!electricEngine)
		{
			array[2].statName = "Electricity Generation";
			array[2].SetValue(electricityGeneration * 100f);
		}
		if (useAfterburner)
		{
			array[3].statName = "Afterburner";
			array[3].SetValue("Yes");
		}
		if (maxSpeed.keys.Length > 2)
		{
			array[num - 1].SetValue(maxSpeed.keys[1].time + " knots");
			array[num - 1].statName = "Optimal speed";
		}
		return array;
	}

	public override void Save(GameDataWriter writer)
	{
		writer.Write(invertDirection);
		writer.Write(maxPower);
		writer.Write(throttleInputOverride.SaveBindingOverridesAsJson());
	}

	public override void Load(GameDataReader reader)
	{
		if (reader.version > 4 && reader.version < 30)
		{
			invertDirection = reader.ReadBool();
			maxPower = reader.ReadFloat();
			string text = reader.ReadString();
			string text2 = reader.ReadString();
			if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
			{
				throttleInputOverride.ApplyBindingOverride(1, text);
			}
			if (!string.IsNullOrEmpty(text2) && !string.IsNullOrWhiteSpace(text2))
			{
				throttleInputOverride.ApplyBindingOverride(2, text2);
			}
		}
		if (reader.version > 28 && reader.version < 30)
		{
			string text3 = reader.ReadString();
			string text4 = reader.ReadString();
			if (!string.IsNullOrEmpty(text3) && !string.IsNullOrWhiteSpace(text3))
			{
				throttleInputOverride.ApplyBindingOverride(4, text3);
			}
			if (!string.IsNullOrEmpty(text4) && !string.IsNullOrWhiteSpace(text4))
			{
				throttleInputOverride.ApplyBindingOverride(5, text4);
			}
		}
		if (reader.version >= 30)
		{
			invertDirection = reader.ReadBool();
			maxPower = reader.ReadFloat();
			throttleInputOverride.LoadBindingOverridesFromJson(reader.ReadString());
		}
	}

	public float GetThrustAtSpeed(float evaluationSpeed)
	{
		if (useAfterburner)
		{
			return afterburnerThrust;
		}
		if (maxSpeed.keys.Length != 0)
		{
			return thrust * maxSpeed.Evaluate(evaluationSpeed);
		}
		return thrust;
	}
}
