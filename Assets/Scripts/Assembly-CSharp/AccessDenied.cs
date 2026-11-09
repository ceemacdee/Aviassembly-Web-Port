using UnityEngine;

public class AccessDenied : MonoBehaviour
{
	public GameObject accessDeniedCanvas;

	public GameObject normalCanvas;

	private void Start()
	{
		normalCanvas.SetActive(value: true);
		accessDeniedCanvas.SetActive(value: false);
	}
}
