using UnityEngine;

public class CloudMaster : MonoBehaviour
{
	[Header("Shading")]
	public Color LightColor;

	public Color DarkColor;

	public float lightAbsorbtion;

	[Header("Shapes")]
	public float scrollSpeed;

	public Vector3 CloudOffset;

	public float CloudScale;

	public float DensityTreshold;

	public float DensityMultiplier;

	[Header("Detail")]
	public float DetailDensityMultiplier;

	public float DetailScale;

	public Vector3 DetailWeights;

	[Header("Sampeling")]
	public int NumSteps;

	public int LightSamples;

	[Header("Setup")]
	public Shader shader;

	public Transform container;

	public NoiseGenerator noiseGenerator;

	private Material material;

	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		Camera.main.depthTextureMode = Camera.main.depthTextureMode | DepthTextureMode.Depth;
		if (material == null)
		{
			material = new Material(shader);
		}
		CloudOffset += new Vector3(1f, 0f, 1f).normalized * scrollSpeed * Time.deltaTime;
		material.SetVector("_BoundsMin", container.position - container.localScale / 2f);
		material.SetVector("_BoundsMax", container.position + container.localScale / 2f);
		material.SetTexture("_Noise", noiseGenerator.GetNoiseTexture());
		material.SetTexture("_DetailNoise", noiseGenerator.GetDetailTexture());
		material.SetVector("_CloudOffset", CloudOffset);
		material.SetFloat("_CloudScale", CloudScale * 0.001f);
		material.SetFloat("_DensityTreshold", DensityTreshold);
		material.SetFloat("_DensityMultiplier", Mathf.Max(DensityMultiplier, 0.001f));
		material.SetFloat("_DetailDensityMultiplier", DetailDensityMultiplier);
		material.SetFloat("_DetailScale", DetailScale * 0.001f);
		material.SetFloat("_LightAbsorbtion", lightAbsorbtion);
		material.SetInt("_NumSteps", Mathf.Max(0, NumSteps));
		material.SetInt("_LightSamples", Mathf.Max(0, LightSamples));
		material.SetVector("_DetailWeights", new Vector4(DetailWeights.x, DetailWeights.y, DetailWeights.z, 1f));
		material.SetColor("_DarkColor", DarkColor);
		material.SetColor("_LightColor", LightColor);
		Graphics.Blit(src, dest, material);
	}
}
