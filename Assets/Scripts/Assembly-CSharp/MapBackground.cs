using UnityEngine;
using UnityEngine.UI;

public class MapBackground : MonoBehaviour
{
	public RawImage image;

	public Map map;

	public int continent;

	public Color surfaceColor;

	private TerrainGenerationManager terrainGenerationManager;

	private Texture2D texture;

	private Color[] colors;

	private int textureSize;

	private CanvasGroup mapCanvasGroup;

	private void Start()
	{
		textureSize = Mathf.RoundToInt(Singleton<ContinentManager>.Instance.continents[continent].continentType.radius * 0.015f);
		terrainGenerationManager = Singleton<TerrainGenerationManager>.Instance;
		texture = new Texture2D(textureSize, textureSize);
		texture.filterMode = FilterMode.Bilinear;
		image.texture = texture;
		colors = new Color[textureSize * textureSize];
		UpdateColors(map.offset);
		texture.SetPixels(colors);
		texture.Apply();
		mapCanvasGroup = map.GetComponent<CanvasGroup>();
		image.enabled = false;
		Material material = new Material(image.material);
		material.SetColor("_Ground", surfaceColor);
		image.material = material;
	}

	public void Update()
	{
		base.transform.position = map.WorldToMapPosition(Singleton<ContinentManager>.Instance.continents[continent].origin);
		float radius = Singleton<ContinentManager>.Instance.continents[continent].continentType.radius;
		base.transform.localScale = Vector3.one * (2f * radius / map.zoom);
		image.enabled = mapCanvasGroup.alpha > 0.5f;
	}

	private void UpdateColors(Vector2 mapOffset)
	{
		if (Singleton<ContinentManager>.Instance.continents[continent].mapColors != null)
		{
			colors = Singleton<ContinentManager>.Instance.continents[continent].mapColors;
			return;
		}
		Vector3 origin = Singleton<ContinentManager>.Instance.continents[continent].origin;
		float radius = Singleton<ContinentManager>.Instance.continents[continent].continentType.radius;
		Vector3 vector = origin - new Vector3(radius, 0f, radius);
		for (int i = 0; i < textureSize; i++)
		{
			for (int j = 0; j < textureSize; j++)
			{
				Vector3 worldPos = vector;
				worldPos.x = Mathf.Lerp(vector.x, vector.x + 2f * radius, (float)j / (float)textureSize);
				worldPos.z = Mathf.Lerp(vector.z, vector.z + 2f * radius, (float)i / (float)textureSize);
				float num = terrainGenerationManager.GetTerrainHeightFast(worldPos, terrainGenerationManager.continentFalloff, terrainGenerationManager.flatTerrainObjects) + 5f;
				Color color = Color.Lerp(Color.black, Color.white, (num + 8f) / 200f);
				colors[i * textureSize + j] = color;
			}
		}
		Singleton<ContinentManager>.Instance.continents[continent].mapColors = colors;
	}
}
