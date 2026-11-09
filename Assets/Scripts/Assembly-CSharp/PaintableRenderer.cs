using UnityEngine;

public class PaintableRenderer : MonoBehaviour
{
	public int paintableMaterialIndex;

	private MeshRenderer meshRenderer;

	public Color currentColor { get; private set; }

	public void Awake()
	{
		meshRenderer = GetComponent<MeshRenderer>();
		currentColor = meshRenderer.materials[paintableMaterialIndex].GetColor("_Color");
		ApplyColor(currentColor);
	}

	public Color GetCurrentColor()
	{
		return currentColor;
	}

	public void ApplyColor(Color color)
	{
		meshRenderer.materials[paintableMaterialIndex].SetColor("_Color", color);
		currentColor = color;
	}

	public void PreviewColor(Color color)
	{
		meshRenderer.materials[paintableMaterialIndex].SetColor("_Color", color);
	}

	public void ResetColor()
	{
		ApplyColor(currentColor);
	}

	private void OnDestroy()
	{
		Object.Destroy(meshRenderer.materials[paintableMaterialIndex]);
	}
}
