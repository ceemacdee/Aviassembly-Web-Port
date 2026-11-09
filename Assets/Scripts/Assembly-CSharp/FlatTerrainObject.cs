using UnityEngine;

public struct FlatTerrainObject
{
	public Vector3 worldPos;

	public FlatnessRectangle rectangle2D;

	public float blendingDistance;

	public float height;

	public FlatTerrainObject(Vector3 worldPos, FlatnessRectangle rectangle2D, float height, float blendingDistance)
	{
		this.worldPos = worldPos;
		this.rectangle2D = rectangle2D;
		this.height = height;
		this.blendingDistance = blendingDistance;
	}
}
