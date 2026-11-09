using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class NoiseGenerator : MonoBehaviour
{
	public ComputeShader worleyNoiseCompute;

	public ComputeShader slicer;

	public RawImage preview;

	public int resolution;

	public int detailResolution;

	public int numPoints;

	public int numPointsDetail;

	public float falloff;

	[Range(0f, 128f)]
	public int selectedLayer;

	private Texture2D texture;

	private RenderTexture shapeTexture;

	private RenderTexture detailTexture;

	private RenderTexture slice;

	private void Start()
	{
		texture = new Texture2D(resolution, resolution);
		slice = new RenderTexture(resolution, resolution, 0);
		slice.enableRandomWrite = true;
		shapeTexture = new RenderTexture(resolution, resolution, 0);
		shapeTexture.enableRandomWrite = true;
		shapeTexture.dimension = TextureDimension.Tex3D;
		shapeTexture.volumeDepth = resolution;
		shapeTexture.wrapMode = TextureWrapMode.Repeat;
		shapeTexture.filterMode = FilterMode.Bilinear;
		shapeTexture.Create();
		detailTexture = new RenderTexture(detailResolution, detailResolution, 0);
		detailTexture.enableRandomWrite = true;
		detailTexture.dimension = TextureDimension.Tex3D;
		detailTexture.volumeDepth = detailResolution;
		detailTexture.wrapMode = TextureWrapMode.Repeat;
		detailTexture.filterMode = FilterMode.Bilinear;
		detailTexture.Create();
		slicer.SetTexture(0, "Result", slice);
		UpdateNoise();
	}

	public RenderTexture GetNoiseTexture()
	{
		return shapeTexture;
	}

	public RenderTexture GetDetailTexture()
	{
		return detailTexture;
	}

	private void UpdateNoise()
	{
		if (numPoints > 0)
		{
			worleyNoiseCompute.SetInt("resolution", resolution);
			worleyNoiseCompute.SetInt("partitions", numPoints);
			worleyNoiseCompute.SetFloat("falloff", falloff);
			worleyNoiseCompute.SetTexture(0, "Result", shapeTexture);
			worleyNoiseCompute.Dispatch(0, resolution / 8, resolution / 8, resolution / 8);
			worleyNoiseCompute.SetInt("resolution", detailResolution);
			worleyNoiseCompute.SetInt("partitions", numPointsDetail);
			worleyNoiseCompute.SetFloat("falloff", falloff);
			worleyNoiseCompute.SetTexture(1, "Result", detailTexture);
			worleyNoiseCompute.Dispatch(1, detailResolution / 8, detailResolution / 8, detailResolution / 8);
		}
	}

	private void UpdateSlicer()
	{
		slicer.SetTexture(0, "Input", shapeTexture);
		slicer.SetInt("layer", selectedLayer);
		slicer.Dispatch(0, resolution / 8, resolution / 8, 1);
		RenderTexture.active = slice;
		texture.ReadPixels(new Rect(0f, 0f, shapeTexture.width, shapeTexture.height), 0, 0);
		texture.Apply();
		if (preview != null)
		{
			preview.texture = texture;
		}
	}
}
