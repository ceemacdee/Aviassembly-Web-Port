using UnityEngine;

public class ScaleGizmo : MonoBehaviour
{
	public ScaleGizmoElement X;

	public ScaleGizmoElement Y;

	public ScaleGizmoElement Z;

	public ScaleGizmoCenter Center;

	public Vector3 currentScale;

	public Vector3 startScale;

	public void ResetScaleOffset(Vector3 startScale)
	{
		this.startScale = startScale;
		X.offset = 0f;
		Y.offset = 0f;
		Z.offset = 0f;
		Center.offset = 0f;
	}

	public void SetElementEnabled(bool x, bool y, bool z)
	{
		X.gameObject.SetActive(x);
		Y.gameObject.SetActive(y);
		Z.gameObject.SetActive(z);
	}

	public void UpdateGizmo()
	{
		X.origin = base.transform;
		Y.origin = base.transform;
		Z.origin = base.transform;
		bool flag = X.selected || Y.selected || Z.selected || Center.selected;
		X.UpdateElement(!flag);
		Y.UpdateElement(!flag);
		Z.UpdateElement(!flag);
		Center.UpdateElement(!flag);
		currentScale = startScale + new Vector3(X.offset, Y.offset, Z.offset) + Center.offset * Vector3.one;
		if (MouseInput.GetMouseButtonUp(0))
		{
			startScale = currentScale;
			X.offset = 0f;
			Y.offset = 0f;
			Z.offset = 0f;
			Center.offset = 0f;
		}
	}

	public bool MouseInGizmo()
	{
		if (!X.MouseOverElement() && !Y.MouseOverElement() && !Z.MouseOverElement())
		{
			return Center.MouseOverElement();
		}
		return true;
	}
}
