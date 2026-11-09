using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DifficultySelector : MonoBehaviour
{
	public Color deselectedColor;

	public Color selectedColor;

	public Graphic[] backgrounds;

	public TMP_Text[] texts;

	public TMP_Text[] texts2;

	private void Start()
	{
		SelectDifficulty(1);
	}

	public void SelectDifficulty(int difficulty)
	{
		DeselectAll();
		Singleton<GameManager>.Instance.gameModeData.currentDifficulty = (Difficulty)difficulty;
		backgrounds[difficulty].color = selectedColor;
		texts[difficulty].color = Color.white;
		texts2[difficulty].color = Color.white;
	}

	public void DeselectAll()
	{
		for (int i = 0; i < 3; i++)
		{
			backgrounds[i].color = deselectedColor;
			texts[i].color = Color.black;
			texts2[i].color = Color.black;
		}
	}
}
