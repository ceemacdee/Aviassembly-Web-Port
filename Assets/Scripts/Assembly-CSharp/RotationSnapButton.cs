using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RotationSnapButton : MonoBehaviour
{
	public TMP_Text snapAngleText;

	public RotationGizmo gizmo;

	public RawImage icon;

	public Texture angle45;

	public Texture angle90;

	public Texture angle360;

	private void Start()
	{
		snapAngleText.text = Mathf.RoundToInt(gizmo.snapIncrement).ToString();
		icon.texture = angle90;
	}

	public void Press()
	{
		gizmo.SetSnap();
		snapAngleText.text = Mathf.RoundToInt(gizmo.snapIncrement).ToString();
		if (Mathf.Approximately(gizmo.snapIncrement, 90f))
		{
			icon.texture = angle90;
		}
		if (Mathf.Approximately(gizmo.snapIncrement, 45f))
		{
			icon.texture = angle45;
		}
		if (gizmo.snapIncrement < 9f)
		{
			snapAngleText.text = "-";
			icon.texture = angle360;
		}
	}
}
