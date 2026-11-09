using UnityEngine;

public class Decal : MonoBehaviour, IPaintable
{
	private PaintableRenderer paintableRenderer;

	public Color currentColor;

	public Texture2D currentTexture;

	public bool hasBeenPlaced;

	public bool sorted;

	public BuildingPart parentBuildingPart;

	public MeshDecal meshDecal { get; private set; }

	private void Awake()
	{
		paintableRenderer = GetComponent<PaintableRenderer>();
		meshDecal = GetComponent<MeshDecal>();
		currentColor = paintableRenderer.currentColor;
	}

	public void SetColor(Color color, bool apply)
	{
		if (apply)
		{
			paintableRenderer.ApplyColor(color);
			currentColor = color;
		}
		else
		{
			paintableRenderer.PreviewColor(color);
		}
	}

	public void SetParent(BuildingPart buildingPart)
	{
		if (parentBuildingPart != null)
		{
			parentBuildingPart.decals.Remove(this);
		}
		buildingPart.decals.Add(this);
		parentBuildingPart = buildingPart;
	}

	public void DestroyDecal()
	{
		if (parentBuildingPart != null)
		{
			parentBuildingPart.decals.Remove(this);
		}
		Singleton<DecalContainer>.Instance.decals.Remove(this);
		Singleton<DecalContainer>.Instance.colliders.Remove(GetComponent<Collider>());
		Object.Destroy(base.gameObject);
	}

	public void ResetColor()
	{
		paintableRenderer.ApplyColor(currentColor);
	}

	public Color GetCurrentColor()
	{
		return currentColor;
	}
}
