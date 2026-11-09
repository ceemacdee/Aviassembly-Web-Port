using UnityEngine;

public class CustomFog : MonoBehaviour
{
	public float profileLerpTime;

	public bool useFog;

	public Color fogColor;

	private float fogFactor;

	public float fogOffset;

	public float heightRefrence;

	public float exponent;

	public Color skyColor;

	public FogProfile defaultFogProfile;

	private FogProfile targetFogProfile;

	private float lerp;

	public void SetFogProfile(FogProfile fogProfile)
	{
		targetFogProfile = fogProfile;
		lerp = 0f;
	}

	private void Update()
	{
		float value = fogFactor;
		if (!useFog)
		{
			value = 0f;
		}
		fogFactor = Singleton<GameManager>.Instance.graphicsSettings.drawDistance;
		RenderSettings.fogEndDistance = fogFactor;
		Shader.SetGlobalColor(Shader.PropertyToID("_CustomFogColor"), fogColor);
		Shader.SetGlobalColor(Shader.PropertyToID("_CustomSkyColor"), skyColor);
		Shader.SetGlobalFloat(Shader.PropertyToID("_CustomFogFactor"), value);
		Shader.SetGlobalFloat(Shader.PropertyToID("_CustomFogOffset"), fogOffset);
		Shader.SetGlobalFloat(Shader.PropertyToID("_CustomHeightRefrence"), heightRefrence);
		Shader.SetGlobalFloat(Shader.PropertyToID("_CustomExponent"), exponent);
		if (!Singleton<GameManager>.Instance.inMenu && Singleton<PlaneContainer>.Instance != null)
		{
			SetFogProfile(Singleton<ContinentManager>.Instance.GetClosestContinent(Singleton<PlaneContainer>.Instance.transform.position).continentType.fogProfile);
			UpdateFogProfile();
		}
		if (Singleton<GameManager>.Instance.inMenu)
		{
			ResetFog();
		}
	}

	private void ResetFog()
	{
		fogColor = defaultFogProfile.fogColor;
		skyColor = defaultFogProfile.skyColor;
		exponent = defaultFogProfile.exponent;
	}

	private void UpdateFogProfile()
	{
		lerp += Time.deltaTime * 1f / profileLerpTime;
		lerp = Mathf.Clamp01(lerp);
		fogColor = Color.Lerp(fogColor, targetFogProfile.fogColor, lerp);
		skyColor = Color.Lerp(skyColor, targetFogProfile.skyColor, lerp);
		exponent = Mathf.Lerp(exponent, targetFogProfile.exponent, lerp);
	}
}
