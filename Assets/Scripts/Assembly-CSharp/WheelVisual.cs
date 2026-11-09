using UnityEngine;

public class WheelVisual : MonoBehaviour
{
	[Header("Wheel rotation")]
	public Transform wheel;

	public Transform secondWheel;

	public float rotationSpeed;

	[Space(15f)]
	[Header("Suspension")]
	public Transform lowerSuspension;

	public float upperSuspensionLength;

	public float lowerSuspensionLength;

	public float retractionHeight;

	private int lastUpdateFrame;

	private float targetHeight;

	private float heightVelocity;

	private void Start()
	{
		ResetSuspension();
	}

	public void UpdateWheelVisual(Vector3 worldContactPoint, Vector3 velocity, Vector3 forwardVector)
	{
		if (lowerSuspension != null)
		{
			float value = 0f - (0f - lowerSuspension.parent.InverseTransformPoint(worldContactPoint).y - lowerSuspensionLength);
			value = Mathf.Clamp(value, 0f - upperSuspensionLength, 0f);
			lowerSuspension.localPosition = new Vector3(lowerSuspension.localPosition.x, value, lowerSuspension.localPosition.z);
		}
		if (wheel != null)
		{
			float num = Vector3.Dot(velocity, forwardVector);
			wheel.transform.rotation *= Quaternion.Euler(0f, 0f, num * rotationSpeed * Time.fixedDeltaTime);
		}
		if (secondWheel != null)
		{
			secondWheel.transform.rotation = wheel.rotation;
		}
		lastUpdateFrame = Time.frameCount;
	}

	private void FixedUpdate()
	{
		if (!(lowerSuspension == null) && (Time.frameCount - lastUpdateFrame >= 2 || Mathf.Approximately(targetHeight, retractionHeight)))
		{
			float y = Mathf.SmoothDamp(lowerSuspension.localPosition.y, targetHeight, ref heightVelocity, 0.2f);
			lowerSuspension.localPosition = new Vector3(lowerSuspension.localPosition.x, y, lowerSuspension.localPosition.z);
		}
	}

	public void ResetSuspension()
	{
		if (lowerSuspension != null)
		{
			float y = Mathf.Lerp(0f, 0f - upperSuspensionLength, 0.5f);
			lowerSuspension.localPosition = new Vector3(lowerSuspension.localPosition.x, y, lowerSuspension.localPosition.z);
			targetHeight = y;
		}
	}

	public void SetToRetractionHeight(bool retracted)
	{
		if (lowerSuspension != null)
		{
			if (retracted)
			{
				targetHeight = retractionHeight;
			}
			else
			{
				targetHeight = Mathf.Lerp(0f, 0f - upperSuspensionLength, 0.5f);
			}
		}
	}
}
