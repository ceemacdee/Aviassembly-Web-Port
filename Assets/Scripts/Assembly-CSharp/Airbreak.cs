using UnityEngine;

public class Airbreak : PlanePart
{
	public Transform airbreakTransform;

	private float sizeVelocity;

	private float currentSize;

	private void Awake()
	{
		currentSize = 0f;
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
			container.airbreakDrag += 1.25f;
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
			target = 0f;
		}
		currentSize = Mathf.SmoothDamp(currentSize, target, ref sizeVelocity, 0.1f);
		airbreakTransform.localEulerAngles = new Vector3(-90f - 45f * currentSize, -90f, 90f);
	}

	private void OnDisable()
	{
		airbreakTransform.localEulerAngles = new Vector3(-90f, -90f, 90f);
	}

	public override PartStat[] GetPartStats()
	{
		return new PartStat[0];
	}
}
