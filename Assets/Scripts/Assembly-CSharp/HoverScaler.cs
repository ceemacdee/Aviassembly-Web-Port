using UnityEngine;
using UnityEngine.EventSystems;

public class HoverScaler : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public Vector3 startSize;

	public int scaleDirction;

	public float scaleMultiplier = 0.1f;

	private Vector3 targetSize;

	private float t;

	public Transform scaleTransformer;

	private void Awake()
	{
		if (scaleTransformer == null)
		{
			scaleTransformer = base.transform;
		}
		startSize = scaleTransformer.localScale;
	}

	private void OnEnable()
	{
		scaleDirction = -1;
	}

	private void Update()
	{
		t += Time.unscaledDeltaTime * 18f * (float)scaleDirction;
		t = Mathf.Clamp01(t);
		scaleTransformer.localScale = Vector3.Lerp(startSize, startSize * (1f + scaleMultiplier), t);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		scaleDirction = 1;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		scaleDirction = -1;
	}
}
