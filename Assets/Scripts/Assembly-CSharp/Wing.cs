using System;
using UnityEngine;

public class Wing : PlanePart
{
	[Space(20f)]
	public Transform orientation;

	public Transform wingTip;

	[HideInInspector]
	public float angleOfAttackOffset;

	[Space(20f)]
	public float liftForce;

	public float dragForce;

	public Vector2 area;

	public float maxSpeed;

	public float fuel;

	[Header("Read-Only")]
	public float currentAngleOfAttack;

	public override void UpdatePart(PlaneContainer container)
	{
		Vector3 pointVelocity = rb.GetPointVelocity(orientation.position);
		Vector3 vector = -pointVelocity;
		Vector3 vector2 = orientation.InverseTransformDirection(vector);
		float num = Mathf.Atan2(vector2.y, Mathf.Abs(vector2.z)) * (0f - Mathf.Sign(vector2.z)) * 57.29578f;
		float num2 = Mathf.Max(angleOfAttackOffset - Mathf.Abs(num), 0f);
		Vector3 lhs = -Vector3.Cross(vector.normalized, orientation.right) * Mathf.Sign(base.transform.localScale.y);
		num = (currentAngleOfAttack = num + num2 * Mathf.Sign(Vector3.Dot(lhs, Vector3.up)));
		ApplyLift(num, vector, pointVelocity, container);
		ApplyDrag(num, pointVelocity);
	}

	public override PartStat[] GetPartStats()
	{
		PartStat[] array = null;
		array = (Mathf.Approximately(fuel, 0f) ? new PartStat[3] : new PartStat[4]);
		array[0].statName = "Lift";
		array[1].statName = "Drag";
		array[2].statName = "Strength";
		array[0].SetValue(Mathf.RoundToInt(liftForce * (area.x * area.y) * 10000f));
		array[1].SetValue(Mathf.RoundToInt(dragForce * (area.x * area.y) * 100000f));
		array[2].SetValue(maxSpeed / 100f);
		if (!Mathf.Approximately(fuel, 0f))
		{
			array[3].statName = "Fuel";
			array[3].SetValue(fuel);
		}
		return array;
	}

	private void ApplyLift(float angleOfAttack, Vector3 airflow, Vector3 velocityAtWing, PlaneContainer container)
	{
		if (airflow.magnitude > maxSpeed * 0.8f)
		{
			Singleton<WarningManager>.Instance.ShowWarning(this);
			Singleton<FlightWarningManager>.Instance.ShowWarning("Wing stress too high", "Slow down to reduce stress", null, 15, 0.1f);
		}
		if (airflow.magnitude > maxSpeed)
		{
			container.gameObject.GetComponent<PartExploder>().ExplodePart(this);
		}
		Vector3 vector = -Vector3.Cross(airflow.normalized, orientation.right);
		float num = Mathf.Sin(Mathf.Abs(angleOfAttack * (MathF.PI / 180f)) * 2f) * Mathf.Sign(angleOfAttack) * Mathf.Pow(velocityAtWing.magnitude, 2f) * GetWingArea() * liftForce;
		float num2 = Mathf.Sign(base.transform.localScale.y);
		Vector3 vector2 = num * vector * num2 * container.GravityMultiplier * container.liftMultiplier;
		Vector3 liftOrigin = container.GetLiftOrigin();
		rb.AddForceAtPosition(vector2, liftOrigin);
		Debug.DrawRay(liftOrigin, vector2, Color.red);
		container.currentLift += liftForce * Mathf.Pow(velocityAtWing.magnitude, 2f) * GetWingArea() * 0.2f;
	}

	private void ApplyDrag(float angleOfAttack, Vector3 velocityAtWing)
	{
		Vector3 vector = -velocityAtWing;
		float num = (0.5f - Mathf.Cos(Mathf.Abs(angleOfAttack) * (MathF.PI / 180f)) * 0.5f) * Mathf.Pow(velocityAtWing.magnitude, 2f) * GetWingArea();
		num *= dragForce;
		Vector3 centerOfMass = rb.centerOfMass;
		Vector3 vector2 = base.transform.parent.TransformPoint(centerOfMass);
		rb.AddForceAtPosition(num * vector, vector2);
		Debug.DrawRay(vector2, vector * num, Color.magenta);
	}

	public float GetMaxLiftForce(float speed, PlaneContainer container)
	{
		float num = Vector3.Dot(Vector3.Cross(container.Forward, orientation.right), container.transform.up);
		float num2 = Mathf.Sin(Mathf.Abs(MathF.PI / 12f) * 2f) * Mathf.Pow(speed, 2f) * GetWingArea() * liftForce;
		float num3 = Mathf.Sign(base.transform.localScale.y);
		return Mathf.Abs(num2 * num3 * container.GravityMultiplier * num * container.liftMultiplier);
	}

	public float GetLiftForce(float speed, float angleOfAttack, PlaneContainer container)
	{
		float num = Mathf.Sin(angleOfAttack * (MathF.PI / 180f) * 2f) * Mathf.Pow(speed, 2f) * GetWingArea() * liftForce;
		float num2 = Mathf.Sign(base.transform.localScale.y);
		return Mathf.Abs(num * num2 * container.GravityMultiplier);
	}

	public float GetLiftForcePoep(float speed, float angleOfAttack, PlaneContainer container)
	{
		float num = Mathf.Sin(angleOfAttack * (MathF.PI / 180f) * 2f) * Mathf.Pow(speed, 2f) * GetWingArea() * liftForce;
		float num2 = Mathf.Sign(base.transform.localScale.y);
		return Mathf.Abs(num * num2 * container.GravityMultiplier);
	}

	public float GetDragForce(float speed, float angleOfAttack)
	{
		return Mathf.Abs((0.5f - Mathf.Cos(angleOfAttack * (MathF.PI / 180f)) * 0.5f) * Mathf.Pow(speed, 2f) * GetWingArea() * dragForce * speed);
	}

	public float GetWingArea()
	{
		return area.x * area.y;
	}

	public float GetLiftForce()
	{
		return GetWingArea() * (liftForce * 100f);
	}

	public Vector3 GetLiftOrigin(Rigidbody rb)
	{
		float t = Mathf.Abs(Vector3.Dot(orientation.transform.up, rb.transform.up));
		Vector3 position = orientation.transform.position;
		return Vector3.Lerp(rb.transform.TransformPoint(rb.centerOfMass), position, t);
	}
}
