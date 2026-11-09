using UnityEngine;

public class ScaleGizmoCenter : MonoBehaviour, IGizmo
{
	public bool highlighted;

	public bool selected;

	public Vector3 clickPosition;

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
			Vector3 vector = GetMousePos() - clickPosition;
			offset = (vector.x + vector.y) * 3f;
		}
		if (MouseInput.GetMouseButtonUp(0))
		{
			selected = false;
		}
		highlighted = false;
	}

	private Vector3 GetMousePos()
	{
		return BuildingCamera.cam.ScreenToViewportPoint(MouseInput.GetMousePosition());
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
			clickPosition = GetMousePos();
		}
	}
}
