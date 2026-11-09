using UnityEngine;

public class ContainerMover : MonoBehaviour
{
	public float cloudHeight;

	private void Update()
	{
		Vector3 position = Singleton<PlaneContainer>.Instance.transform.position;
		position.y = cloudHeight;
		base.transform.position = position;
	}
}
