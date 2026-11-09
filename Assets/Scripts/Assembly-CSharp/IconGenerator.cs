using System.Collections.Generic;
using UnityEngine;

public class IconGenerator : Singleton<IconGenerator>
{
	public Transform parent;

	public Camera cam;

	private Vector3 defaultCamPosition;

	private float defaultRotation;

	private Vector3 defaultPosition;

	private Dictionary<GameObject, Texture2D> cache = new Dictionary<GameObject, Texture2D>();

	private void Start()
	{
		defaultCamPosition = cam.transform.position;
		defaultPosition = parent.transform.position;
		defaultRotation = parent.transform.eulerAngles.y;
	}

	public Texture GenerateIcon(GameObject itemPrefab, ItemFraming framing)
	{
		if (cache.ContainsKey(itemPrefab))
		{
			return cache[itemPrefab];
		}
		RenderTexture targetTexture = cam.targetTexture;
		Texture2D texture2D = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA32, mipChain: true, linear: true);
		parent.transform.position = defaultPosition + framing.positionOffset;
		parent.transform.eulerAngles = new Vector3(0f, defaultRotation, 0f) + framing.rotationOffset;
		cam.transform.position = defaultCamPosition + cam.transform.forward * framing.distanceOffset;
		GameObject gameObject = Object.Instantiate(itemPrefab);
		gameObject.transform.parent = parent.transform;
		gameObject.transform.localPosition = -GetLocalBoundsCenter(gameObject);
		gameObject.transform.localRotation = Quaternion.identity;
		if ((bool)gameObject.GetComponent<RetractableLandingGear>())
		{
			gameObject.GetComponent<RetractableLandingGear>().UpdateMatrix();
		}
		cam.Render();
		RenderTexture.active = targetTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, targetTexture.width, targetTexture.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = null;
		gameObject.transform.position += Vector3.up * -100000f;
		Object.Destroy(gameObject);
		cache.Add(itemPrefab, texture2D);
		return texture2D;
	}

	private Vector3 GetLocalBoundsCenter(GameObject obj)
	{
		MeshRenderer[] componentsInChildren = obj.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		if (componentsInChildren == null)
		{
			return obj.transform.position;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			zero += componentsInChildren[i].bounds.center;
		}
		Vector3 position = zero / componentsInChildren.Length;
		return obj.transform.InverseTransformPoint(position) * obj.transform.localScale.x;
	}
}
