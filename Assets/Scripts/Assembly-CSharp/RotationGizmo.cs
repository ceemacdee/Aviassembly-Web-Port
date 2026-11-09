using UnityEngine;

public class RotationGizmo : MonoBehaviour
{
	public Transform outline;

	public RotationGizmoElement X;

	public RotationGizmoElement Y;

	public RotationGizmoElement Z;

	private Vector3 currentRotation;

	private Quaternion startRotation;

	public Quaternion openRotation;

	private bool snapping;

	public float snapIncrement { get; private set; }

	private void Awake()
	{
		snapping = true;
		snapIncrement = 90f;
	}

	public void SetElementEnabled(bool x, bool y, bool z)
	{
		X.gameObject.SetActive(x);
		Y.gameObject.SetActive(y);
		Z.gameObject.SetActive(z);
	}

	private void OnEnable()
	{
		startRotation = base.transform.rotation;
	}

	public void LateUpdate()
	{
		bool flag = X.selected || Y.selected || Z.selected;
		X.UpdateElement(!flag);
		Y.UpdateElement(!flag);
		Z.UpdateElement(!flag);
		if (MouseInput.GetMouseButtonDown(0))
		{
			currentRotation = Vector3.zero;
			startRotation = base.transform.rotation;
		}
		if (MouseInput.GetMouseButton(0))
		{
			snapIncrement = Mathf.Max(snapIncrement, 0.01f);
			Vector3 vector = new Vector3(X.GetDisplacement(), Y.GetDisplacement(), Z.GetDisplacement());
			currentRotation += vector;
			Vector3 vector2 = new Vector3(CustomMath.SnapToIncrement(currentRotation.x, snapIncrement), CustomMath.SnapToIncrement(currentRotation.y, snapIncrement), CustomMath.SnapToIncrement(currentRotation.z, snapIncrement));
			if (!snapping)
			{
				vector2 = currentRotation;
			}
			base.transform.rotation = startRotation;
			base.transform.rotation = Quaternion.AngleAxis(vector2.x, X.transform.forward) * base.transform.rotation;
			base.transform.rotation = Quaternion.AngleAxis(vector2.y, Y.transform.forward) * base.transform.rotation;
			base.transform.rotation = Quaternion.AngleAxis(vector2.z, Z.transform.forward) * base.transform.rotation;
		}
		base.transform.localScale = Vector3.one * Vector3.Distance(base.transform.position, BuildingCamera.cam.transform.position) * 0.125f;
		outline.rotation = Quaternion.LookRotation(-BuildingCamera.cam.transform.forward);
	}

	public void SetSnap()
	{
		if (Mathf.Approximately(snapIncrement, 90f))
		{
			snapIncrement = 45f;
		}
		else if (Mathf.Approximately(snapIncrement, 45f))
		{
			snapIncrement = 0f;
		}
		else if (snapIncrement < 9f)
		{
			snapIncrement = 90f;
		}
	}

	public bool MouseInGizmo()
	{
		if (!X.MouseOverElement() && !Y.MouseOverElement())
		{
			return Z.MouseOverElement();
		}
		return true;
	}
}
