using UnityEngine;

public class CategoryBar : MonoBehaviour
{
	public GameObject[] partPanels;

	public CategoryButton[] buttons;

	public PartPanel panel;

	public GameObject partBackround;

	private int selectedIndex;

	private void Start()
	{
		Close();
		selectedIndex = -1;
		SelectCategory(0);
	}

	public void SelectCategory(int index)
	{
		if (buttons.Length != partPanels.Length)
		{
			Debug.LogError("buttons and part panel counts don't match");
		}
		else if (index >= 0 && index <= partPanels.Length - 1 && !(partPanels[index] == null))
		{
			for (int i = 0; i < partPanels.Length; i++)
			{
				partPanels[i].gameObject.SetActive(value: false);
				buttons[i].Deselect();
			}
			if (selectedIndex == index)
			{
				selectedIndex = -1;
				partBackround.SetActive(value: false);
				return;
			}
			buttons[index].Select();
			partPanels[index].SetActive(value: true);
			partBackround.SetActive(value: true);
			selectedIndex = index;
			panel.Update();
		}
	}

	public void Close()
	{
		for (int i = 0; i < partPanels.Length; i++)
		{
			partPanels[i].gameObject.SetActive(value: false);
			buttons[i].Deselect();
		}
		partBackround.SetActive(value: false);
	}
}
