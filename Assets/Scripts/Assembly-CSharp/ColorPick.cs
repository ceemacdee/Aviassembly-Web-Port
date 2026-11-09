using UnityEngine;
using UnityEngine.UI;

public class ColorPick : MonoBehaviour
{
	public PartPainter partPainter;

	public Image colorImage;

	public Image selectionImage;

	private Color color;

	public void Init(PartPainter painter, Color color)
	{
		partPainter = painter;
		colorImage.color = color;
		this.color = color;
		Deselect();
	}

	public void Select()
	{
		partPainter.SetCurrentColor(color);
		selectionImage.color = Color.black;
	}

	public void Deselect()
	{
		selectionImage.color = Color.clear;
	}
}
