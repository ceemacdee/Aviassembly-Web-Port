using UnityEngine;

public class OnHoverTrigger : MonoBehaviour, IGizmo
{
	public MonoBehaviour triggerObject;

	public void OnHover()
	{
		((IGizmo)triggerObject).OnHover();
	}
}
