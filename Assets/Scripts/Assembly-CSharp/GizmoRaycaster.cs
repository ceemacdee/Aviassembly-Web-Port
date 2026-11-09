using UnityEngine;

public class GizmoRaycaster : MonoBehaviour
{
	public Camera buildingCamera;

	public LayerMask gizmoMask;

	private void Update()
	{
		if (Physics.Raycast(buildingCamera.ScreenPointToRay(MouseInput.GetMousePosition()), out var hitInfo, float.MaxValue, gizmoMask))
		{
			hitInfo.collider.gameObject.GetComponentInChildren<IGizmo>()?.OnHover();
		}
	}
}
