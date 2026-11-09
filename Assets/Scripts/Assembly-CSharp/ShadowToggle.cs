using UnityEngine;

public class ShadowToggle : MonoBehaviour
{
	private Light directionalLight;

	private GameManager gameManager;

	private bool prevValue;

	private void Start()
	{
		directionalLight = GetComponent<Light>();
		directionalLight.shadows = (Singleton<GameManager>.Instance.graphicsSettings.shadows ? LightShadows.Soft : LightShadows.None);
		gameManager = Singleton<GameManager>.Instance;
		prevValue = Singleton<GameManager>.Instance.graphicsSettings.shadows;
	}

	private void Update()
	{
		bool shadows = Singleton<GameManager>.Instance.graphicsSettings.shadows;
		if (shadows != prevValue)
		{
			directionalLight.shadows = (Singleton<GameManager>.Instance.graphicsSettings.shadows ? LightShadows.Soft : LightShadows.None);
			prevValue = shadows;
		}
	}
}
