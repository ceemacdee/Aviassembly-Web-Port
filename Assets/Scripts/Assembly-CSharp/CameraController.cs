using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;

public class CameraController : MonoBehaviour
{
	public PlaneContainer targetObject;

	public FlyingUIController flyingUIController;

	[Header("Camera settings")]
	public Vector3 offset;

	public float ySmoothTime;

	public float yawSmoothTime;

	public float refrenceSpeed;

	public float maxDistanceOffset;

	public float rotation;

	public float explosionZoomSpeed;

	[Range(0f, 1f)]
	public float maxHeightOffset;

	[Header("Freecam")]
	public CameraMode cameraMode;

	public float zoomSensitivity;

	public float zoomSpeed;

	public float keyboardRotationSensitivity;

	[Header("First person camera")]
	public float firstPersonCameraSensitivity;

	public float firstPersonCameraSpeed;

	private float yVelocity;

	private float currentY;

	private float explosionOffset;

	private float yawVelocity;

	private PlaneController planeController;

	private float targetDistance;

	private float currentDistance;

	private float pitch;

	private float yaw;

	private float firstPersonPitch;

	private float firstpersonYaw;

	private Vector2 rotationOffset;

	private float planeLenght;

	private float planeHeight;

	[Header("Dynamic FOV")]
	public float minFov;

	public float maxFov;

	public float maxFovRefrenceSpeed;

	[Header("Camera shake effect")]
	public bool shake;

	public float shakeAmount;

	public float shakeSpeed;

	private float bloomAmount;

	private PostProcessVolume postProcessVolume;

	private Transform firstPersonCameraTransform;

	public static Camera cam { get; private set; }

	public void SetCameraMode(int cameraMode, Transform firstPersonCamera = null)
	{
		this.cameraMode = (CameraMode)cameraMode;
		firstPersonCameraTransform = firstPersonCamera;
	}

	private void Awake()
	{
		targetObject = Singleton<PlaneContainer>.Instance;
		planeController = targetObject.GetComponent<PlaneController>();
		postProcessVolume = GetComponent<PostProcessVolume>();
		Vector3 targetPosition = GetTargetPosition();
		base.transform.position = targetPosition;
		base.transform.rotation = Quaternion.LookRotation(targetObject.transform.position - base.transform.position);
		cam = GetComponent<Camera>();
		currentDistance = 10f;
		StartCoroutine(LateFixedUpdateManager());
		planeLenght = Singleton<PlaneContainer>.Instance.GetPlaneLength();
		planeHeight = Singleton<PlaneContainer>.Instance.GetPlaneHeight();
		planeLenght *= 1.5f;
		planeHeight *= 1.5f;
		targetDistance = planeLenght * 1.7f;
		explosionOffset = 0f;
		bloomAmount = 1f;
	}

	private void LateFixedUpdate()
	{
		cam.farClipPlane = Singleton<GameManager>.Instance.graphicsSettings.drawDistance;
		if (planeController.Exploded)
		{
			return;
		}
		if (cameraMode == CameraMode.Turntable)
		{
			UpdateFreecam();
		}
		if (cameraMode == CameraMode.Normal)
		{
			Vector3 targetPosition = GetTargetPosition();
			base.transform.rotation = Quaternion.LookRotation(targetObject.transform.position - targetPosition);
			cam.fieldOfView = Mathf.Lerp(minFov, maxFov, Mathf.Clamp01(Singleton<PlaneContainer>.Instance.GetVelocityMagintude() / maxFovRefrenceSpeed));
			currentDistance = Vector3.Distance(targetPosition, Singleton<PlaneContainer>.Instance.transform.position);
			pitch = base.transform.eulerAngles.x;
			yaw = Mathf.SmoothDamp(yaw, yaw + Mathf.DeltaAngle(yaw, base.transform.eulerAngles.y), ref yawVelocity, yawSmoothTime);
			if (Input.GetMouseButton(1) && !flyingUIController.UIOpen && !EventSystem.current.IsPointerOverGameObject())
			{
				float num = MouseInput.GetMouseDelta().x * 7.5f;
				float num2 = MouseInput.GetMouseDelta().y * 7.5f;
				rotationOffset.x += num;
				rotationOffset.y -= num2;
				rotationOffset.x = Mathf.DeltaAngle(0f, rotationOffset.x);
				rotationOffset.y = Mathf.DeltaAngle(0f, rotationOffset.y);
			}
			else
			{
				rotationOffset -= new Vector2(rotationOffset.x * Time.deltaTime * 10f, rotationOffset.y * Time.deltaTime * 10f);
			}
			Vector3 eulerAngles = new Vector3(pitch, yaw) + new Vector3(rotationOffset.y, rotationOffset.x, 0f);
			base.transform.eulerAngles = eulerAngles;
			float num3 = Mathf.Clamp01(Singleton<PlaneContainer>.Instance.GetVelocityMagintude() / refrenceSpeed) * maxDistanceOffset;
			float num4 = Mathf.Abs(currentDistance) + num3;
			base.transform.position = Singleton<PlaneContainer>.Instance.transform.position - base.transform.forward * num4;
			base.transform.rotation = base.transform.rotation * Quaternion.AngleAxis(rotation, Vector3.Cross(Singleton<PlaneContainer>.Instance.Forward, planeController.transform.up).normalized);
			ShakeCamera();
		}
	}

	private void ShakeCamera()
	{
		if (shake)
		{
			Vector3 vector = new Vector3(Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) * 2f - 1f, Mathf.PerlinNoise(0f, Time.time * shakeSpeed) * 2f - 1f);
			base.transform.localPosition += vector * shakeAmount;
			cam.fieldOfView *= 1f - Mathf.PerlinNoise(Time.time * shakeSpeed * 5f, 0f) * 0.1f * shakeAmount;
		}
	}

	private Vector3 GetTargetPosition()
	{
		Vector3 forward = targetObject.Forward;
		currentY = Mathf.SmoothDamp(currentY, forward.y, ref yVelocity, ySmoothTime);
		currentY = Mathf.Lerp(currentY, 0f, targetObject.helicopter);
		forward.y = currentY;
		forward.Normalize();
		float t = 1f - Mathf.Clamp01(Singleton<PlaneContainer>.Instance.GetVelocityMagintude() / refrenceSpeed);
		t = Mathf.Lerp(maxHeightOffset, 1f, t);
		Vector3 normalized = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
		return targetObject.transform.position - forward * (offset.z + planeLenght) + normalized * (offset.y + planeHeight) * t;
	}

	private IEnumerator LateFixedUpdateManager()
	{
		while (true)
		{
			LateFixedUpdate();
			yield return new WaitForFixedUpdate();
		}
	}

	private void Update()
	{
		if (planeController.Exploded)
		{
			Vector3 normalized = (targetObject.transform.position - base.transform.position).normalized;
			base.transform.position -= normalized * Time.deltaTime * explosionZoomSpeed;
			postProcessVolume.profile.TryGetSettings<Bloom>(out var outSetting);
			outSetting.active = true;
			outSetting.intensity.value = Mathf.Lerp(0f, 15f, bloomAmount);
			bloomAmount -= Time.deltaTime;
			return;
		}
		if (cameraMode == CameraMode.Turntable && Time.timeScale < 0.5f)
		{
			UpdateFreecam();
		}
		if (cameraMode == CameraMode.FirstPerson)
		{
			UpdateFirstPersonCamera();
		}
		if (cameraMode == CameraMode.Lookat)
		{
			UpdateLookAtCamera();
		}
	}

	private void UpdateFreecam()
	{
		targetDistance -= Input.mouseScrollDelta.y * zoomSensitivity;
		currentDistance = Mathf.MoveTowards(currentDistance, targetDistance, Time.unscaledDeltaTime * Mathf.Abs(targetDistance - currentDistance) * zoomSpeed);
		if (Input.GetMouseButton(1))
		{
			float axis = Input.GetAxis("Mouse X");
			float axis2 = Input.GetAxis("Mouse Y");
			yaw += axis;
			pitch -= axis2;
			pitch = Mathf.Clamp(pitch, -89f, 89f);
		}
		float num = (Input.GetKey(KeyCode.I) ? 1 : 0) + (Input.GetKey(KeyCode.P) ? (-1) : 0);
		yaw += num * keyboardRotationSensitivity * Time.unscaledDeltaTime;
		Vector3 eulerAngles = new Vector3(pitch, yaw);
		base.transform.eulerAngles = eulerAngles;
		base.transform.position = Singleton<PlaneContainer>.Instance.transform.position - base.transform.forward * Mathf.Abs(currentDistance);
	}

	private void UpdateFirstPersonCamera()
	{
		base.transform.position = firstPersonCameraTransform.position + firstPersonCameraTransform.forward * 0.4f + firstPersonCameraTransform.up * 0.33f * Mathf.Sign(firstPersonCameraTransform.localScale.y);
		base.transform.rotation = Quaternion.LookRotation(firstPersonCameraTransform.up * Mathf.Sign(firstPersonCameraTransform.localScale.y), targetObject.transform.up);
	}

	private void UpdateLookAtCamera()
	{
		base.transform.rotation = Quaternion.LookRotation(targetObject.transform.position - base.transform.position);
	}
}
