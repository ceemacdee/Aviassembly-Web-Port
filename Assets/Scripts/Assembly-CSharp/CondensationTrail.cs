using UnityEngine;

public class CondensationTrail : MonoBehaviour
{
	public GameObject trailPrefab;

	public Vector3 wingTip;

	private TrailRenderer trail;

	private PlaneContainer planeContainer;

	private void Start()
	{
		planeContainer = Singleton<PlaneContainer>.Instance;
		GameManager.buildModeLoaded += DeActivate;
		GameManager.flyModeLoaded += Activate;
	}

	private void Activate()
	{
		planeContainer = Singleton<PlaneContainer>.Instance;
		if (isClosest())
		{
			GameObject gameObject = Object.Instantiate(trailPrefab);
			trail = gameObject.GetComponent<TrailRenderer>();
		}
	}

	private void DeActivate()
	{
		if (trail != null)
		{
			Object.Destroy(trail.gameObject);
		}
	}

	private void OnDisable()
	{
		DeActivate();
	}

	private void Update()
	{
		if (trail == null && GameManager.gameMode == GameMode.Flying)
		{
			Activate();
		}
		if (!(trail == null))
		{
			trail.transform.position = base.transform.TransformPoint(wingTip);
			float num = Mathf.Max(planeContainer.GetVelocityMagintude() - 100f, 0f) / 1500f;
			num *= 0.33f;
			Color startColor = new Color(1f, 1f, 1f, num);
			Color endColor = new Color(1f, 1f, 1f, 0f);
			trail.startColor = startColor;
			trail.endColor = endColor;
			trail.gameObject.SetActive(planeContainer.gameObject.activeInHierarchy);
		}
	}

	private float GetDistance(Vector3 worldWingtip)
	{
		Vector3 forward = planeContainer.Forward;
		Vector3 vector = Vector3.ProjectOnPlane(worldWingtip, forward);
		Vector3 vector2 = Vector3.ProjectOnPlane(planeContainer.transform.position, forward);
		Vector3 normalized = Vector3.ProjectOnPlane(Vector3.Cross(planeContainer.transform.up, forward), forward).normalized;
		Debug.DrawLine(planeContainer.transform.position, planeContainer.transform.position + normalized);
		float num = Vector3.Dot((vector - vector2).normalized, normalized);
		return Vector3.Distance(vector, vector2) * num;
	}

	private bool isClosest()
	{
		float num = float.MinValue;
		float num2 = float.MaxValue;
		bool flag = false;
		bool flag2 = false;
		CondensationTrail[] componentsInChildren = planeContainer.gameObject.GetComponentsInChildren<CondensationTrail>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			float distance = GetDistance(componentsInChildren[i].GetWorldWingtip());
			if (distance > num)
			{
				num = distance;
				flag2 = componentsInChildren[i] == this;
			}
			if (distance < num2)
			{
				num2 = distance;
				flag = componentsInChildren[i] == this;
			}
		}
		return flag | flag2;
	}

	public Vector3 GetWorldWingtip()
	{
		return base.transform.TransformPoint(wingTip);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.TransformPoint(wingTip), 0.1f);
	}
}
