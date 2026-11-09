using UnityEngine;

public class BuildingCamera : Singleton<BuildingCamera>
{
	public float mouseSensitivity;

	public float zoomSensitivity;

	public Transform targetObject;

	public float currentDistance;

	private float mouseMovement;

	private float pitch;

	private float yaw;

	private Vector3 currentOrigin;

	private Vector3 refOrigin;

	private static Vector3 buildCameraPosition;

	private static Quaternion buildCameraRotation;

	private static Vector3 buildCameraOrigin;

	public static bool intialized;

	private PlaneContainer planeContainer;

	[HideInInspector]
	public static Camera cam { get; private set; }

	public bool Rotated { get; private set; }

	private void Start()
	{
		planeContainer = Singleton<PlaneContainer>.Instance;
	}

	protected override void Awake()
	{
		base.Awake();
		cam = GetComponent<Camera>();
		if (intialized)
		{
			base.transform.position = buildCameraPosition;
			base.transform.rotation = buildCameraRotation;
		}
		else
		{
			buildCameraPosition = base.transform.position;
			buildCameraRotation = base.transform.rotation;
			buildCameraOrigin = Vector3.zero;
			intialized = true;
		}
		pitch = base.transform.eulerAngles.x;
		yaw = base.transform.eulerAngles.y;
		currentDistance = (base.transform.position - buildCameraOrigin).magnitude;
		if (base.transform.position.magnitude < 1f)
		{
			currentDistance = 5f;
		}
	}

	public void UpdateOrigin(bool instantant = false)
	{
		if (!(planeContainer.transform.position.magnitude > 5f) && !MouseInput.currentMouse.leftButton.isPressed)
		{
			Vector3 planeCenter = GetPlaneCenter();
			currentOrigin = Vector3.SmoothDamp(currentOrigin, planeCenter, ref refOrigin, instantant ? 0f : 0.33f);
			buildCameraOrigin = planeCenter;
		}
	}

	public void Update()
	{
		if (!Singleton<MouseInput>.Instance.PointerIsOverUI)
		{
			currentDistance -= MouseInput.GetDeltaScroll() * zoomSensitivity;
		}
		currentDistance = Mathf.Max(Mathf.Abs(currentDistance), 1f);
		if (MouseInput.currentMouse.rightButton.isPressed)
		{
			float num = MouseInput.GetMouseDelta().x;
			float num2 = MouseInput.GetMouseDelta().y;
			if (Input.GetKey(KeyCode.K))
			{
				num = Input.GetAxisRaw("Mouse X");
				num2 = Input.GetAxisRaw("Mouse Y");
			}
			yaw += num * 7.5f;
			pitch -= num2 * 7.5f;
			pitch = Mathf.Clamp(pitch, -89f, 89f);
			mouseMovement += Mathf.Abs(num) + Mathf.Abs(num2);
		}
		if (MouseInput.currentMouse.rightButton.wasPressedThisFrame)
		{
			Rotated = false;
			mouseMovement = 0f;
		}
		if (mouseMovement > 5f)
		{
			Rotated = true;
		}
		Vector3 eulerAngles = new Vector3(pitch, yaw);
		base.transform.eulerAngles = eulerAngles;
		base.transform.position = currentOrigin - base.transform.forward * Mathf.Abs(currentDistance);
		buildCameraPosition = base.transform.position;
		buildCameraRotation = base.transform.rotation;
	}

	private Vector3 GetPlaneCenter()
	{
		if (planeContainer.transform.childCount == 0)
		{
			return buildCameraOrigin;
		}
		return planeContainer.GetRigidBodyBounds().center;
	}
}
