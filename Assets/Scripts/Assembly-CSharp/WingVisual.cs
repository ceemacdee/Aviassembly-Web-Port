using UnityEngine;

public class WingVisual : MonoBehaviour
{
	public Transform controlSurface;

	public float maxAngle;

	private float angleVelocity;

	private float targetAngle;

	private float angle;

	private Vector3 localRotation;

	private void Start()
	{
		if (controlSurface != null)
		{
			localRotation = controlSurface.transform.localEulerAngles;
		}
	}

	private void Update()
	{
		angle = Mathf.SmoothDamp(angle, targetAngle, ref angleVelocity, 0.05f);
		if (controlSurface != null)
		{
			controlSurface.transform.localEulerAngles = new Vector3(localRotation.x, localRotation.y, angle);
		}
	}

	public void SetAileron(Vector3 direction)
	{
		if (direction.magnitude > 0.1f)
		{
			targetAngle = maxAngle * (0f - Mathf.Sign(Vector3.Dot(direction, base.transform.up))) * Mathf.Sign(base.transform.localScale.y);
		}
		else
		{
			targetAngle = 0f;
		}
	}
}
