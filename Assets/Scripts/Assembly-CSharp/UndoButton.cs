using UnityEngine;
using UnityEngine.UI;

public class UndoButton : MonoBehaviour
{
	public RawImage image;

	public void Undo()
	{
		Singleton<PlaneStorage>.Instance.Undo();
		image.color = Color.black;
	}

	public void Update()
	{
		if (!PointerInRect())
		{
			image.color = Color.black;
		}
	}

	private bool PointerInRect()
	{
		return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)base.transform, MouseInput.GetMousePosition());
	}

	public void Click()
	{
		image.color = Color.white;
	}
}
