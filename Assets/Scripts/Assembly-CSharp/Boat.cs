using UnityEngine;

public class Boat : MovingObject
{
	public float rotationAmount;

	public float rotationSpeed;

	public float upDownSpeed;

	public float upDownAmount;

	private float time;

	public new void PhysicsSyncUpdate(float deltaTime)
	{
		time += deltaTime;
		base.transform.eulerAngles = new Vector3(Mathf.Sin(time * rotationSpeed) * rotationAmount, 0f, 0f);
		base.transform.position += Vector3.up * Mathf.Sin(time * upDownSpeed) * upDownAmount;
	}
}
