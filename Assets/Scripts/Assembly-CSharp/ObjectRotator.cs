using UnityEngine;

public class ObjectRotator : MovingObject
{
	public Vector3 rotationSpeed;

	public override void PhysicsSyncUpdate(float deltaTime)
	{
		base.transform.localEulerAngles += rotationSpeed * deltaTime;
	}
}
