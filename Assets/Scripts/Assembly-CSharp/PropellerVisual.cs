using UnityEngine;

public class PropellerVisual : MonoBehaviour
{
	public Transform propeller;

	public float propSpeed;

	public GameObject backside;

	public RotorCollider rotorCollider;

	private Quaternion defaultRotation;

	private float currentAngle;

	private void Awake()
	{
		if (propeller != null)
		{
			defaultRotation = propeller.localRotation;
		}
	}

	public void ResetPropeller()
	{
		if (propeller != null)
		{
			propeller.localRotation = defaultRotation;
			currentAngle = 0f;
		}
	}

	public void UpdatePropeller(float input)
	{
		float num = input * propSpeed * Time.deltaTime;
		if (propeller != null)
		{
			propeller.rotation *= Quaternion.Euler(num, 0f, 0f);
		}
		if (rotorCollider != null)
		{
			currentAngle = (currentAngle + num) % 360f;
			rotorCollider.rotorRotation = currentAngle;
		}
	}
}
