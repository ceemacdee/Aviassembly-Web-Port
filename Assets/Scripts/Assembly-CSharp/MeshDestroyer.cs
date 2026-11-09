using UnityEngine;

public class MeshDestroyer : MonoBehaviour
{
	private void OnDestroy()
	{
		Object.Destroy(GetComponent<MeshFilter>().sharedMesh);
	}
}
