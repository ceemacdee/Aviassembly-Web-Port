using UnityEngine;
using UnityEngine.UI;

public class PlaneSaveButton : MonoBehaviour
{
	private Button button;

	private Transform planeContainer;

	private void Awake()
	{
		button = GetComponent<Button>();
		planeContainer = Singleton<PlaneContainer>.Instance.transform;
	}

	private void Update()
	{
		button.interactable = planeContainer.childCount > 0 && PartPlacer.PlaneReady;
	}
}
