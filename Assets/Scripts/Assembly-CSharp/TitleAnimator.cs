using UnityEngine;

public class TitleAnimator : MonoBehaviour
{
	public float speed;

	public float amplitude;

	private void Update()
	{
		base.transform.localScale = Vector2.one * ((Mathf.Sin(Time.time * speed) + 1f) * 0.5f * amplitude + 1f);
	}
}
