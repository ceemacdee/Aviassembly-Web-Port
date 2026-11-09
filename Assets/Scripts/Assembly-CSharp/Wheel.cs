using System;
using UnityEngine;

public class Wheel : PlanePart
{
	[Header("Ray")]
	[Space(5f)]
	public LayerMask layerMask;

	public float rayUpOffset;

	public float suspentionLength;

	[Header("Suspension")]
	[Space(5f)]
	public float suspensionStrenth;

	public float suspensionDamping;

	public GameObject stopCollider;

	public GameObject partCollider;

	[Header("Surface Interaction")]
	[Space(5f)]
	public float traction;

	public float maxFriction;

	[Header("Failure")]
	[Space(5f)]
	public float maxWeightCapacity;

	public float maxImpactVelocity;

	private Vector3 localPosition;

	private WheelVisual visual;

	private PlaneController planeController;

	private PartExploder partExploder;

	private float prevHitDistance;

	private RollingAvarage avarageWeightOnWheel;

	public override void UpdatePart(PlaneContainer container)
	{
	}

	private void Awake()
	{
		avarageWeightOnWheel = new RollingAvarage(3f, 0f);
		localPosition = base.transform.localPosition;
		visual = GetComponentInParent<WheelVisual>();
		planeController = Singleton<PlaneContainer>.Instance.gameObject.GetComponent<PlaneController>();
		partExploder = planeController.GetComponent<PartExploder>();
		Deactivate();
		if (stopCollider != null)
		{
			stopCollider.transform.position = base.transform.position + base.transform.up * 0.2f;
		}
	}

	public override void Activate()
	{
		base.Activate();
		partCollider.SetActive(value: false);
		if (stopCollider != null)
		{
			stopCollider.SetActive(value: true);
		}
	}

	public override void Deactivate()
	{
		base.Deactivate();
		partCollider.SetActive(value: true);
		if (stopCollider != null)
		{
			stopCollider.SetActive(value: false);
		}
		if (visual != null)
		{
			visual.ResetSuspension();
		}
	}

	public void ResetPosition()
	{
		base.transform.localPosition = localPosition;
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = new PartStat[2];
		array[0].statName = "Load Limit";
		array[0].SetValue(maxWeightCapacity);
		if (PartPlacer.GetBuildingPartComponent(base.gameObject).GetComponentsInChildren<Wheel>().Length > 1)
		{
			array[0].statValue = "2 x " + Math.Round(maxWeightCapacity, 2);
		}
		array[1].statName = "Friction";
		array[1].SetValue(maxFriction * 10f);
		return array;
	}

	private void FixedUpdate()
	{
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		Vector3 vector = -base.transform.up;
		vector *= Mathf.Sign(base.transform.lossyScale.y);
		if (Physics.Raycast(base.transform.position - vector * rayUpOffset, vector, out var hitInfo, suspentionLength + rayUpOffset, layerMask))
		{
			float num = Mathf.Clamp(hitInfo.distance - rayUpOffset, 0f, suspentionLength);
			float num2 = 1f - num / suspentionLength;
			Vector3 pointVelocity = rb.GetPointVelocity(base.transform.position);
			float num3 = (prevHitDistance - num) / Time.fixedDeltaTime;
			float num4 = suspensionStrenth * instance.GravityMultiplier * num2;
			float num5 = num4 + suspensionDamping * num3;
			rb.AddForceAtPosition(num5 * hitInfo.normal, base.transform.position, ForceMode.Force);
			float num6 = Vector3.Dot(base.transform.right, pointVelocity);
			rb.AddForceAtPosition(-base.transform.right * num6 * traction * num4, base.transform.position, ForceMode.Force);
			Debug.DrawRay(base.transform.position, -base.transform.right * num6 * traction * num4, Color.green);
			Vector3 vector2 = Vector3.Cross(hitInfo.normal, base.transform.right);
			float f = Vector3.Dot(vector2, pointVelocity);
			float num7 = Mathf.Min(Mathf.Abs(f), maxFriction) * Mathf.Sign(f);
			num7 *= Mathf.Lerp(3f, 1f, Mathf.Abs(Vector3.Dot(instance.Forward, instance.GetTotalThrust())));
			rb.AddForceAtPosition(-vector2 * num7 * num4, base.transform.position, ForceMode.Force);
			Debug.DrawRay(base.transform.position, -vector2 * num7 * num4, Color.blue);
			CheckWheelFailure(hitInfo, num5);
			prevHitDistance = num;
			if (visual != null)
			{
				visual.UpdateWheelVisual(hitInfo.point, rb.linearVelocity, base.transform.forward);
			}
		}
		else
		{
			prevHitDistance = suspentionLength;
			avarageWeightOnWheel.Reset();
		}
	}

	private void CheckWheelFailure(RaycastHit hit, float suspensionForce)
	{
		float num = Vector3.Dot(suspensionForce * hit.normal, Vector3.up) / Singleton<PlaneContainer>.Instance.RealGravity * 15f;
		if (num > maxWeightCapacity && avarageWeightOnWheel.GetAvarage(0.75f) > maxWeightCapacity)
		{
			Singleton<WarningManager>.Instance.ShowWarning(this, Singleton<WarningManager>.Instance.weightIcon);
			Singleton<FlightWarningManager>.Instance.ShowWarning("Wheel load too heavy", "Add more wheels or add stronger wheels", Singleton<WarningManager>.Instance.weightIcon, 15, 2.5f);
		}
		float num2 = Vector3.Dot(rb.GetPointVelocity(hit.point), -hit.normal);
		if (num2 < 2f)
		{
			avarageWeightOnWheel.Add(num);
			if (avarageWeightOnWheel.GetMinValue(3f) > maxWeightCapacity)
			{
				partExploder.ExplodePart(this);
			}
		}
		if (num2 > maxImpactVelocity)
		{
			partExploder.ExplodePart(this);
		}
	}

	private void OnDrawGizmos()
	{
		Vector3 vector = -base.transform.up;
		Gizmos.color = new Color(0.9f, 0.3f, 0.3f);
		Gizmos.DrawLine(base.transform.position, base.transform.position + vector * suspentionLength);
	}
}
