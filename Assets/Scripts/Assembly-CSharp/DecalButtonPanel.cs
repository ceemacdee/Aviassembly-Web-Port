using UnityEngine;

public class DecalButtonPanel : MonoBehaviour
{
	public DecalPlacer placer;

	public GameObject decalButtonPrefab;

	private void Start()
	{
		Texture2D[] allDecalTextures = PartPrefabs.GetAllDecalTextures();
		for (int i = 0; i < allDecalTextures.Length; i++)
		{
			GameObject obj = Object.Instantiate(decalButtonPrefab);
			obj.transform.SetParent(base.transform, worldPositionStays: true);
			obj.transform.localScale = Vector3.one;
			obj.GetComponent<DecalButton>().Init(allDecalTextures[i], placer);
		}
	}
}
