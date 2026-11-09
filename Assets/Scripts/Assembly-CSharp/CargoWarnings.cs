using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CargoWarnings : MonoBehaviour
{
	private CargoInventory cargoInventory;

	private float warningTimer;

	public GameObject warningObject;

	public TMP_Text warningText;

	public RawImage warningIcon;

	public float warningShowTime;

	private void Start()
	{
		warningObject.SetActive(value: false);
		cargoInventory = Singleton<CargoInventory>.Instance;
		cargoInventory.ExpirableCargoCleared += CargoExpired;
		cargoInventory.FragileCargoCleared += CargoBroke;
	}

	private void OnDestroy()
	{
		cargoInventory.ExpirableCargoCleared -= CargoExpired;
		cargoInventory.FragileCargoCleared -= CargoBroke;
	}

	private void Update()
	{
		warningTimer -= Time.deltaTime;
		warningObject.SetActive(warningTimer > 0f);
	}

	private void CargoExpired(CargoType type)
	{
		warningTimer = warningShowTime;
		warningText.text = type.name + " expired";
		warningIcon.texture = type.icon;
	}

	private void CargoBroke(CargoType type, int amount)
	{
		warningTimer = warningShowTime;
		warningText.text = amount + " " + type.name + " broke";
		warningIcon.texture = type.icon;
	}
}
