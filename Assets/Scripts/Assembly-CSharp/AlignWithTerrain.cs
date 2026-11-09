using UnityEngine;

public class AlignWithTerrain : MonoBehaviour
{
	private void Start()
	{
		base.transform.position = new Vector3(base.transform.position.x, Singleton<TerrainGenerationManager>.Instance.GetTerrainHeight(base.transform.position), base.transform.position.z);
	}
}
