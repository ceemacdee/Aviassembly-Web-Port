using System;
using UnityEngine;

public class RotationGizmoElement : MonoBehaviour, IGizmo
{
	public bool highlighted;

	public bool selected;

	private int highlightedFrame;

	private Vector3 mouseDownPoint;

	private Vector3 prevMousePosition;

	private Mesh regularMesh;

	private Mesh bigMesh;

	private MeshFilter meshFilter;

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
		if (MouseInput.GetMouseButtonDown(0) && highlighted)
		{
			selected = true;
			mouseDownPoint = MouseInput.GetMousePosition();
			prevMousePosition = MouseInput.GetMousePosition();
		}
		if (!MouseInput.GetMouseButton(0))
		{
			selected = false;
		}
		meshFilter.sharedMesh = ((selected || highlighted) ? bigMesh : regularMesh);
		highlighted = highlightedFrame == Time.frameCount;
	}

	public float GetDisplacement()
	{
		if (!selected)
		{
			return 0f;
		}
		Vector3 vector = BuildingCamera.cam.WorldToScreenPoint(base.transform.position);
		Vector3 normalized = (BuildingCamera.cam.WorldToScreenPoint(base.transform.position + base.transform.forward) - vector).normalized;
		Vector3 vector2 = MouseInput.GetMousePosition() - prevMousePosition;
		float f = Vector3.Dot(BuildingCamera.cam.transform.forward, base.transform.forward);
		float num = Vector3.Angle((vector - mouseDownPoint).normalized, Vector3.up) % 180f;
		num = Mathf.Min(num, 180f - num);
		float f2 = 1f - num / 90f;
		float num2 = 1f - Mathf.Abs(f2);
		float num3 = Mathf.Sign(Vector3.Dot((vector - mouseDownPoint).normalized, Vector3.right));
		float num4 = 0f - Mathf.Sign(Vector3.Dot((vector - mouseDownPoint).normalized, Vector3.up));
		float a = (vector2.x * (1f - num2) * num4 + vector2.y * num2 * num3) * (0f - Mathf.Sign(f));
		float value = Vector3.Dot(normalized, Vector3.up);
		float num5 = 1f - Math.Abs(value);
		float b = vector2.x * (1f - num5) * (0f - Mathf.Sign(Vector3.Dot(normalized, Vector3.up))) + vector2.y * num5 * Mathf.Sign(Vector3.Dot(normalized, Vector3.right));
		float t = 1f - Mathf.Abs(f);
		float result = Mathf.Lerp(a, b, t);
		prevMousePosition = MouseInput.GetMousePosition();
		return result;
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
		highlightedFrame = Time.frameCount;
	}
}
