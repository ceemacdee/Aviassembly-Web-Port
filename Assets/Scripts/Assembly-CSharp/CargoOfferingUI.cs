using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CargoOfferingUI : MonoBehaviour
{
	public TMP_Text cargoTypeHeader;

	public RawImage icon;

	public RawImage cargoSpaceIcon;

	public TMP_Text weight;

	public GameObject fragile;

	public TMP_Text stopwatch;

	public TMP_Text cargoSpace;

	public float takeButtonhighlighterFlickerSpeed;

	public GameObject takeButtonHighlighter;

	public Color normal;

	public Color red;

	public TooltipTrigger takeButtonTooltip;

	private CargoType currentCargoType;

	public void SetCargoType(CargoType cargoType)
	{
		cargoTypeHeader.text = (string.IsNullOrEmpty(cargoType.cargoName) ? cargoType.name : cargoType.cargoName);
		icon.texture = cargoType.icon;
		weight.text = cargoType.weight.ToString();
		cargoSpace.text = cargoType.cargoSpace.ToString();
		currentCargoType = cargoType;
	}

	private void LateUpdate()
	{
		bool flag = Singleton<CargoInventory>.Instance.EnoughSpace(currentCargoType);
		cargoSpace.color = (flag ? Color.black : red);
		cargoSpaceIcon.color = (flag ? Color.black : red);
		takeButtonTooltip.enabled = !flag;
		takeButtonHighlighter.SetActive(flag && Singleton<CargoInventory>.Instance.CurrentVolume == 0f && Mathf.Sin(Time.time * takeButtonhighlighterFlickerSpeed) > 0f);
		bool flag2 = Singleton<GameManager>.Instance.gameModeData.currentDifficulty == Difficulty.Relaxed;
		fragile.SetActive(currentCargoType.fragile && !flag2);
		stopwatch.transform.parent.gameObject.SetActive(currentCargoType.expires && !flag2);
		stopwatch.text = currentCargoType.expirationTime.ToString();
	}

	public void TakeCargo()
	{
		Singleton<CargoInventory>.Instance.AddCargo(currentCargoType);
	}

	public void TakeAllCargo()
	{
		for (int i = 0; i < 1000; i++)
		{
			Singleton<CargoInventory>.Instance.AddCargo(currentCargoType);
		}
	}
}
