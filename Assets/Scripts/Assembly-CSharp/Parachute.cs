using UnityEngine;

public class Parachute : PlanePart
{
	public Transform parachuteTransform;

	private float sizeVelocity;

	private float currentSize;

	private void Awake()
	{
		currentSize = 1f;
	}

	private void OnDestroy()
	{
	}

	public override void Activate()
	{
	}

	public override void Deactivate()
	{
	}

	public override void UpdatePart(PlaneContainer container)
	{
		if (Singleton<InputManager>.Instance.throttleInput < 0f && (double)Singleton<PlaneContainer>.Instance.GetTotalThrust().magnitude < 0.01 && Vector3.Dot(Singleton<PlaneContainer>.Instance.GetVelocity(), Singleton<PlaneContainer>.Instance.Forward) > 0f)
		{
			container.airbreakDrag += 1.5f;
		}
	}

	private void Update()
	{
		float target = 0f;
		if (Singleton<InputManager>.Instance.throttleInput < 0f && (double)Singleton<PlaneContainer>.Instance.GetTotalThrust().magnitude < 0.01 && Vector3.Dot(Singleton<PlaneContainer>.Instance.GetVelocity(), Singleton<PlaneContainer>.Instance.Forward) > 0f)
		{
			target = 1f;
		}
		if (GameManager.gameMode == GameMode.Building)
		{
			target = 1f;
		}
		currentSize = Mathf.SmoothDamp(currentSize, target, ref sizeVelocity, 0.1f);
		parachuteTransform.localScale = Vector3.one * currentSize * 75f;
	}

	private void OnDisable()
	{
		parachuteTransform.localScale = Vector3.one * 75f;
	}

	public override PartStat[] GetPartStats()
	{
		return new PartStat[0];
	}
}
