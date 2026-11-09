using UnityEngine;

public class TooltipLayout : MonoBehaviour
{
	public float spacing;

	public RectTransform canvas;

	public GameObject defaultContent;

	[HideInInspector]
	public RectTransform prefabContent;

	public RectTransform targetRectTransform;

	private RectTransform rectTransform;

	private TooltipArrow arrow;

	private float arrowSize;

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
		arrow = GetComponent<TooltipArrow>();
		arrowSize = 15f;
	}

	public void Update()
	{
		SetPosition(MouseInput.GetMousePosition());
		defaultContent.SetActive(prefabContent == null);
		if (prefabContent != null)
		{
			rectTransform.sizeDelta = new Vector2(prefabContent.sizeDelta.x + spacing * 2f, prefabContent.sizeDelta.y + spacing * 2f);
		}
	}

	private void SetPosition(Vector3 position)
	{
		if (targetRectTransform != null)
		{
			position = targetRectTransform.position;
		}
		rectTransform.position = position;
		Vector2 offset = GetOffset(position, arrowSize);
		rectTransform.anchoredPosition += offset;
		if (arrow != null)
		{
			arrow.UpdateArrow(rectTransform, position, arrowSize);
		}
	}

	private Vector2 GetOffset(Vector3 position, float size)
	{
		size = Mathf.Max(size, 5f);
		float num = ((targetRectTransform != null) ? (targetRectTransform.rect.height / 2f) : 0f);
		float x = rectTransform.rect.width / 2f + size;
		float y = 0f - rectTransform.rect.height / 2f - size - num;
		return new Vector2(x, y);
	}
}
