using UnityEngine;

public class QuestAddedHighlighter : MonoBehaviour
{
	public float moveSpeed;

	public float moveRange;

	public bool vertical;

	private Vector3 startPos;

	private RectTransform trans;

	private void Start()
	{
		trans = (RectTransform)base.transform;
		startPos = trans.anchoredPosition;
	}

	private void Update()
	{
		Vector3 vector = (vertical ? Vector3.up : Vector3.right);
		trans.anchoredPosition = startPos + vector * Mathf.Sin(moveSpeed * Time.time) * moveRange;
	}
}
