using UnityEngine;

public class ProceduralGizmo : MonoBehaviour
{
	public GizmoMode currentMode;

	public AnimationCurve openAnimation;

	public ProceduralSizeGizmo sizeGizmo;

	public ProceduralScaleGizmos scaleGizmo;

	public ProceduralScaleGizmos roundnessGizmo;

	public ProceduralAxisScaleGizmo xScaleGizmo;

	public ProceduralAxisScaleGizmo yScaleGizmo;

	public ProceduralSizeGizmo xOffset;

	public ProceduralSizeGizmo yOffset;

	public ProceduralScaleGizmos[] corners;

	public GameObject[] roundnessControlls;

	[HideInInspector]
	public Vector3 targetScale;

	private float currentScale;

	private Vector2 unclampedRadius;

	private Vector4 unclampedRoundness;

	private ProceduralFuselageSide lastSide;

	private Vector3 lastRight;

	private Vector3 lastUp;

	private void Start()
	{
	}

	public bool IsHighlighted()
	{
		if (!sizeGizmo.IsHighlighted() && !scaleGizmo.IsHighlighted() && !roundnessGizmo.IsHighlighted() && !xScaleGizmo.IsHighlighted() && !yScaleGizmo.IsHighlighted() && !corners[0].IsHighlighted() && !corners[1].IsHighlighted() && !corners[2].IsHighlighted() && !corners[3].IsHighlighted() && !xOffset.IsHighlighted())
		{
			return yOffset.IsHighlighted();
		}
		return true;
	}

	public void UpdateTransformDirection(Vector3 right, Vector3 up, ProceduralFuselageSide side)
	{
		xOffset.transformDirection = right;
		yOffset.transformDirection = up;
		sizeGizmo.transformDirection = sizeGizmo.transform.forward;
		lastSide = side;
		lastRight = right;
		lastUp = up;
		UpdateRoundnessGizmoPositions(right, up, side);
	}

	private void UpdateRoundnessGizmoPositions(Vector3 right, Vector3 up, ProceduralFuselageSide side)
	{
		Vector2 radius = side.radius;
		radius.x = Mathf.Max(radius.x, 0.155f);
		radius.y = Mathf.Max(radius.y, 0.155f);
		corners[0].transform.position = base.transform.position - right * radius.x + up * radius.y - sizeGizmo.transform.forward * 0.2f;
		corners[1].transform.position = base.transform.position + right * radius.x + up * radius.y - sizeGizmo.transform.forward * 0.2f;
		corners[2].transform.position = base.transform.position + right * radius.x - up * radius.y - sizeGizmo.transform.forward * 0.2f;
		corners[3].transform.position = base.transform.position - right * radius.x - up * radius.y - sizeGizmo.transform.forward * 0.2f;
		roundnessControlls[0].transform.position = base.transform.position + up * (radius.y + 0.0225f) - sizeGizmo.transform.forward * 0.2f;
		roundnessControlls[1].transform.position = base.transform.position - up * (radius.y + 0.0225f) - sizeGizmo.transform.forward * 0.2f;
		roundnessControlls[2].transform.position = base.transform.position + right * (radius.x + 0.0225f) - sizeGizmo.transform.forward * 0.2f;
		roundnessControlls[3].transform.position = base.transform.position - right * (radius.x + 0.0225f) - sizeGizmo.transform.forward * 0.2f;
	}

	public void ResetGizmo(ProceduralFuselageSide fuselageSide)
	{
		sizeGizmo.ResetOffset(fuselageSide.lengthOffset.z);
		xOffset.ResetOffset(fuselageSide.lengthOffset.x);
		yOffset.ResetOffset(fuselageSide.lengthOffset.y);
	}

	public void UpdateAnimation()
	{
		if (base.enabled)
		{
			currentScale += Time.deltaTime * 10f;
		}
		else
		{
			currentScale -= Time.deltaTime * 10f;
		}
		currentScale = Mathf.Clamp01(currentScale);
		float num = ((currentMode == GizmoMode.Roundness) ? currentScale : openAnimation.Evaluate(currentScale));
		base.transform.localScale = targetScale * num;
		UpdateRoundnessGizmoPositions(lastRight, lastUp, lastSide);
	}

	public void SetActive(bool value)
	{
		base.enabled = value;
		sizeGizmo.enabled = value;
		scaleGizmo.enabled = value;
		roundnessGizmo.enabled = value;
		xScaleGizmo.enabled = value;
		yScaleGizmo.enabled = value;
		xOffset.enabled = value;
		yOffset.enabled = value;
		for (int i = 0; i < corners.Length; i++)
		{
			corners[i].enabled = value;
		}
	}

	public void UpdateMode(GizmoMode mode)
	{
		currentMode = mode;
		sizeGizmo.gameObject.SetActive(mode == GizmoMode.Default || mode == GizmoMode.Move);
		scaleGizmo.gameObject.SetActive(mode == GizmoMode.Default || mode == GizmoMode.Scale);
		roundnessGizmo.gameObject.SetActive(mode == GizmoMode.Roundness);
		xScaleGizmo.gameObject.SetActive(mode == GizmoMode.Scale);
		yScaleGizmo.gameObject.SetActive(mode == GizmoMode.Scale);
		xOffset.gameObject.SetActive(mode == GizmoMode.Move);
		yOffset.gameObject.SetActive(mode == GizmoMode.Move);
		for (int i = 0; i < corners.Length; i++)
		{
			corners[i].gameObject.SetActive(mode == GizmoMode.Roundness);
		}
	}

	public void SelectNewPart(ProceduralFuselageSide fuselageSide, Transform fuselageOrigin)
	{
		ResetGizmo(fuselageSide);
		Vector3 objectCenter = CustomMath.GetObjectCenter(fuselageOrigin.gameObject);
		sizeGizmo.origin = objectCenter;
		xOffset.origin = objectCenter;
		yOffset.origin = objectCenter;
		xScaleGizmo.origin = objectCenter;
		yScaleGizmo.origin = objectCenter;
		unclampedRadius = fuselageSide.radius;
		unclampedRoundness = fuselageSide.roundness;
	}

	public void SetGizmoManager(ProceduralGizmoManager manager)
	{
		sizeGizmo.manager = manager;
		scaleGizmo.manager = manager;
		roundnessGizmo.manager = manager;
		xScaleGizmo.manager = manager;
		yScaleGizmo.manager = manager;
		xOffset.manager = manager;
		yOffset.manager = manager;
		for (int i = 0; i < 4; i++)
		{
			corners[i].manager = manager;
		}
	}

	public ProceduralFuselageSide GetUpdatedFuselageSide(ProceduralFuselageSide currentFuselageSide, ProceduralFuselageSide otherSide)
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return currentFuselageSide;
		}
		currentFuselageSide = UpdateLength(currentFuselageSide, otherSide);
		currentFuselageSide = UpdateXOffset(currentFuselageSide);
		currentFuselageSide = UpdateYOffset(currentFuselageSide);
		currentFuselageSide = UpdateScaleIndiviual(currentFuselageSide);
		currentFuselageSide = UpdateScale(currentFuselageSide);
		currentFuselageSide = UpdateRoundness(currentFuselageSide);
		currentFuselageSide = UpdateRoundnessIndividual(currentFuselageSide);
		return currentFuselageSide;
	}

	private ProceduralFuselageSide UpdateLength(ProceduralFuselageSide currentFuselageSide, ProceduralFuselageSide otherSide)
	{
		currentFuselageSide.lengthOffset.z = Mathf.Max(sizeGizmo.offset, 0f - otherSide.lengthOffset.z + 0.1f);
		sizeGizmo.ResetOffset(currentFuselageSide.lengthOffset.z);
		return currentFuselageSide;
	}

	private ProceduralFuselageSide UpdateXOffset(ProceduralFuselageSide currentFuselageSide)
	{
		currentFuselageSide.lengthOffset.x = xOffset.offset;
		xOffset.ResetOffset(currentFuselageSide.lengthOffset.x);
		return currentFuselageSide;
	}

	private ProceduralFuselageSide UpdateYOffset(ProceduralFuselageSide currentFuselageSide)
	{
		currentFuselageSide.lengthOffset.y = yOffset.offset;
		yOffset.ResetOffset(currentFuselageSide.lengthOffset.y);
		return currentFuselageSide;
	}

	private ProceduralFuselageSide UpdateScale(ProceduralFuselageSide currentFuselageSide)
	{
		unclampedRadius.x += scaleGizmo.offset * 0.2f;
		unclampedRadius.y += scaleGizmo.offset * 0.2f;
		unclampedRadius.x = Mathf.Clamp(unclampedRadius.x, 0f, 1.5f);
		unclampedRadius.y = Mathf.Clamp(unclampedRadius.y, 0f, 1.5f);
		Vector2 radius = unclampedRadius;
		radius.x = CustomMath.SnapToIncrement(unclampedRadius.x, 0.1f);
		radius.y = CustomMath.SnapToIncrement(unclampedRadius.y, 0.1f);
		radius.x = Mathf.Max(0.035f, radius.x);
		radius.y = Mathf.Max(0.035f, radius.y);
		currentFuselageSide.radius = radius;
		return currentFuselageSide;
	}

	private ProceduralFuselageSide UpdateScaleIndiviual(ProceduralFuselageSide currentFuselageSide)
	{
		unclampedRadius.x += xScaleGizmo.offset * 0.2f;
		unclampedRadius.y += yScaleGizmo.offset * 0.2f;
		unclampedRadius.x = Mathf.Clamp(unclampedRadius.x, 0f, 1.5f);
		unclampedRadius.y = Mathf.Clamp(unclampedRadius.y, 0f, 1.5f);
		Vector2 radius = unclampedRadius;
		radius.x = CustomMath.SnapToIncrement(unclampedRadius.x, 0.1f);
		radius.y = CustomMath.SnapToIncrement(unclampedRadius.y, 0.1f);
		radius.x = Mathf.Max(0.035f, radius.x);
		radius.y = Mathf.Max(0.035f, radius.y);
		currentFuselageSide.radius = radius;
		return currentFuselageSide;
	}

	private ProceduralFuselageSide UpdateRoundness(ProceduralFuselageSide currentFuselageSide)
	{
		unclampedRoundness -= Vector4.one * roundnessGizmo.offset * 0.2f;
		unclampedRoundness.x = Mathf.Clamp01(unclampedRoundness.x);
		unclampedRoundness.y = Mathf.Clamp01(unclampedRoundness.y);
		unclampedRoundness.z = Mathf.Clamp01(unclampedRoundness.z);
		unclampedRoundness.w = Mathf.Clamp01(unclampedRoundness.w);
		Vector4 roundness = unclampedRoundness;
		roundness.x = CustomMath.SnapToIncrement(unclampedRoundness.x, 0.1f);
		roundness.y = CustomMath.SnapToIncrement(unclampedRoundness.y, 0.1f);
		roundness.z = CustomMath.SnapToIncrement(unclampedRoundness.z, 0.1f);
		roundness.w = CustomMath.SnapToIncrement(unclampedRoundness.w, 0.1f);
		currentFuselageSide.roundness = roundness;
		return currentFuselageSide;
	}

	private ProceduralFuselageSide UpdateRoundnessIndividual(ProceduralFuselageSide currentFuselageSide)
	{
		for (int i = 0; i < 4; i++)
		{
			unclampedRoundness[i] -= corners[(i + 3) % 4].offset * 0.2f;
		}
		unclampedRoundness.x = Mathf.Clamp01(unclampedRoundness.x);
		unclampedRoundness.y = Mathf.Clamp01(unclampedRoundness.y);
		unclampedRoundness.z = Mathf.Clamp01(unclampedRoundness.z);
		unclampedRoundness.w = Mathf.Clamp01(unclampedRoundness.w);
		Vector4 roundness = unclampedRoundness;
		roundness.x = CustomMath.SnapToIncrement(unclampedRoundness.x, 0.1f);
		roundness.y = CustomMath.SnapToIncrement(unclampedRoundness.y, 0.1f);
		roundness.z = CustomMath.SnapToIncrement(unclampedRoundness.z, 0.1f);
		roundness.w = CustomMath.SnapToIncrement(unclampedRoundness.w, 0.1f);
		currentFuselageSide.roundness = roundness;
		return currentFuselageSide;
	}
}
