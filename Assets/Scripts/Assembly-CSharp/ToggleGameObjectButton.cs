using UnityEngine;

public class ToggleGameObjectButton : MonoBehaviour
{
	public GameObject targetObject;

	public void Press()
	{
		targetObject.SetActive(!targetObject.activeInHierarchy);
	}
}
