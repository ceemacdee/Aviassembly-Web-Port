using UnityEngine;

public class EagleFlight : MovingObject
{
	public float flightSpeed;

	public float animationSpeed;

	private Vector3 forwardVector;

	private Transform cam;

	private float speed;

	private void Start()
	{
		GetComponent<Animation>()["Armature|ArmatureAction.001"].speed = animationSpeed;
	}

	public void Init(Transform cam)
	{
		forwardVector = base.transform.forward;
		speed = Random.Range(flightSpeed * 0.9f, flightSpeed * 1.33f);
		this.cam = cam;
	}

	public override void PhysicsSyncUpdate(float deltaTime)
	{
		Vector3 position = base.transform.position + forwardVector * deltaTime * speed;
		base.transform.position = position;
		if (Vector3.Distance(cam.position, base.transform.position) > 4000f)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
