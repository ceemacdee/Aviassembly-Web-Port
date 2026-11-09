using UnityEngine;

public class ProceduralAxisScaleGizmo : MonoBehaviour, IGizmo
{
	public ProceduralGizmoManager manager;

	public float sensitivity;

	public float offset;

	public float prevProjectedMousePos;

	public Vector3 origin;

	private bool selected;

	private Renderer gizmoRenderer;

	public bool highlighted { get; private set; }

	private void Awake()
	{
		gizmoRenderer = GetComponent<Renderer>();
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
		offset = 0f;
		if (MouseInput.GetMouseButton(0) && selected)
		{
			float num = Vector3.Dot(base.transform.forward, GetProjectedMousePos());
			offset = (num - prevProjectedMousePos) * sensitivity;
			prevProjectedMousePos = num;
			manager.gizmoPosition = base.transform.position;
		}
		if (MouseInput.GetMouseButtonUp(0))
		{
			selected = false;
		}
		highlighted = false;
	}

	private Vector3 GetProjectedMousePos()
	{
		Ray ray = BuildingCamera.cam.ScreenPointToRay(MouseInput.GetMousePosition());
		Ray ray2 = new Ray(origin - base.transform.forward * 50f, base.transform.forward * 100f);
		return CustomMath.ClosestPointsOnTwoLines(ray2.origin, ray2.direction, ray.origin, ray.direction);
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

	public void OnHover()
	{
		if (base.enabled && !manager.GizmoHover())
		{
			highlighted = true;
			if (MouseInput.GetMouseButtonDown(0))
			{
				selected = true;
				float num = Vector3.Dot(base.transform.forward, GetProjectedMousePos());
				prevProjectedMousePos = num;
			}
		}
	}
}
