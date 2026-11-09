using UnityEngine;

public class HighlightingCanvas : MonoBehaviour
{
	public Canvas highlightParentCanvas;

	public Camera highlightCamera;

	private void Awake()
	{
		Singleton<AiportHighlighterManager>.Instance.InitFlyMode(highlightParentCanvas, highlightCamera);
	}
}
