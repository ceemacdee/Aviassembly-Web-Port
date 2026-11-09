using UnityEngine;

public class ProceduralSizeGizmo : MonoBehaviour, IGizmo
{
	public ProceduralGizmoManager manager;

	public float offset;

	public float currentOffset;

	public Vector3 origin;

	private bool selected;

	private Renderer gizmoRenderer;

	private float ignoreOffset;

	private float offsetOnClick;

	public Vector3 transformDirection;

	public bool highlighted { get; private set; }

	private void Awake()
	{
		gizmoRenderer = GetComponent<Renderer>();
	}

	public void ResetOffset(float current)
	{
		offset = current;
		currentOffset = current;
	}

	public bool IsHighlighted()
	{
		if (!highlighted)
		{
			return selected;
		}
		return true;
	}

	private void LateUpdate()
	{
		gizmoRenderer.materials[0].color = manager.black;
		if (selected || highlighted)
		{
			gizmoRenderer.materials[0].color = manager.white;
		}
		if (MouseInput.GetMouseButton(0) && selected)
		{
			float value = Vector3.Dot(transformDirection, GetProjectedMousePos() - origin) - offsetOnClick - ignoreOffset;
			value = CustomMath.SnapToIncrement(value, 0.1f);
			offset = offsetOnClick + value;
			manager.gizmoPosition = base.transform.position;
		}
		if (MouseInput.GetMouseButtonUp(0))
		{
			selected = false;
		}
		highlighted = false;
	}

	public void OnDisable()
	{
		highlighted = false;
		selected = false;
	}

	private void OnDestroy()
	{
		OnDisable();
		Object.Destroy(gizmoRenderer.materials[0]);
	}

	private Vector3 GetProjectedMousePos()
	{
		Ray ray = BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition());
		Ray ray2 = new Ray(origin - transformDirection * 50f, transformDirection * 100f);
		return CustomMath.ClosestPointsOnTwoLines(ray2.origin, ray2.direction, ray.origin, ray.direction);
	}

	public void OnHover()
	{
		if (base.enabled)
		{
			highlighted = true;
			if (MouseInput.GetMouseButtonDown(0))
			{
				selected = true;
				float num = Vector3.Dot(transformDirection, GetProjectedMousePos() - origin);
				ignoreOffset = num - currentOffset;
				offsetOnClick = currentOffset;
			}
		}
	}
}
