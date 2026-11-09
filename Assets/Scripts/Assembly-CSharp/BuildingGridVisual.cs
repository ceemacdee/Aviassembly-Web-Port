using UnityEngine;

public class BuildingGridVisual : MonoBehaviour
{
	public PartPlacer placer;

	public Renderer renderer1;

	public Renderer renderer2;

	public Transform buildingCamera;

	private Color startColor;

	private void Start()
	{
		startColor = renderer1.material.GetColor("_Color");
	}

	private void Update()
	{
		float t = Mathf.Clamp01(Mathf.Abs(Vector3.Dot((buildingCamera.position - base.transform.position).normalized, Vector3.up)) * 4f);
		renderer1.material.SetColor("_Color", new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(0f, startColor.a, t)));
		renderer2.material.SetColor("_Color", new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(0f, startColor.a, t)));
		if (!(placer.currentMovingPart != null) && Singleton<PlaneContainer>.Instance.transform.childCount != 0)
		{
			Bounds rigidBodyBounds = Singleton<PlaneContainer>.Instance.GetRigidBodyBounds();
			if (Singleton<PlaneContainer>.Instance.transform.childCount < 2)
			{
				Vector3 center = rigidBodyBounds.center;
				base.transform.position = center;
			}
			base.transform.position = new Vector3(base.transform.position.x, rigidBodyBounds.center.y - rigidBodyBounds.extents.y, base.transform.position.z);
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(renderer1.material);
		Object.Destroy(renderer2.material);
	}
}
