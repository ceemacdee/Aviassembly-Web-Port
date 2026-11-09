using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class SSAOToggle : MonoBehaviour
{
	private PostProcessVolume postProcessVolume;

	private AmbientOcclusion ssaoEffect;

	private void Start()
	{
		postProcessVolume = GetComponent<PostProcessVolume>();
		if (postProcessVolume.profile.TryGetSettings<AmbientOcclusion>(out ssaoEffect))
		{
			ssaoEffect.enabled.value = true;
		}
		ToggleSSAO(Singleton<GameManager>.Instance.graphicsSettings.SSAO);
	}

	private void Update()
	{
		ToggleSSAO(Singleton<GameManager>.Instance.graphicsSettings.SSAO);
	}

	public void ToggleSSAO(bool enable)
	{
		if (ssaoEffect != null)
		{
			ssaoEffect.enabled.value = enable;
		}
	}
}
