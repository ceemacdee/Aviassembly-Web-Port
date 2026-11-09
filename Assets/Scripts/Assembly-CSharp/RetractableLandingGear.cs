using UnityEngine;

public class RetractableLandingGear : MonoBehaviour
{
	public Transform upperSuspension;

	public GameObject struts;

	public Wheel wheelComponent;

	public float targetAngle;

	public float disableAngle;

	public float animationSpeed;

	private WheelVisual visual;

	private Renderer[] renderers;

	private bool retracted;

	public void SetRetracted(bool retracted)
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			for (int num = renderers[i].materials.Length - 1; num >= 0; num--)
			{
				renderers[i].materials[num].SetFloat("_DragFactor", retracted ? 1 : 0);
			}
		}
	}

	private void Awake()
	{
		renderers = GetComponentsInChildren<Renderer>();
		visual = GetComponent<WheelVisual>();
		UpdateMatrix();
	}

	public void UpdateMatrix()
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].materials[0].SetMatrix("_WorldToLocal", base.transform.worldToLocalMatrix);
		}
	}

	public void Reset()
	{
		retracted = false;
		visual.SetToRetractionHeight(retracted: false);
		wheelComponent.enabled = true;
		upperSuspension.localEulerAngles = Quaternion.Euler(upperSuspension.localEulerAngles.x, 0f, upperSuspension.localEulerAngles.z).eulerAngles;
	}

	private void Update()
	{
		UpdateMatrix();
		if (Singleton<InputManager>.Instance.wheelRetraction && GameManager.gameMode == GameMode.Flying)
		{
			retracted = !retracted;
			wheelComponent.enabled = !retracted;
			if (retracted)
			{
				visual.SetToRetractionHeight(retracted: true);
			}
			Singleton<PlaneContainer>.Instance.SetWheelRetraction(retracted);
		}
		if (Mathf.Approximately(upperSuspension.localEulerAngles.y, 0f) && !retracted)
		{
			visual.SetToRetractionHeight(retracted: false);
		}
		Quaternion to = Quaternion.Euler(upperSuspension.localEulerAngles.x, retracted ? targetAngle : 0f, upperSuspension.localEulerAngles.z);
		upperSuspension.localEulerAngles = Quaternion.RotateTowards(upperSuspension.localRotation, to, animationSpeed * Time.deltaTime).eulerAngles;
		bool active = upperSuspension.localEulerAngles.y > disableAngle || upperSuspension.localEulerAngles.y < 10f;
		upperSuspension.gameObject.SetActive(active);
		if (struts != null)
		{
			struts.SetActive(active);
		}
	}

	private void OnDestroy()
	{
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Object.Destroy(componentsInChildren[i].materials[0]);
		}
	}
}
