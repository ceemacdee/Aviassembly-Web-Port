using UnityEngine;

[CreateAssetMenu]
public class ContinentType : ScriptableObject
{
	public string continentName;

	[Space(40f)]
	[Header("Position")]
	public float angleFromOrigin;

	public float distanceFromOrigin;

	[Space(10f)]
	[Header("Size")]
	public float radius;

	[Space(40f)]
	[Header("Terrain")]
	public Material terrainMaterial;

	public float minHeight;

	public float maxHeight;

	public SurfaceDecoration[] surfaceDecorations;

	public FogProfile fogProfile;

	public AudioClip backgroundMusic;

	public Color surfaceColor;

	public int temprature;

	public int scrapAmount;

	public float hintDistance;

	[Space(30f)]
	[Header("Airports")]
	public AirportData[] airports;
}
