using UnityEngine;

public class PartPanel : MonoBehaviour
{
	public PartBar[] partPanels;

	public RectTransform panel;

	public PartPlacer placer;

	public float size;

	public float padding;

	public void Update()
	{
		float y = 0f;
		for (int i = 0; i < partPanels.Length; i++)
		{
			if (partPanels[i].gameObject.activeInHierarchy)
			{
				int num = Mathf.CeilToInt((float)partPanels[i].prefabs.Length / 2f);
				y = (float)num * size + padding * 2f + padding * (float)(num - 1);
			}
		}
		panel.sizeDelta = new Vector2(panel.sizeDelta.x, y);
	}
}
