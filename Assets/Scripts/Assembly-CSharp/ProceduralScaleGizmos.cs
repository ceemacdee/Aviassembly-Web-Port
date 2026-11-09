using UnityEngine;

public class ProceduralScaleGizmos : MonoBehaviour, IGizmo
{
	public ProceduralGizmoManager manager;

	public float offset;

	private bool selected;

	private Renderer[] gizmoRenderer;

	public Color black;

	public Color white;

	public bool highlighted { get; private set; }

	private void Awake()
	{
		gizmoRenderer = GetComponentsInChildren<Renderer>();
	}

	public void ResetOffset(float current)
	{
		offset = current;
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
		for (int i = 0; i < gizmoRenderer.Length; i++)
		{
			gizmoRenderer[i].materials[0].color = manager.black;
			if (selected || highlighted)
			{
				gizmoRenderer[i].materials[0].color = manager.white;
			}
		}
		offset = 0f;
		if (MouseInput.GetMouseButton(0) && selected)
		{
			offset = (MouseInput.GetMouseDelta().x + MouseInput.GetMouseDelta().y) * 0.4f;
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
		for (int i = 0; i < gizmoRenderer.Length; i++)
		{
			Object.Destroy(gizmoRenderer[i].materials[0]);
		}
	}

	public void OnHover()
	{
		if (base.enabled && !manager.GizmoHover())
		{
			highlighted = true;
			if (MouseInput.GetMouseButtonDown(0))
			{
				selected = true;
			}
		}
	}
}
