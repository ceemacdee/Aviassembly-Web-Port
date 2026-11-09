using System.Collections.Generic;
using UnityEngine;

public class DragSimulator : Singleton<DragSimulator>
{
	public float defaultDrag;

	public float dragMultiplier;

	[Range(0f, 1f)]
	public float calculateDragWeight;

	[Space(10f)]
	public float dragFactor;

	public float dragFactorRetracted;

	[Space(10f)]
	public Camera dragFactorCalculationCamera;

	public Shader dragReplacement;

	public Shader depthReplacement;

	private RenderTexture dragFactorCalculationTexture;

	private RenderTexture depthTexture;

	private Vector2 size;

	private int depthTextureUpdateFrame;

	private void Start()
	{
		dragFactorCalculationCamera.enabled = false;
		dragFactorCalculationTexture = new RenderTexture(1, 1, 24);
		dragFactorCalculationTexture.Create();
		depthTexture = new RenderTexture(1024, 1024, 24);
		depthTexture.filterMode = FilterMode.Point;
		depthTexture.Create();
		dragFactorCalculationCamera.targetTexture = depthTexture;
		Singleton<PlaneStorage>.Instance.PlaneUpdated += MarkDepthTextureDirty;
	}

	private void LateUpdate()
	{
		if (GameManager.gameMode == GameMode.Building && Singleton<PlaneContainer>.Instance != null && Singleton<PlaneContainer>.Instance.transform.childCount > 0)
		{
			UpdateOverlay();
		}
	}

	private void OnDestroy()
	{
		if (Singleton<PlaneStorage>.Instance != null)
		{
			Singleton<PlaneStorage>.Instance.PlaneUpdated -= MarkDepthTextureDirty;
		}
	}

	private void MarkDepthTextureDirty()
	{
		depthTextureUpdateFrame = Time.frameCount + 1;
	}

	private void UpdateDepthTextureSize()
	{
		if (depthTexture != null)
		{
			depthTexture.Release();
			Object.Destroy(depthTexture);
			depthTexture = null;
		}
		Vector2 clampedTextureSize = GetClampedTextureSize(1048576f, size);
		depthTexture = new RenderTexture(Mathf.FloorToInt(clampedTextureSize.x), Mathf.FloorToInt(clampedTextureSize.y), 24);
		// Point sampling: the drag overlay shader stores packed depth values that must not be blended.
		depthTexture.filterMode = FilterMode.Point;
		depthTexture.Create();
		dragFactorCalculationCamera.targetTexture = depthTexture;
	}

	public void UpdateOverlay()
	{
		CameraSetup();
		if (size.x > 0f && size.y > 0f)
		{
			RenderDepthTexture();
		}
	}

	public void UpdateDragFactor()
	{
		CameraSetup();
		if (size.x > 0f && size.y > 0f)
		{
			EnableRetractableGear(value: true);
			RenderDragTexture();
			dragFactorRetracted = Mathf.Lerp(defaultDrag, CalculateDragFactor(size) * 0.04f, calculateDragWeight);
			EnableRetractableGear(value: false);
			RenderDragTexture();
			dragFactor = Mathf.Lerp(defaultDrag, CalculateDragFactor(size) * 0.04f, calculateDragWeight);
		}
	}

	private void EnableRetractableGear(bool value)
	{
		RetractableLandingGear[] componentsInChildren = Singleton<PlaneContainer>.Instance.gameObject.GetComponentsInChildren<RetractableLandingGear>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetRetracted(value);
		}
	}

	private float CalculateDragFactor(Vector2 size)
	{
		Texture2D texture2D = new Texture2D(dragFactorCalculationTexture.width, dragFactorCalculationTexture.height, TextureFormat.RGB24, mipChain: false);
		RenderTexture.active = dragFactorCalculationTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, texture2D.width, texture2D.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = null;
		Color[] pixels = texture2D.GetPixels();
		Object.Destroy(texture2D);
		float num = 0f;
		for (int i = 0; i < pixels.Length; i++)
		{
			num += pixels[i].r;
		}
		float num2 = num / (float)pixels.Length;
		float num3 = size.x * size.y;
		return num2 * num3 * dragMultiplier;
	}

	private void CameraSetup()
	{
		Bounds rigidBodyBounds = Singleton<PlaneContainer>.Instance.GetRigidBodyBounds();
		if (!(rigidBodyBounds.size.magnitude < 0.01f))
		{
			float num = Mathf.Max(Mathf.Max(rigidBodyBounds.size.x, rigidBodyBounds.size.y), rigidBodyBounds.size.z);
			Vector3 position = rigidBodyBounds.center + Singleton<PlaneContainer>.Instance.UpdateForwardDirection() * (num * 0.5f + dragFactorCalculationCamera.nearClipPlane + 0.1f);
			dragFactorCalculationCamera.transform.position = position;
			dragFactorCalculationCamera.transform.LookAt(rigidBodyBounds.center);
			size = GetSize(rigidBodyBounds.size, rigidBodyBounds.center, dragFactorCalculationCamera.transform);
			dragFactorCalculationCamera.orthographicSize = size.y * 0.5f;
			dragFactorCalculationCamera.farClipPlane = num + dragFactorCalculationCamera.nearClipPlane + 0.1f;
			Shader.SetGlobalVector("_DragCameraWorldPos", rigidBodyBounds.center - dragFactorCalculationCamera.transform.position);
		}
	}

	private void RenderDragTexture()
	{
		if (dragFactorCalculationTexture != null)
		{
			dragFactorCalculationTexture.Release();
			Object.Destroy(dragFactorCalculationTexture);
			dragFactorCalculationTexture = null;
		}
		dragFactorCalculationTexture = new RenderTexture(Mathf.FloorToInt(size.x * 30f), Mathf.FloorToInt(size.y * 30f), 24);
		dragFactorCalculationTexture.Create();
		dragFactorCalculationCamera.targetTexture = dragFactorCalculationTexture;
		dragFactorCalculationCamera.SetReplacementShader(dragReplacement, "Replacement");
		dragFactorCalculationCamera.Render();
		dragFactorCalculationCamera.targetTexture = depthTexture;
	}

	private void RenderDepthTexture()
	{
		if (depthTextureUpdateFrame == Time.frameCount)
		{
			UpdateDepthTextureSize();
		}
		float a = (float)depthTexture.height / (float)depthTexture.width * size.x;
		float y = size.y;
		dragFactorCalculationCamera.orthographicSize = Mathf.Max(a, y) * 0.5f;
		dragFactorCalculationCamera.SetReplacementShader(depthReplacement, "Replacement");
		dragFactorCalculationCamera.Render();
		Shader.SetGlobalTexture("_DragDepthTexture", depthTexture);
		Shader.SetGlobalMatrix("_DragViewMatrix", dragFactorCalculationCamera.worldToCameraMatrix);
		Shader.SetGlobalMatrix("_DragProjectionMatrix", dragFactorCalculationCamera.projectionMatrix);
	}

	private Vector2 GetClampedTextureSize(float maxTotalPixels, Vector2 size)
	{
		if (size.x <= 0f || size.y <= 0f || maxTotalPixels <= 0f)
		{
			return new Vector2(0f, 0f);
		}
		float num = size.x * size.y;
		float num2 = Mathf.Sqrt(maxTotalPixels / num);
		return size * num2;
	}

	private Vector2 GetSize(Vector3 worldSize, Vector3 worldCenter, Transform camTransfrom)
	{
		float num = worldSize.y * 0.5f;
		float num2 = worldSize.x * 0.5f;
		float num3 = worldSize.z * 0.5f;
		List<Vector3> list = new List<Vector3>();
		list.Add(worldCenter + Vector3.right * num2 + Vector3.up * num + Vector3.forward * num3);
		list.Add(worldCenter - Vector3.right * num2 + Vector3.up * num + Vector3.forward * num3);
		list.Add(worldCenter + Vector3.right * num2 - Vector3.up * num + Vector3.forward * num3);
		list.Add(worldCenter - Vector3.right * num2 - Vector3.up * num + Vector3.forward * num3);
		list.Add(worldCenter + Vector3.right * num2 + Vector3.up * num - Vector3.forward * num3);
		list.Add(worldCenter - Vector3.right * num2 + Vector3.up * num - Vector3.forward * num3);
		list.Add(worldCenter + Vector3.right * num2 - Vector3.up * num - Vector3.forward * num3);
		list.Add(worldCenter - Vector3.right * num2 - Vector3.up * num - Vector3.forward * num3);
		float num4 = float.MaxValue;
		float num5 = float.MaxValue;
		float num6 = float.MinValue;
		float num7 = float.MinValue;
		for (int i = 0; i < list.Count; i++)
		{
			Vector3 position = Vector3.ProjectOnPlane(list[i], camTransfrom.forward);
			Vector3 vector = camTransfrom.InverseTransformPoint(position);
			num4 = Mathf.Min(vector.x, num4);
			num5 = Mathf.Min(vector.y, num5);
			num6 = Mathf.Max(vector.x, num6);
			num7 = Mathf.Max(vector.y, num7);
		}
		float x = num6 - num4;
		float y = num7 - num5;
		return new Vector2(x, y);
	}

	private void OnDrawGizmos()
	{
		PlaneContainer instance = Singleton<PlaneContainer>.Instance;
		if (instance != null)
		{
			Vector3 vector = instance.GetRigidBodyBounds().size;
			Gizmos.DrawWireCube(instance.GetRigidBodyBounds().center, vector);
		}
	}
}
