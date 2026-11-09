using UnityEngine;

public class PlaneTooHeavyWarning : MonoBehaviour
{
	public GameObject warning;

	private float maxThrust;

	private Wing[] wings;

	private PlaneContainer planeContainer;

	private void Start()
	{
		planeContainer = Singleton<PlaneContainer>.Instance;
		wings = planeContainer.gameObject.GetComponentsInChildren<Wing>();
		Engine[] componentsInChildren = planeContainer.gameObject.GetComponentsInChildren<Engine>();
		float num = 0f;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			num += componentsInChildren[i].thrust;
		}
	}

	private void Update()
	{
		float speed = maxThrust / planeContainer.GetMass() * 5f;
		float num = 0f;
		for (int i = 0; i < wings.Length; i++)
		{
			num += wings[i].GetMaxLiftForce(speed, planeContainer);
		}
		_ = planeContainer.RealGravity;
		planeContainer.GetMass();
		warning.gameObject.SetActive(value: false);
	}
}
