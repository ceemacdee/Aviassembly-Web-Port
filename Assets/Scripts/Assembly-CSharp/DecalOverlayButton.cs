using UnityEngine;
using UnityEngine.UI;

public class DecalOverlayButton : MonoBehaviour
{
	public DecalContainer container;

	[Space(10f)]
	public Image buttonImage;

	public GameObject disabledText;

	public Color selectedColor;

	public Color deselectedColor;

	private void Start()
	{
		container = Singleton<DecalContainer>.Instance;
		buttonImage.color = ((!container.decalsHidden) ? selectedColor : deselectedColor);
	}

	private void LateUpdate()
	{
		buttonImage.color = ((!container.decalsHidden) ? selectedColor : deselectedColor);
		disabledText.SetActive(container.decalsHidden);
		((RectTransform)base.transform).sizeDelta = new Vector2(container.decalsHidden ? 300 : 55, 55f);
	}

	public void Press()
	{
		container.SetDecalsHidden(!container.decalsHidden);
	}
}
