using System;
using System.Collections.Generic;
using UnityEngine;

public class PlaneController : MonoBehaviour
{
	public float controlHandicap;

	public float rollControlSurfaceLift;

	public float pitchControlSurfaceLift;

	[Space(15f)]
	[Header("Explosion")]
	public AudioDef explosionSound;

	public GameObject explosionParticlePrefab;

	public float maxTriggerVelocity;

	public float minTriggerVelocity;

	[Space(15f)]
	[Header("Helper forces")]
	[Range(0f, 1f)]
	public float airTraction;

	public float stablisationForce;

	public float helicopterStabilisationForce;

	[Range(0f, 1f)]
	public float landingHelper;

	[Space(15f)]
	[Header("Auto Steer")]
	public float rollTurnForce;

	public float rollRestoreForce;

	public bool autoSteer;

	public bool newSteering;

	[Space(15f)]
	[Header("Controls")]
	public float rollForce;

	public float pitchForce;

	public float yawForce;

	[Space(10f)]
	public float throttleSpeed;

	private Rigidbody rb;

	private WingVisual leftWingVisual;

	private WingVisual rightWingVisual;

	private float prefVelocityMagnitude;

	private float rudderInputSmooth;

	private float rudderInputVelocity;

	public float rudderSmoothTime;

	private float rudderInput;

	private float throttleVelocity;

	private PlaneContainer container;

	private InputManager inputManager;

	private List<Tuple<Wing, float>> tailWings = new List<Tuple<Wing, float>>();

	private List<Wing> mainWings = new List<Wing>();

	private Dictionary<GameObject, WingVisual> wingVisuals = new Dictionary<GameObject, WingVisual>();

	private float prevVelocity;

	public float currentThrottle { get; private set; }

	public float currentThrottleRaw { get; private set; }

	public bool Exploded { get; private set; }

	private void Awake()
	{
		container = GetComponent<PlaneContainer>();
	}

	private void Start()
	{
		inputManager = Singleton<InputManager>.Instance;
	}

	private void Update()
	{
		UpdateThrottle();
	}

	private void UpdateThrottle()
	{
		int num = (int)inputManager.throttleInput;
		currentThrottle = Mathf.SmoothDamp(currentThrottle, num, ref throttleVelocity, throttleSpeed);
		currentThrottleRaw = num;
		rudderInput = inputManager.yawInput;
		rudderInputSmooth = Mathf.SmoothDamp(rudderInputSmooth, rudderInput, ref rudderInputVelocity, rudderSmoothTime);
		if (rudderInput != 0f)
		{
			rudderInputSmooth = rudderInput;
		}
	}

	private void ApplyInertiaTensorAjustedTorque(Vector3 worldSpaceTorque)
	{
		Vector3 vector = base.transform.InverseTransformDirection(worldSpaceTorque);
		Vector3 direction = container.CalculateInertiaTensorMatrix() * vector;
		Vector3 torque = base.transform.TransformDirection(direction);
		rb.AddTorque(torque);
	}

	private void UpdateControlls()
	{
		autoSteer = Singleton<GameManager>.Instance.autoSteer;
		float num = Mathf.Clamp01(Mathf.InverseLerp(50f, 0f, container.GetMass() * 15f));
		controlHandicap = 0.4f + num * 0.6f;
		float num2 = Vector3.Dot(rb.linearVelocity.normalized, container.Forward);
		float num3 = Mathf.Clamp01(rb.linearVelocity.magnitude / 50f);
		Vector3 normalized = Vector3.Cross(-container.Forward, base.transform.up).normalized;
		float num4 = Mathf.Clamp01(container.DistanceFromGround() / 30f);
		float num5 = Mathf.Clamp01(rollControlSurfaceLift * 150f * controlHandicap + container.helicopter);
		float num6 = rollForce * Mathf.Lerp(num3, 1f, container.helicopter);
		if (autoSteer)
		{
			num6 *= num4;
		}
		ApplyInertiaTensorAjustedTorque(-container.Forward * inputManager.rollInput * num6 * num5);
		float num7 = Mathf.Clamp01(controlHandicap + pitchControlSurfaceLift * 40f);
		float a = num3 * num2;
		float num8 = pitchForce * Mathf.Lerp(a, 1f, container.helicopter);
		ApplyInertiaTensorAjustedTorque(normalized * inputManager.pitchInput * num8 * num7);
		float num9 = Mathf.Clamp01(rb.linearVelocity.magnitude / 10f);
		float num10 = yawForce * num9 * num2;
		float num11 = (container.IsGrounded() ? 1.25f : 1f);
		ApplyInertiaTensorAjustedTorque(base.transform.up * rudderInputSmooth * num10 * num11);
		if (autoSteer)
		{
			Vector3 normalized2 = Vector3.Cross(Vector3.up, container.Forward).normalized;
			Vector3 normalized3 = Vector3.Cross(container.Forward, normalized2).normalized;
			container.yawDrag = 0f;
			rollForce = 5f;
			yawForce = 1.5f;
			float currentRoll = GetCurrentRoll();
			float b = 0.99f;
			float t = Mathf.Clamp01(Mathf.InverseLerp(0.95f, b, Mathf.Abs(currentRoll)));
			currentRoll *= 1f - Mathf.SmoothStep(0f, 1f, t);
			if (Mathf.Abs(Vector3.Dot(container.Forward, Vector3.up)) > 0.98f)
			{
				currentRoll = 0f;
			}
			float f = Mathf.Clamp01(Mathf.Abs(currentRoll) - 0.0125f) * Mathf.Sign(currentRoll) * rollTurnForce;
			f = Mathf.Sqrt(Mathf.Abs(f)) * Mathf.Sign(f);
			ApplyInertiaTensorAjustedTorque(normalized3 * f);
			float num12 = Mathf.Sqrt(Mathf.Abs(currentRoll)) * Mathf.Sign(currentRoll);
			ApplyInertiaTensorAjustedTorque(container.Forward * num12 * rollRestoreForce);
			f = inputManager.rollInput;
			ApplyInertiaTensorAjustedTorque(normalized3 * (1f - num4) * yawForce * num10 * f * num11);
		}
		else
		{
			container.yawDrag = 0.2f;
			rollForce = 3.5f;
			yawForce = 6f;
		}
	}

	private void FixedUpdate()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
			return;
		}
		UpdateWingVisuals();
		UpdateControlls();
		PlaneStabilisationForce(1f - container.helicopter);
		HelicopterStabilisationForce(container.helicopter);
		UpdateControllability();
		Vector3 normalized = Vector3.Cross(container.Forward, base.transform.up).normalized;
		float num = Vector3.Dot(normalized.normalized, rb.linearVelocity) * airTraction * rb.mass;
		float num2 = Mathf.Abs(Vector3.Dot(base.transform.up, Vector3.up));
		rb.AddForceAtPosition(num * -normalized * num2, base.transform.TransformPoint(rb.centerOfMass));
		float magnitude = rb.linearVelocity.magnitude;
		float t = (Vector3.Dot(Vector3.up, base.transform.up) + 1f) * 0.5f;
		float num3 = Mathf.Lerp(minTriggerVelocity, maxTriggerVelocity, t);
		if (Mathf.Sign(magnitude - prefVelocityMagnitude) < 0f && container.IsGrounded())
		{
			container.liftMultiplier = 1f - landingHelper;
		}
		else
		{
			container.liftMultiplier = 1f;
		}
		if (Mathf.Abs(magnitude - prefVelocityMagnitude) > num3)
		{
			ExplodePlane();
		}
		if (base.transform.position.y < -12f)
		{
			ExplodePlane();
		}
		prefVelocityMagnitude = magnitude;
	}

	public void ResetController()
	{
		prefVelocityMagnitude = 0f;
		currentThrottle = 0f;
		Exploded = false;
		wingVisuals.Clear();
		WingVisual[] componentsInChildren = GetComponentsInChildren<WingVisual>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			wingVisuals.Add(componentsInChildren[i].gameObject, componentsInChildren[i]);
		}
	}

	private void UpdateStabilisationForce()
	{
		Vector3 forward = container.Forward;
		Vector3 normalized = Vector3.Cross(container.Forward, base.transform.up).normalized;
		Vector3 vector = base.transform.InverseTransformDirection(-normalized);
		Vector3 vector2 = Vector3.ProjectOnPlane(rb.linearVelocity, normalized);
		float num = (1f - Mathf.Abs(Vector3.Dot(forward, vector2.normalized))) * Mathf.Sign(Vector3.SignedAngle(vector2, forward, normalized));
		num *= vector2.magnitude / 50f;
		num *= stablisationForce;
		Vector3 vector3 = vector * num;
		Vector3 direction = container.CalculateInertiaTensorMatrix() * vector3;
		Vector3 torque = base.transform.TransformDirection(direction);
		rb.AddTorque(torque);
		Debug.DrawRay(base.transform.position, forward * 100f, Color.blue);
		Debug.DrawRay(base.transform.position, rb.linearVelocity * 100f, Color.red);
	}

	private void PlaneStabilisationForce(float multiplier)
	{
		Vector3 forward = container.Forward;
		Vector3 linearVelocity = rb.linearVelocity;
		Vector3 normalized = Vector3.Cross(linearVelocity.normalized, forward).normalized;
		float num = (1f - Mathf.Abs(Vector3.Dot(forward, linearVelocity.normalized))) * Mathf.Sign(Vector3.SignedAngle(forward, linearVelocity.normalized, normalized));
		num *= linearVelocity.magnitude / 50f;
		num *= stablisationForce;
		ApplyInertiaTensorAjustedTorque(normalized * num * multiplier);
	}

	private void HelicopterStabilisationForce(float multiplier)
	{
		Vector3 normalized = Vector3.Cross(base.transform.up, Vector3.up).normalized;
		float num = (1f - Mathf.Abs(Vector3.Dot(Vector3.up, base.transform.up))) * Mathf.Sign(Vector3.SignedAngle(base.transform.up, Vector3.up, normalized));
		num *= helicopterStabilisationForce;
		ApplyInertiaTensorAjustedTorque(normalized * num * multiplier);
	}

	private float GetCurrentRoll()
	{
		Vector3 normalized = Vector3.ProjectOnPlane(Vector3.up, container.Forward).normalized;
		float num = 1f - Mathf.Clamp01((Vector3.Dot(base.transform.up, normalized) + 1f) * 0.5f);
		float num2 = Mathf.Sign(Vector3.SignedAngle(base.transform.up, normalized, container.Forward));
		return num * num2;
	}

	private WingVisual GetWingVisual(GameObject obj)
	{
		if (wingVisuals.ContainsKey(obj))
		{
			return wingVisuals[obj];
		}
		return null;
	}

	public void ApplyMouseSteering(Vector3 targetDirection, float targetRoll)
	{
		Vector3 forward = container.Forward;
		Vector3 normalized = Vector3.Cross(targetDirection.normalized, forward).normalized;
		float num = Mathf.Max(1f - Mathf.Abs(Vector3.Dot(forward, targetDirection.normalized)), 0.05f) * Mathf.Sign(Vector3.SignedAngle(forward, targetDirection.normalized, normalized));
		num *= stablisationForce * 2f * Mathf.Clamp01(container.GetVelocityMagintude() / 60f);
		ApplyInertiaTensorAjustedTorque(normalized * num);
		float num2 = (Mathf.Lerp(-0.5f, 0.5f, (targetRoll + 1f) * 0.5f) - GetCurrentRoll()) * 5f;
		ApplyInertiaTensorAjustedTorque(container.Forward * (0f - num2));
	}

	private void UpdateWingVisuals()
	{
		float num = 0f;
		float num2 = float.MaxValue;
		Vector3 lhs = Vector3.Cross(container.Forward, base.transform.up);
		for (int i = 0; i < base.transform.childCount; i++)
		{
			GameObject gameObject = base.transform.GetChild(i).gameObject;
			if (gameObject.name.Contains("Wing"))
			{
				float num3 = Vector3.Dot(lhs, gameObject.transform.position - base.transform.position);
				if (num3 > num)
				{
					rightWingVisual = GetWingVisual(gameObject);
					num = num3;
				}
				if (num3 < num2)
				{
					leftWingVisual = GetWingVisual(gameObject);
					num2 = num3;
				}
			}
		}
		if (leftWingVisual != null)
		{
			leftWingVisual.SetAileron(base.transform.up * (int)Singleton<InputManager>.Instance.rollInput);
		}
		if (rightWingVisual != null)
		{
			rightWingVisual.SetAileron(-base.transform.up * (int)Singleton<InputManager>.Instance.rollInput);
		}
		float num4 = 0f;
		float num5 = float.MaxValue;
		for (int j = 0; j < base.transform.childCount; j++)
		{
			Transform child = base.transform.GetChild(j);
			float num6 = Vector3.Distance(base.transform.position, child.transform.position) * Vector3.Dot(container.Forward.normalized, (child.transform.position - base.transform.position).normalized);
			if (num6 < num5)
			{
				num4 = Mathf.Abs(num6);
				num5 = num6;
			}
		}
		Vector3 a = base.transform.position - container.Forward * num4;
		for (int k = 0; k < base.transform.childCount; k++)
		{
			GameObject gameObject2 = base.transform.GetChild(k).gameObject;
			string text = gameObject2.name;
			if (!text.Contains("Wing") && !text.Contains("wing"))
			{
				continue;
			}
			bool flag = Mathf.Abs(Vector3.Dot(base.transform.up, gameObject2.transform.forward)) > 0.6f;
			bool num7 = Vector3.Distance(a, gameObject2.transform.position) < 1.5f;
			if (num7 & flag)
			{
				WingVisual wingVisual = GetWingVisual(gameObject2);
				if ((bool)wingVisual)
				{
					wingVisual.SetAileron(base.transform.right * (0f - rudderInput));
				}
			}
			if (num7 && !flag)
			{
				WingVisual wingVisual2 = GetWingVisual(gameObject2);
				if ((bool)wingVisual2)
				{
					wingVisual2.SetAileron(base.transform.up * Singleton<InputManager>.Instance.pitchInput);
				}
			}
		}
	}

	public void UpdateControllSurfaces(Rigidbody rb)
	{
		mainWings.Clear();
		tailWings.Clear();
		Wing[] componentsInChildren = base.gameObject.GetComponentsInChildren<Wing>();
		float planeLength = container.GetPlaneLength();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Vector3 closestPointOnLine = CustomMath.GetClosestPointOnLine(base.transform.TransformPoint(rb.centerOfMass), container.Forward, componentsInChildren[i].transform.position);
			Vector3 vector = base.transform.TransformPoint(rb.centerOfMass) - closestPointOnLine;
			float magnitude = vector.magnitude;
			float num = Vector3.Dot(container.Forward, vector.normalized);
			if (magnitude > planeLength * 0.25f && num > 0f && Mathf.Abs(Vector3.Dot(componentsInChildren[i].orientation.up, base.transform.up)) > 0.75f)
			{
				tailWings.Add(new Tuple<Wing, float>(componentsInChildren[i], magnitude));
			}
			else
			{
				mainWings.Add(componentsInChildren[i]);
			}
		}
	}

	private void UpdateControllability()
	{
		rollControlSurfaceLift = 0f;
		pitchControlSurfaceLift = 0f;
		for (int i = 0; i < mainWings.Count; i++)
		{
			if (mainWings[i].gameObject.activeInHierarchy)
			{
				rollControlSurfaceLift += mainWings[i].liftForce;
			}
		}
		for (int j = 0; j < tailWings.Count; j++)
		{
			if (tailWings[j].Item1.gameObject.activeInHierarchy)
			{
				pitchControlSurfaceLift += tailWings[j].Item1.liftForce * tailWings[j].Item2;
			}
		}
		rollControlSurfaceLift = Mathf.Max(pitchControlSurfaceLift, rollControlSurfaceLift);
	}

	public void ExplodePlane()
	{
		Singleton<AudioManager>.Instance.PlaySound(explosionSound);
		GameObject gameObject = UnityEngine.Object.Instantiate(explosionParticlePrefab);
		gameObject.transform.position = base.transform.position;
		gameObject.transform.localScale = Vector3.one * container.GetPlaneLength() * 0.2f;
		UnityEngine.Object.Destroy(gameObject, gameObject.GetComponent<ParticleSystem>().main.duration - 0.1f);
		base.gameObject.SetActive(value: false);
		Singleton<ParticleSpawner>.Instance.DestoyAll();
		Exploded = true;
		container.GetComponent<PartExploder>().ExplodePlane();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		if (rb != null)
		{
			Gizmos.DrawWireSphere(base.transform.TransformPoint(rb.centerOfMass), 0.2f);
		}
	}
}
