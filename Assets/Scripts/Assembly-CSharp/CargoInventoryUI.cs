using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CargoInventoryUI : MonoBehaviour
{
	public const float RequiredDistanceFromAirport = 100f;

	public const float RequiredVelocity = 25f;

	public FlyingUIController flyingUIController;

	public CargoInventory cargoInventory;

	public TMP_Text cargoTypeText;

	[Space(15f)]
	public AirportInspector airportInspector;

	public QuestAdditionUI questAdditionUI;

	public TMP_Text cargoVolumeText;

	public AirportMoneyIndicator airportMoneyIndicator;

	public GameObject takeButtonHighlighter;

	public float takeButtonhighlighterFlickerSpeed;

	public GameObject airportInventoryWindow;

	public GameObject cargoInventoryWindow;

	public Image cargoVolumeBar;

	public RawImage airportCargoIcon;

	public GameObject expirationTimer;

	public RawImage expirationIcon;

	public Image expirationBar;

	public TMP_Text airportCargoWeightText;

	public TMP_Text airportCargoSpaceText;

	public TMP_Text planeWeight;

	public CargoInventorySlot[] slots;

	public CargoOfferingUI[] cargoOfferings;

	private bool airportUI;

	private void Start()
	{
		cargoInventory = Singleton<CargoInventory>.Instance;
	}

	private void Update()
	{
		planeWeight.text = Math.Round(Singleton<PlaneContainer>.Instance.GetMass() * 15f, 1).ToString();
		cargoVolumeBar.fillAmount = cargoInventory.CurrentVolume / (float)Mathf.RoundToInt(cargoInventory.MaxVolume);
		cargoVolumeText.text = (int)cargoInventory.CurrentVolume + " / " + Mathf.RoundToInt(cargoInventory.MaxVolume);
		int num = 0;
		foreach (KeyValuePair<CargoType, int> item in cargoInventory.GetCurrentCargo())
		{
			float num2 = cargoInventory.GetCargoCount(item.Key);
			if (num2 != 0f)
			{
				slots[num].gameObject.SetActive(value: true);
				slots[num].Init(item.Key.icon, num2);
				num++;
			}
		}
		for (int i = num; i < slots.Length; i++)
		{
			slots[i].gameObject.SetActive(value: false);
		}
		bool flag = false;
		Rigidbody component = Singleton<PlaneContainer>.Instance.GetComponent<Rigidbody>();
		bool active = false;
		if (component != null)
		{
			Airport closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(component.transform.position);
			flag = Singleton<PlaneContainer>.Instance.IsAtAirport();
			airportInspector.SetEnabled(flag, flyingUIController.lockUI);
			airportInspector.SetAirport(closestAirport);
			for (int j = 0; j < cargoOfferings.Length; j++)
			{
				bool flag2 = (closestAirport.cargoType != null && closestAirport.cargoType.Length > j) & flag;
				cargoOfferings[j].gameObject.SetActive(flag2);
				if (flag2)
				{
					cargoOfferings[j].SetCargoType(closestAirport.cargoType[j]);
					active = true;
				}
			}
		}
		airportInventoryWindow.gameObject.SetActive(active);
		airportMoneyIndicator.SetEnable((flag || cargoVolumeBar.gameObject.activeInHierarchy) && !questAdditionUI.IsOpen(), flyingUIController.lockUI);
		bool active2 = cargoInventory.ContainsExpirableCargo() && Singleton<GameManager>.Instance.gameModeData.currentDifficulty != Difficulty.Relaxed;
		expirationTimer.SetActive(active2);
		expirationBar.fillAmount = cargoInventory.GetExpirationProgress();
		expirationIcon.texture = cargoInventory.GetExpirationIcon();
	}

	public void ClearInventory()
	{
		Singleton<CargoInventory>.Instance.ClearInventory();
	}

	public void Refuel()
	{
		Singleton<PlaneContainer>.Instance.Refuel();
	}

	public void OpenAirportInventory(bool airport)
	{
		airportUI = airport;
	}
}
