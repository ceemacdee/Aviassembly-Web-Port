using UnityEngine;

public class ScaleGizmoElement : MonoBehaviour, IGizmo
{
	public Transform direction;

	public bool highlighted;

	public bool selected;

	public Transform origin;

	private Mesh regularMesh;

	private Mesh bigMesh;

	private MeshFilter meshFilter;

	public float offset;

	public float currentOffset;

	private float initialOffset;

	private void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		regularMesh = meshFilter.sharedMesh;
		bigMesh = GetComponent<MeshCollider>().sharedMesh;
	}

	public void UpdateElement(bool highlightable)
	{
		if (!highlightable)
		{
			highlighted = false;
		}
		if (!MouseInput.GetMouseButton(0))
		{
			selected = false;
		}
		meshFilter.sharedMesh = ((selected || highlighted) ? bigMesh : regularMesh);
		highlighted = false;
		UpdateOffset();
	}

	private void UpdateOffset()
	{
		if (MouseInput.GetMouseButton(0) && selected)
		{
			offset = Vector3.Dot(direction.forward, GetProjectedMousePos() - origin.transform.position) - initialOffset;
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
		Ray ray2 = new Ray(origin.transform.position - direction.forward * 50f, direction.forward * 100f);
		return CustomMath.ClosestPointsOnTwoLines(ray2.origin, ray2.direction, ray.origin, ray.direction);
	}

	public bool MouseOverElement()
	{
		if (highlighted || selected)
		{
			return base.gameObject.activeInHierarchy;
		}
		return false;
	}

	public void OnHover()
	{
		highlighted = true;
		if (MouseInput.GetMouseButtonDown(0))
		{
			selected = true;
			float num = Vector3.Dot(direction.forward, GetProjectedMousePos() - origin.transform.position);
			initialOffset = num - currentOffset;
		}
	}
}
