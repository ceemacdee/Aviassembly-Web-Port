using UnityEngine;

public class MoveGizmo : MonoBehaviour
{
	public MoveGizmoElement X;

	public MoveGizmoElement Y;

	public MoveGizmoElement Z;

	public void UpdateGizmo()
	{
		X.origin = base.transform;
		Y.origin = base.transform;
		Z.origin = base.transform;
		bool flag = X.selected || Y.selected || Z.selected;
		X.UpdateElement(!flag);
		Y.UpdateElement(!flag);
		Z.UpdateElement(!flag);
		base.transform.localScale = Vector3.one * Vector3.Distance(base.transform.position, BuildingCamera.cam.transform.position) * 0.06f;
	}

	public void SetElementEnabled(bool x, bool y, bool z)
	{
		X.gameObject.SetActive(x);
		Y.gameObject.SetActive(y);
		Z.gameObject.SetActive(z);
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
