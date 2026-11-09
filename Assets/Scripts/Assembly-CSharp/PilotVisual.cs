using UnityEngine;

public class PilotVisual : MonoBehaviour
{
	public GameObject pilotHead;

	private void Awake()
	{
		pilotHead.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		pilotHead.SetActive(GameManager.gameMode == GameMode.Flying);
	}
}
