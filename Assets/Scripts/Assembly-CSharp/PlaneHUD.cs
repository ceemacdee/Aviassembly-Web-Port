using UnityEngine;
using UnityEngine.UI;

public class PlaneHUD : MonoBehaviour
{
	public Image fuel;

	public Image electricity;

	public GameObject electricityParent;

	public Gradient fuelColorGradient;

	public Texture fuelIcon;

	public HorizontalLayoutGroup throttleStack;

	public GameObject moneyIndicator;

	public GameObject meters;

	private PlaneContainer planeContainer;

	private float meterScaleRef;

	public void ReloadFlyMode()
	{
		Singleton<GameManager>.Instance.StartFlyMode(resetPlane: true, resetCargo: true);
	}

	private void Start()
	{
		Engine[] componentsInChildren = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<Engine>();
		bool flag = false;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].electricEngine)
			{
				flag = true;
			}
		}
		electricityParent.SetActive(flag);
		throttleStack.spacing = (flag ? 20 : (-20));
	}

	private void LateUpdate()
	{
		if (planeContainer == null)
		{
			planeContainer = Singleton<PlaneContainer>.Instance;
			return;
		}
		float num = Mathf.MoveTowards(fuel.fillAmount, planeContainer.fuel / Mathf.Max(planeContainer.refrenceFuelCapacity, 0.001f), Time.deltaTime * 0.33f);
		fuel.fillAmount = num;
		fuel.color = fuelColorGradient.Evaluate(num);
		float fillAmount = Mathf.MoveTowards(electricity.fillAmount, planeContainer.electricity / Mathf.Max(planeContainer.refrenceElectricityStorageCapacity, 0.001f), Time.deltaTime * 0.33f);
		electricity.fillAmount = fillAmount;
		if (num < 0.01f)
		{
			Singleton<FlightWarningManager>.Instance.ShowWarning("No Fuel", "Try to land", fuelIcon, 10, 3f);
		}
		if (num > 0.01f && num < 0.25f)
		{
			Singleton<FlightWarningManager>.Instance.ShowWarning("Low Fuel", "Find a place to land", fuelIcon, 3, 3f);
		}
		Vector3 spawnPoint = planeContainer.spawnPoint;
		spawnPoint.y = 0f;
		Vector3 position = planeContainer.transform.position;
		position.y = 0f;
		float num2 = Mathf.SmoothDamp(meters.transform.localScale.x, (!moneyIndicator.activeInHierarchy) ? 1 : 0, ref meterScaleRef, 0.1f);
		if (moneyIndicator.activeInHierarchy)
		{
			num2 = 0f;
		}
		meters.transform.localScale = Vector3.one * num2;
	}
}
