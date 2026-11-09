using UnityEngine;

public class ProceduralConnection : MonoBehaviour
{
	public Vector2 radius;

	public Vector4 roundness;

	public Transform snapPoint1;

	public Transform snapPoint2;

	private void OnDrawGizmos()
	{
		if (snapPoint1 != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(snapPoint1.position, radius * 2f);
		}
		if (snapPoint2 != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(snapPoint2.position, radius * 2f);
		}
	}
}
