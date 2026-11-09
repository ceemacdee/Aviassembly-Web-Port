using System.Collections.Generic;
using UnityEngine;

public class PlaneContainer : Singleton<PlaneContainer>
{
	[Range(0f, 1f)]
	public float helicopter;

	[Header("Drag")]
	[Range(0f, 1f)]
	public float yawDrag;

	[Range(0f, 1f)]
	public float rollDrag;

	[Range(0f, 1f)]
	public float pitchDrag;

	public float verticalLiniarDrag;

	public float angleDragMultiplier;

	[Header("Force Symmetry")]
	[Range(0f, 1f)]
	public float liftSymmetry;

	[Range(0f, 1f)]
	public float thrustSymmetry;

	[Header("Gravity")]
	public float gravityForce;

	public float planeGravityMultiplier;

	public float helicopterGravityMultiplier;

	[HideInInspector]
	public float liftMultiplier;

	public LayerMask groundedCheckLayerMask;

	[Header("CG calculation")]
	public float maxCGShift;

	[Header("Fuel weight")]
	public float fuelWeight;

	private Rigidbody rb;

	private PlaneController controller;

	private PartExploder exploder;

	private float mass;

	private float drag;

	private Vector3 cachedLocalForward;

	[HideInInspector]
	public List<PlanePart> planeParts = new List<PlanePart>();

	[HideInInspector]
	public float fuel;

	[HideInInspector]
	public float fuelCapacity;

	public float refrenceFuelCapacity;

	[HideInInspector]
	public float electricity;

	[HideInInspector]
	public float electricityStorageCapacity;

	[HideInInspector]
	public float refrenceElectricityStorageCapacity;

	[HideInInspector]
	public Vector3 spawnPoint;

	[HideInInspector]
	public float airbreakDrag;

	private float previousVelocity;

	private Engine[] engines = new Engine[0];

	public float currentLift;

	public float GravityMultiplier
	{
		get
		{
			return Mathf.Lerp(planeGravityMultiplier, helicopterGravityMultiplier, helicopter);
		}
		set
		{
		}
	}

	public float RealGravity
	{
		get
		{
			return gravityForce * GravityMultiplier;
		}
		set
		{
		}
	}

	public Vector3 Forward
	{
		get
		{
			return base.transform.TransformDirection(cachedLocalForward);
		}
		private set
		{
		}
	}

	public float cargoVolume { get; private set; }

	public bool FlightModeInitialized { get; private set; }

	public bool hasRetractableGear { get; private set; }

	public bool hasBalloons { get; private set; }

	public bool IsAccelerating { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		controller = GetComponent<PlaneController>();
		liftMultiplier = 1f;
		exploder = GetComponent<PartExploder>();
		exploder.Init();
	}

	private void FixedUpdate()
	{
		if (rb == null)
		{
			return;
		}
		if (rb.inertiaTensor.magnitude > Mathf.Epsilon)
		{
			rb.inertiaTensor = new Vector3(Mathf.Max(1f, rb.inertiaTensor.x), Mathf.Max(1f, rb.inertiaTensor.y), Mathf.Max(1f, rb.inertiaTensor.z));
		}
		airbreakDrag = 1f;
		currentLift = 0f;
		for (int i = 0; i < planeParts.Count; i++)
		{
			if (planeParts[i].gameObject.activeInHierarchy)
			{
				planeParts[i].UpdatePart(this);
			}
		}
		ApplyAngularDrag();
		ApplyLiniarDrag(helicopter * verticalLiniarDrag);
		rb.AddForce(Vector3.down * RealGravity * rb.mass);
		float b = (1f - Mathf.Abs(Vector3.Dot(Forward, GetVelocity().normalized))) * angleDragMultiplier + 1f;
		float num = Mathf.Lerp(1f - Mathf.Clamp01(Vector3.Dot(Forward, Vector3.down)), b, helicopter);
		rb.linearDamping = drag * num * airbreakDrag;
		rb.mass = mass + fuel * fuelWeight / 15f;
		float angleOfAttackOffset = Mathf.Lerp(10f, 0f, Mathf.Clamp01(DistanceFromGround() / 10f));
		if (!Input.GetKey(KeyCode.Space))
		{
			angleOfAttackOffset = 0f;
		}
		Wing[] componentsInChildren = GetComponentsInChildren<Wing>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			componentsInChildren[j].angleOfAttackOffset = angleOfAttackOffset;
		}
		float velocityMagintude = GetVelocityMagintude();
		float num2 = Mathf.Sign(Vector3.Dot(Forward, GetVelocity().normalized));
		IsAccelerating = velocityMagintude - previousVelocity > 0.05f && num2 > 0f;
		previousVelocity = velocityMagintude;
		Vector3 zero = Vector3.zero;
		for (int k = 0; k < engines.Length; k++)
		{
			zero += engines[k].GetDirection();
		}
		Vector3 normalized = (zero / engines.Length).normalized;
		helicopter = Mathf.Abs(Vector3.Dot(base.transform.up, normalized));
		if (componentsInChildren == null || componentsInChildren.Length == 0)
		{
			helicopter = 1f;
		}
		UpdateAutoHover();
	}

	private void ApplyAngularDrag()
	{
		Vector3 vector = new Vector3(pitchDrag, yawDrag, rollDrag);
		Vector3 vector2 = base.transform.InverseTransformDirection(rb.angularVelocity);
		Vector3 vector3 = CalculateInertiaTensorMatrix() * (-vector2 / Time.fixedDeltaTime);
		Vector3 direction = new Vector3(vector3.x * vector.x, vector3.y * vector.y, vector3.z * vector.z);
		Vector3 torque = base.transform.TransformDirection(direction);
		rb.AddTorque(torque);
	}

	private void ApplyLiniarDrag(float amount)
	{
		Vector3 vector = new Vector3(0f, (rb.linearVelocity / Time.fixedDeltaTime * rb.mass).y * amount, 0f);
		rb.AddForce(-vector);
	}

	public Matrix4x4 CalculateInertiaTensorMatrix()
	{
		Matrix4x4 matrix4x = Matrix4x4.Rotate(rb.inertiaTensorRotation);
		Matrix4x4 matrix4x2 = Matrix4x4.Scale(rb.inertiaTensor);
		return matrix4x * matrix4x2 * matrix4x.transpose;
	}

	public void ResetPlane()
	{
		exploder.ResetParts();
		RetractableLandingGear[] componentsInChildren = GetComponentsInChildren<RetractableLandingGear>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Reset();
		}
		base.gameObject.SetActive(value: true);
		Object.Destroy(rb);
		controller.enabled = false;
		PlanePart[] componentsInChildren2 = GetComponentsInChildren<PlanePart>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].enabled = false;
			componentsInChildren2[j].Deactivate();
		}
		base.transform.position = new Vector3(0f, 0f, 0f);
		base.transform.rotation = Quaternion.identity;
		Singleton<DragSimulator>.Instance.UpdateDragFactor();
		controller.ResetController();
		FlightModeInitialized = false;
		Singleton<DecalContainer>.Instance.ResetContainer();
		engines = GetComponentsInChildren<Engine>(includeInactive: true);
		RotorCollider[] componentsInChildren3 = GetComponentsInChildren<RotorCollider>();
		for (int k = 0; k < componentsInChildren3.Length; k++)
		{
			componentsInChildren3[k].Reset();
		}
	}

	public void OnBuildModeLoaded()
	{
		PartPlacer partPlacer = Object.FindFirstObjectByType<PartPlacer>();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			BuildingPart component = base.transform.GetChild(i).gameObject.GetComponent<BuildingPart>();
			if ((bool)component)
			{
				partPlacer.partContainer.AddPart(component);
			}
		}
	}

	public void ActivateFlyMode()
	{
		exploder.ResetParts();
		rb = base.gameObject.AddComponent<Rigidbody>();
		rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		rb.mass = 0f;
		rb.linearDamping = Singleton<DragSimulator>.Instance.dragFactor;
		drag = rb.linearDamping;
		rb.useGravity = false;
		rb.angularDamping = 2f;
		rb.ResetCenterOfMass();
		rb.centerOfMass = CalculateCenterOfMass();
		CenterPlane();
		Physics.SyncTransforms();
		rb.ResetCenterOfMass();
		rb.centerOfMass = CalculateCenterOfMass();
		rb.ResetInertiaTensor();
		controller.ResetController();
		controller.enabled = true;
		planeParts.Clear();
		cargoVolume = 0f;
		fuelCapacity = 1f;
		electricityStorageCapacity = 0.05f;
		hasBalloons = false;
		PlanePart[] componentsInChildren = GetComponentsInChildren<PlanePart>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].rb = rb;
			componentsInChildren[i].enabled = true;
			componentsInChildren[i].Activate();
			if (componentsInChildren[i].GetType() == typeof(Engine) || componentsInChildren[i].GetType() == typeof(Wing) || componentsInChildren[i].GetType() == typeof(Rotator) || componentsInChildren[i].GetType() == typeof(Decoupler) || componentsInChildren[i].GetType() == typeof(AirTurbine) || componentsInChildren[i].GetType() == typeof(Balloon) || componentsInChildren[i].GetType() == typeof(Parachute) || componentsInChildren[i].GetType() == typeof(Airbreak))
			{
				planeParts.Add(componentsInChildren[i]);
			}
			if (componentsInChildren[i].GetType() == typeof(Balloon))
			{
				hasBalloons = true;
			}
			if (componentsInChildren[i].GetType() == typeof(FuelTank))
			{
				fuelCapacity += Mathf.Abs(((FuelTank)componentsInChildren[i]).volume);
			}
			if (componentsInChildren[i].GetType() == typeof(Wing))
			{
				fuelCapacity += Mathf.Abs(((Wing)componentsInChildren[i]).fuel);
			}
			if (componentsInChildren[i].GetType() == typeof(Battery))
			{
				electricityStorageCapacity += Mathf.Abs(((Battery)componentsInChildren[i]).capacity);
			}
			if (componentsInChildren[i].GetType() == typeof(Fuselage))
			{
				cargoVolume += Mathf.Abs(((Fuselage)componentsInChildren[i]).cargoVolume);
			}
			rb.mass += componentsInChildren[i].weight / 15f;
		}
		mass = rb.mass;
		refrenceFuelCapacity = fuelCapacity;
		refrenceElectricityStorageCapacity = electricityStorageCapacity;
		Singleton<CargoInventory>.Instance.ApplyCargo();
		hasRetractableGear = GetComponentInChildren<RetractableLandingGear>() != null;
		cachedLocalForward = base.transform.InverseTransformDirection(UpdateForwardDirection());
		FlightModeInitialized = true;
		controller.UpdateControllSurfaces(rb);
		Singleton<QuestGenerator>.Instance.CacheCurrentDesign();
	}

	public void ReInitializePlane()
	{
		rb.mass = 0f;
		PlanePart[] componentsInChildren = GetComponentsInChildren<PlanePart>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].gameObject.activeInHierarchy)
			{
				rb.mass += componentsInChildren[i].weight / 15f;
			}
		}
		rb.mass += Singleton<CargoInventory>.Instance.GetCargoMass() / 15f;
		mass = rb.mass;
		cargoVolume = GetCargoVolume();
		Singleton<CargoInventory>.Instance.RecalculateCargo();
		rb.centerOfMass = CalculateCenterOfMass();
	}

	public void Refuel()
	{
		fuel = fuelCapacity;
		electricity = electricityStorageCapacity;
	}

	private void CenterPlane()
	{
		Vector3 vector = CalculateCenterOfMass();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			base.transform.GetChild(i).transform.localPosition -= vector;
		}
		Singleton<DecalContainer>.Instance.Recenter(vector);
	}

	public Vector3 CalculateCenterOfMass(bool worldPosition = false)
	{
		Vector3 zero = Vector3.zero;
		float num = 0f;
		PlanePart[] componentsInChildren = GetComponentsInChildren<PlanePart>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			zero += GetPartPosition(componentsInChildren[i].gameObject) * componentsInChildren[i].weight;
			num += componentsInChildren[i].weight;
		}
		Vector3 position = zero / num;
		Vector3 vector = base.transform.InverseTransformPoint(position);
		if (worldPosition)
		{
			return base.transform.TransformPoint(vector);
		}
		return vector;
	}

	public Vector3 UpdateForwardDirection()
	{
		Engine[] componentsInChildren = GetComponentsInChildren<Engine>(includeInactive: true);
		Vector3 forward = base.transform.forward;
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if (child.name.Contains("Cock"))
			{
				forward = child.transform.forward;
			}
		}
		Vector3 zero = Vector3.zero;
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			zero += componentsInChildren[j].GetDirection();
		}
		Vector3 normalized = (zero / componentsInChildren.Length).normalized;
		helicopter = Mathf.Abs(Vector3.Dot(base.transform.up, normalized));
		if (componentsInChildren.Length == 0 || Mathf.Abs(Vector3.Dot(base.transform.up, normalized)) > 0.5f)
		{
			return forward;
		}
		return normalized;
	}

	public Vector3 GetLiftOrigin()
	{
		Vector3 vector = Vector3.Cross(Forward, base.transform.up);
		Vector3 vector2 = base.transform.TransformPoint(rb.centerOfMass);
		float num = Vector3.Dot(GetCenterOfPressure() - vector2, vector);
		return vector2 + vector * num * liftSymmetry;
	}

	public Vector3 GetThrustOrigin()
	{
		Engine[] componentsInChildren = GetComponentsInChildren<Engine>(includeInactive: true);
		if (componentsInChildren.Length == 0)
		{
			if (!(rb != null))
			{
				return base.transform.position;
			}
			return base.transform.TransformPoint(rb.centerOfMass);
		}
		Vector3 zero = Vector3.zero;
		float num = 0f;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			zero += componentsInChildren[i].transform.position;
			num++;
		}
		Vector3 b = zero / num;
		float t = Mathf.Lerp(thrustSymmetry, 0f, helicopter);
		return Vector3.Lerp(base.transform.TransformPoint(rb.centerOfMass), b, t);
	}

	public void UpdateAutoHover()
	{
		Engine[] componentsInChildren = GetComponentsInChildren<Engine>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].autoHoverTargetThrottle = 0f;
		}
		List<Engine> list = new List<Engine>();
		while (list.Count < componentsInChildren.Length)
		{
			Engine item = null;
			float num = float.MinValue;
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				if (!list.Contains(componentsInChildren[j]))
				{
					float num2 = componentsInChildren[j].thrust / componentsInChildren[j].fuelConsumption;
					if (num2 > num)
					{
						num = num2;
						item = componentsInChildren[j];
					}
				}
			}
			list.Add(item);
		}
		float num3 = RealGravity * rb.mass;
		num3 -= Mathf.Abs(currentLift);
		num3 = Mathf.Max(0f, num3);
		num3 -= 0.5f;
		for (int k = 0; k < list.Count; k++)
		{
			if (!(Vector3.Dot(list[k].GetDirection(), base.transform.up) < 0.99f))
			{
				float num4 = Mathf.Clamp01(num3 / list[k].thrust);
				num3 -= num4 * list[k].thrust;
				list[k].autoHoverTargetThrottle = num4;
			}
		}
	}

	private Vector3 GetCenterOfPressure()
	{
		Wing[] componentsInChildren = GetComponentsInChildren<Wing>(includeInactive: false);
		if (componentsInChildren.Length == 0)
		{
			if (!(rb != null))
			{
				return base.transform.position;
			}
			return rb.centerOfMass;
		}
		Vector3 zero = Vector3.zero;
		float num = 0f;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			float num2 = Mathf.Abs(Vector3.Dot(Vector3.up, componentsInChildren[i].orientation.up));
			float num3 = componentsInChildren[i].GetLiftForce() * num2 * num2;
			zero += componentsInChildren[i].GetLiftOrigin(rb) * num3;
			num += num3;
		}
		if (Mathf.Approximately(num, 0f))
		{
			if (!(rb != null))
			{
				return base.transform.position;
			}
			return rb.centerOfMass;
		}
		return zero / num;
	}

	public float GetLowestPosition()
	{
		Vector3 vector = CalculateCenterOfMass(worldPosition: true);
		Bounds rigidBodyBounds = GetRigidBodyBounds();
		float num = vector.y - rigidBodyBounds.center.y;
		return 0f - rigidBodyBounds.extents.y - num;
	}

	public float GetPlaneLength()
	{
		Bounds rigidBodyBounds = GetRigidBodyBounds();
		return Mathf.Max(rigidBodyBounds.extents.x, rigidBodyBounds.extents.z);
	}

	public float GetPlaneHeight()
	{
		return GetRigidBodyBounds().extents.y;
	}

	public float GetCargoVolume()
	{
		float num = 0f;
		PlanePart[] componentsInChildren = GetComponentsInChildren<PlanePart>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].GetType() == typeof(Fuselage) && componentsInChildren[i].gameObject.activeInHierarchy)
			{
				num += Mathf.Abs(((Fuselage)componentsInChildren[i]).cargoVolume);
			}
		}
		return Mathf.RoundToInt(num);
	}

	public float GetVelocityMagintude()
	{
		if (rb == null)
		{
			return 0f;
		}
		return rb.linearVelocity.magnitude;
	}

	public Vector3 GetVelocity()
	{
		if (rb == null)
		{
			return Vector3.zero;
		}
		return rb.linearVelocity;
	}

	public void SetWheelRetraction(bool retracted)
	{
		if (retracted)
		{
			drag = Singleton<DragSimulator>.Instance.dragFactorRetracted;
		}
		else
		{
			drag = Singleton<DragSimulator>.Instance.dragFactor;
		}
	}

	public void ChangeMass(float deltaMass)
	{
		mass += deltaMass / 15f;
		rb.mass += deltaMass / 15f;
	}

	public void ChangeCargoVolume(float deltaCargoVolume)
	{
		cargoVolume += deltaCargoVolume;
	}

	public float GetMass()
	{
		if (rb == null)
		{
			return 0f;
		}
		return rb.mass;
	}

	public float GetLiniarDamping()
	{
		if (rb == null)
		{
			return 0f;
		}
		return rb.linearDamping;
	}

	private Vector3 GetPartPosition(GameObject part)
	{
		if (part.transform.parent != null && !part.transform.parent.GetComponent<PlaneContainer>())
		{
			part = part.transform.parent.gameObject;
		}
		Renderer[] componentsInChildren = part.GetComponentsInChildren<Renderer>(includeInactive: false);
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			zero += componentsInChildren[i].bounds.center;
		}
		return zero / componentsInChildren.Length;
	}

	public Bounds GetRigidBodyBounds()
	{
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: false);
		Bounds result = new Bounds(Vector3.zero, Vector3.zero);
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			zero += componentsInChildren[i].bounds.center;
		}
		result.center = zero / componentsInChildren.Length;
		Renderer[] array = componentsInChildren;
		foreach (Renderer renderer in array)
		{
			result.Encapsulate(renderer.bounds);
		}
		return result;
	}

	public float DistanceFromGround()
	{
		if (rb != null)
		{
			Bounds rigidBodyBounds = GetRigidBodyBounds();
			if (Physics.Raycast(rigidBodyBounds.center, Vector3.down, out var hitInfo, 5000f, groundedCheckLayerMask))
			{
				return hitInfo.distance - rigidBodyBounds.extents.y;
			}
		}
		return float.MaxValue;
	}

	public bool IsGrounded()
	{
		if (DistanceFromGround() < 1f)
		{
			return true;
		}
		return false;
	}

	public bool IsAtAirport()
	{
		Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(base.transform.position);
		return IsAtAirport(closestAirport);
	}

	public Vector3 GetTotalThrust()
	{
		Vector3 zero = Vector3.zero;
		float num = 0f;
		for (int i = 0; i < planeParts.Count; i++)
		{
			if (planeParts[i].GetType() == typeof(Engine))
			{
				zero += ((Engine)planeParts[i]).GetThrustPercentage();
				num++;
			}
		}
		if (Mathf.Approximately(num, 0f))
		{
			return zero;
		}
		return zero / num;
	}

	public float GetPlanePrice()
	{
		float num = 0f;
		BuildingPart[] componentsInChildren = base.gameObject.GetComponentsInChildren<BuildingPart>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			num += componentsInChildren[i].price;
		}
		return num;
	}

	public bool IsAtAirport(Airport airport)
	{
		bool flag = IsGrounded() || (hasBalloons && airport.IsBaseAirport);
		if (airport.flatnessObject == null)
		{
			Debug.LogError("wtf");
		}
		float distanceToRectangle = airport.flatnessObject.GetDistanceToRectangle(base.transform.position);
		float num = GetVelocityMagintude() * (1f - Vector3.Dot(GetVelocity().normalized, Vector3.up));
		bool exploded = controller.Exploded;
		if ((num < 25f && distanceToRectangle < 100f) & flag)
		{
			return !exploded;
		}
		return false;
	}
}
