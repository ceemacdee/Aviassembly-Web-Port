using UnityEngine;

public class TooltipArrow : MonoBehaviour
{
	public RectTransform arrow;

	public RectTransform outline;

	[HideInInspector]
	public bool alignHorizontal;

	public void UpdateArrow(RectTransform tooltip, Vector3 position, float arrowSize)
	{
		arrow.position = position;
		arrow.localPosition = ClampPosition(arrow.localPosition, arrowSize);
		outline.position = arrow.position;
	}

	public Vector3 ClampPosition(Vector3 localPosition, float size)
	{
		RectTransform obj = (RectTransform)base.transform;
		Vector2 min = obj.rect.min;
		Vector2 max = obj.rect.max;
		Vector2 vector = new Vector2(size, 0f);
		if (alignHorizontal)
		{
			vector = new Vector2(0f, size);
		}
		min += vector;
		max -= vector;
		localPosition.x = Mathf.Clamp(localPosition.x, min.x, max.x);
		localPosition.y = Mathf.Clamp(localPosition.y, min.y, max.y);
		return localPosition;
	}
}
