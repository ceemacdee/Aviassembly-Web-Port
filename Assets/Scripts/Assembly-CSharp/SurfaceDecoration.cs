using UnityEngine;

[CreateAssetMenu]
public class SurfaceDecoration : ScriptableObject
{
	[Header("Mesh")]
	public Mesh mesh;

	public Mesh[] meshVariants;

	public Mesh[] LODs;

	[Space(10f)]
	[Header("Spawning")]
	public int density;

	public float minSize;

	public float maxSize;

	public float randomizationOffset;

	[Header("Clustering")]
	public bool clustering;

	public int maxClusterSize;

	public float clusterRadius;

	[Header("Slope")]
	public float minSlope;

	public float maxSlope;

	[Space(10f)]
	[Header("Collider")]
	public float height;

	public float radius;

	public bool center;

	[Header("Rendering")]
	[Range(0f, 1f)]
	public float shade;
}
