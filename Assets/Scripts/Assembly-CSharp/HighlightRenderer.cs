using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HighlightRenderer : Singleton<HighlightRenderer>
{
	private class HighlightObject
	{
		public GameObject gameObject;

		public Color color;

		public HighlightObject(GameObject gameObject, Color color)
		{
			this.gameObject = gameObject;
			this.color = color;
		}
	}

	private List<HighlightObject> outlineObjects = new List<HighlightObject>();

	private List<HighlightObject> highlightObjects = new List<HighlightObject>();

	private List<BuildingPart> addedOutlineParts = new List<BuildingPart>();

	private List<BuildingPart> addedHighlightParts = new List<BuildingPart>();

	public Shader prePassShader;

	public Shader blurShader;

	public Shader compositeShader;

	public float outlineThickness;

	private Camera cam;

	private CommandBuffer commandBuffer;

	private Material prePassMaterial;

	private Material blurMaterial;

	private Material compositeMaterial;

	private int prePassTextureID;

	private int blurPassTextureID;

	private int outlineColorID;

	private int highlightTextureID;

	private int highlightDepthTextureID;

	public void AddOutlineObject(GameObject outlineObject, Color color)
	{
		if (!(outlineObject == null))
		{
			outlineObjects.Add(new HighlightObject(outlineObject, color));
		}
	}

	public void AddHighlightObject(GameObject highlightObject, Color color)
	{
		if (!(highlightObject == null))
		{
			highlightObjects.Add(new HighlightObject(highlightObject, color));
		}
	}

	public void HighlightBuildingPart(BuildingPart part, bool outline, bool highlight, Color color, float highlightAlpha = 0.2f)
	{
		if (part != null)
		{
			if (highlight && !addedHighlightParts.Contains(part))
			{
				color.a = highlightAlpha;
				AddHighlightObject(part.gameObject, color);
				addedHighlightParts.Add(part);
			}
			if (outline && !addedOutlineParts.Contains(part))
			{
				color.a = 1f;
				AddOutlineObject(part.gameObject, color);
				addedOutlineParts.Add(part);
			}
			for (int i = 0; i < part.children.Count; i++)
			{
				HighlightBuildingPart(part.children[i], outline, highlight, color, highlightAlpha);
			}
		}
	}

	protected override void Awake()
	{
		base.Awake();
		commandBuffer = new CommandBuffer();
		commandBuffer.name = "Highlight rendering buffer";
		cam = GetComponent<Camera>();
		cam.AddCommandBuffer(CameraEvent.BeforeImageEffects, commandBuffer);
		prePassMaterial = new Material(prePassShader);
		blurMaterial = new Material(blurShader);
		compositeMaterial = new Material(compositeShader);
		prePassTextureID = Shader.PropertyToID("_PrePass");
		blurPassTextureID = Shader.PropertyToID("_BlurPass");
		outlineColorID = Shader.PropertyToID("_OutlineColor");
		highlightTextureID = Shader.PropertyToID("_HighlightTexture");
		highlightDepthTextureID = Shader.PropertyToID("_HighlightDepthTexture");
	}

	private void OnPreRender()
	{
		blurMaterial.SetFloat("_OutlineThickness", outlineThickness);
		commandBuffer.Clear();
		commandBuffer.GetTemporaryRT(prePassTextureID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, Mathf.Max(1, QualitySettings.antiAliasing));
		commandBuffer.SetRenderTarget(prePassTextureID);
		commandBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
		RenderOutlineObjects();
		commandBuffer.GetTemporaryRT(blurPassTextureID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
		commandBuffer.Blit(prePassTextureID, blurPassTextureID, blurMaterial);
		commandBuffer.GetTemporaryRT(highlightTextureID, Screen.width, Screen.height, 32, FilterMode.Bilinear, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
		commandBuffer.SetRenderTarget(highlightTextureID);
		commandBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
		RenderHighlightObjects();
		commandBuffer.GetTemporaryRT(highlightDepthTextureID, Screen.width, Screen.height, 32, FilterMode.Bilinear, RenderTextureFormat.Depth, RenderTextureReadWrite.Default);
		commandBuffer.SetRenderTarget(highlightDepthTextureID);
		commandBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
		RenderHighlightObjects();
		commandBuffer.ReleaseTemporaryRT(prePassTextureID);
		commandBuffer.ReleaseTemporaryRT(blurPassTextureID);
		commandBuffer.ReleaseTemporaryRT(highlightTextureID);
		commandBuffer.ReleaseTemporaryRT(highlightDepthTextureID);
		outlineObjects.Clear();
		highlightObjects.Clear();
		addedHighlightParts.Clear();
		addedOutlineParts.Clear();
	}

	private void RenderOutlineObjects()
	{
		for (int i = 0; i < outlineObjects.Count; i++)
		{
			if (outlineObjects[i].gameObject == null)
			{
				continue;
			}
			Renderer[] componentsInChildren = outlineObjects[i].gameObject.GetComponentsInChildren<Renderer>();
			MeshFilter[] componentsInChildren2 = outlineObjects[i].gameObject.GetComponentsInChildren<MeshFilter>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				for (int k = 0; k < componentsInChildren[j].sharedMaterials.Length; k++)
				{
					commandBuffer.SetGlobalColor(outlineColorID, outlineObjects[i].color);
					commandBuffer.DrawMesh(componentsInChildren2[j].sharedMesh, componentsInChildren[j].localToWorldMatrix, prePassMaterial, k);
				}
			}
		}
	}

	private void RenderHighlightObjects()
	{
		for (int i = 0; i < highlightObjects.Count; i++)
		{
			if (highlightObjects[i].gameObject == null)
			{
				continue;
			}
			Renderer[] componentsInChildren = highlightObjects[i].gameObject.GetComponentsInChildren<Renderer>();
			MeshFilter[] componentsInChildren2 = highlightObjects[i].gameObject.GetComponentsInChildren<MeshFilter>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				for (int k = 0; k < componentsInChildren[j].sharedMaterials.Length; k++)
				{
					commandBuffer.SetGlobalColor(outlineColorID, highlightObjects[i].color);
					commandBuffer.DrawMesh(componentsInChildren2[j].sharedMesh, componentsInChildren[j].localToWorldMatrix, prePassMaterial, k);
				}
			}
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination, compositeMaterial);
	}
}
