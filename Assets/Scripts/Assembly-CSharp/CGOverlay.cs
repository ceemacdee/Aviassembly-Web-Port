using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CGOverlay : MonoBehaviour
{
	public Transform CGobject;

	public Transform outline;

	public PartPlacer placer;

	public Camera cam;

	public Image buttonImage;

	public TMP_Text text;

	public RawImage icon;

	public Color normal;

	public Color selected;

	private void Start()
	{
		CGobject.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		outline.transform.LookAt(cam.transform.position);
		if (CGobject.gameObject.activeInHierarchy)
		{
			buttonImage.color = selected;
			text.color = Color.white;
			icon.color = Color.white;
		}
		else
		{
			buttonImage.color = normal;
			text.color = Color.black;
			icon.color = Color.black;
		}
		if (!(placer.currentMovingPart != null) && Singleton<PlaneContainer>.Instance.transform.childCount != 0)
		{
			CGobject.transform.position = Singleton<PlaneContainer>.Instance.CalculateCenterOfMass(worldPosition: true);
		}
	}

	public void ToggleOverlay()
	{
		CGobject.gameObject.SetActive(!CGobject.gameObject.activeInHierarchy && Singleton<PlaneContainer>.Instance.transform.childCount > 0);
	}
}
