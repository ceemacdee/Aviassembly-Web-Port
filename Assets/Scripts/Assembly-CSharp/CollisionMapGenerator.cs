using UnityEngine;

public class CollisionMapGenerator : Singleton<CollisionMapGenerator>
{
	public Transform parent;

	public Camera cam;

	public Texture GenerateMap(GameObject airport)
	{
		Vector3 position = airport.transform.position;
		RenderTexture targetTexture = cam.targetTexture;
		Texture2D texture2D = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA32, mipChain: true, linear: true);
		airport.transform.position = parent.transform.position;
		cam.Render();
		RenderTexture.active = targetTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, targetTexture.width, targetTexture.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = null;
		airport.transform.position = position;
		return texture2D;
	}
}
