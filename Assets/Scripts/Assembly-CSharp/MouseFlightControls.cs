using UnityEngine;

public class MouseFlightControls : MonoBehaviour
{
	public Transform currentDirectionIndicator;

	public Transform targetDirectionIndicator;

	public Camera flightCam;

	private Vector2 offset;

	private Vector3 prevMousePos;

	private Vector3 targetDirection;

	private void Start()
	{
		prevMousePos = Input.mousePosition;
		targetDirection = Singleton<PlaneContainer>.Instance.Forward;
	}

	private void Update()
	{
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		Cursor.visible = true;
		currentDirectionIndicator.gameObject.SetActive(value: false);
		targetDirectionIndicator.gameObject.SetActive(value: false);
		if (Input.GetMouseButton(0))
		{
			Cursor.visible = false;
			currentDirectionIndicator.gameObject.SetActive(value: true);
			targetDirectionIndicator.gameObject.SetActive(value: true);
			targetDirection = Quaternion.AngleAxis(Input.GetAxisRaw("Mouse X"), flightCam.transform.up) * targetDirection;
			targetDirection = Quaternion.AngleAxis(0f - Input.GetAxisRaw("Mouse Y"), flightCam.transform.right) * targetDirection;
			Vector3 position = instance.transform.position + instance.Forward * 35f;
			Vector3 position2 = instance.transform.position + targetDirection * 35f;
			currentDirectionIndicator.transform.position = flightCam.WorldToScreenPoint(position);
			targetDirectionIndicator.transform.position = flightCam.WorldToScreenPoint(position2);
			float roll = GetRoll(new Vector2(targetDirectionIndicator.transform.position.x - currentDirectionIndicator.transform.position.x, targetDirectionIndicator.transform.position.y - currentDirectionIndicator.transform.position.y));
			if ((bool)instance.gameObject.GetComponent<Rigidbody>())
			{
				instance.gameObject.GetComponent<PlaneController>().ApplyMouseSteering(targetDirection, roll);
			}
		}
	}

	private float GetRoll(Vector2 screenOffset)
	{
		if (screenOffset.magnitude < 100f || Mathf.Abs(screenOffset.x) < 10f)
		{
			return 0f;
		}
		if (Singleton<PlaneContainer>.Instance.DistanceFromGround() < 20f)
		{
			return 0f;
		}
		return Mathf.Lerp(0f, 1f, Mathf.Clamp01(Mathf.Abs(screenOffset.normalized.x) - 0.2f)) * Mathf.Sign(screenOffset.x);
	}
}
