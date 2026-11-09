using UnityEngine;

public class RotatingIconGeneration : Singleton<RotatingIconGeneration>
{
	public Transform rotationTable;

	public Transform parent;

	public Camera cam;

	public RenderTexture icon;

	[HideInInspector]
	public GameObject currentObject;

	private GameObject currentPrefab;

	private Vector3 defaultCamPosition;

	private float defaultRotation;

	private Vector3 defaultPosition;

	public void SetObject(GameObject itemPrefab, ItemFraming framing)
	{
		if (!(currentPrefab != null) || !(currentPrefab == itemPrefab))
		{
			currentPrefab = itemPrefab;
			Object.Destroy(currentObject);
			currentObject = Object.Instantiate(itemPrefab);
			currentObject.transform.SetParent(parent, worldPositionStays: true);
			cam.transform.position = defaultCamPosition + cam.transform.forward * (framing.distanceOffset - 0.85f);
			currentObject.transform.localPosition = -GetLocalBoundsCenter(currentObject);
			currentObject.transform.localRotation = Quaternion.identity;
			parent.transform.eulerAngles = framing.rotationOffset;
		}
	}

	private void Start()
	{
		defaultCamPosition = cam.transform.position;
		defaultPosition = parent.transform.position;
		defaultRotation = parent.transform.eulerAngles.y;
	}

	private void Update()
	{
		rotationTable.transform.Rotate(Vector3.up, Time.unscaledDeltaTime * 100f);
	}

	private Vector3 GetLocalBoundsCenter(GameObject obj)
	{
		MeshRenderer[] componentsInChildren = obj.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		if (componentsInChildren == null)
		{
			return obj.transform.position;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			zero += componentsInChildren[i].bounds.center;
		}
		Vector3 position = zero / componentsInChildren.Length;
		return obj.transform.InverseTransformPoint(position) * obj.transform.localScale.x;
	}
}
