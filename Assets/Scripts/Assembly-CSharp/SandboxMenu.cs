using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SandboxMenu : MonoBehaviour
{
	public Texture[] images;

	public Image[] buttons;

	public TMP_Text[] texts;

	public Color selectedColor;

	public Color deselectedColor;

	public RawImage previewImage;

	public Toggle infiniteFuel;

	public Color orange;

	private GameManager gameManager;

	private void Start()
	{
		gameManager = Singleton<GameManager>.Instance;
		gameManager.gameModeData.infiniteFuel = true;
		gameManager.gameModeData.mapType = MapType.Flat;
		infiniteFuel.SetIsOnWithoutNotify(gameManager.gameModeData.infiniteFuel);
	}

	private void Update()
	{
		previewImage.texture = images[(int)Singleton<GameManager>.Instance.gameModeData.mapType];
		for (int i = 0; i < buttons.Length; i++)
		{
			if (Singleton<GameManager>.Instance.gameModeData.mapType == (MapType)i)
			{
				buttons[i].color = selectedColor;
				texts[i].color = Color.white;
			}
			else
			{
				buttons[i].color = deselectedColor;
				texts[i].color = Color.black;
			}
		}
	}

	public void ToggleInfinityFuel(bool value)
	{
		Singleton<GameManager>.Instance.gameModeData.infiniteFuel = value;
	}
}
