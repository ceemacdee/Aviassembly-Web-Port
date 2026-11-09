using System;
using System.IO;
using UnityEngine;

public class PlaneThumbnailGenerator : Singleton<PlaneThumbnailGenerator>
{
	public Camera cam;

	public float padding;

	private PlaneContainer planeContainer;

	private void Start()
	{
		planeContainer = Singleton<PlaneContainer>.Instance;
	}

	public void FrameCamera(float yaw, float pitch, float padding)
	{
		Renderer[] componentsInChildren = planeContainer.GetComponentsInChildren<Renderer>(includeInactive: false);
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		float num = Vector3.SignedAngle(planeContainer.Forward, Vector3.forward, Vector3.up);
		Quaternion quaternion = Quaternion.Euler(pitch, yaw + num + 180f, 0f);
		cam.transform.rotation = quaternion;
		Vector3 vector = quaternion * Vector3.forward;
		Vector3 vector2 = quaternion * Vector3.right;
		Vector3 vector3 = quaternion * Vector3.up;
		float f = cam.fieldOfView * 0.5f * (MathF.PI / 180f);
		float f2 = Mathf.Atan(Mathf.Tan(f) * cam.aspect);
		float num2 = Mathf.Tan(f);
		float num3 = Mathf.Tan(f2);
		int num4 = componentsInChildren.Length * 8;
		Vector3[] array = new Vector3[num4];
		float num5 = float.MaxValue;
		float num6 = float.MinValue;
		float num7 = float.MaxValue;
		float num8 = float.MinValue;
		float num9 = float.MaxValue;
		float num10 = float.MinValue;
		int num11 = 0;
		Renderer[] array2 = componentsInChildren;
		foreach (Renderer obj in array2)
		{
			Bounds localBounds = obj.localBounds;
			Matrix4x4 localToWorldMatrix = obj.localToWorldMatrix;
			Vector3 center = localBounds.center;
			Vector3 extents = localBounds.extents;
			for (int j = 0; j < 8; j++)
			{
				Vector3 point = center + new Vector3(((j & 1) == 0) ? extents.x : (0f - extents.x), ((j & 2) == 0) ? extents.y : (0f - extents.y), ((j & 4) == 0) ? extents.z : (0f - extents.z));
				Vector3 lhs = localToWorldMatrix.MultiplyPoint3x4(point);
				float num12 = Vector3.Dot(lhs, vector2);
				float num13 = Vector3.Dot(lhs, vector3);
				float num14 = Vector3.Dot(lhs, vector);
				array[num11++] = new Vector3(num12, num13, num14);
				if (num12 < num5)
				{
					num5 = num12;
				}
				if (num12 > num6)
				{
					num6 = num12;
				}
				if (num13 < num7)
				{
					num7 = num13;
				}
				if (num13 > num8)
				{
					num8 = num13;
				}
				if (num14 < num9)
				{
					num9 = num14;
				}
				if (num14 > num10)
				{
					num10 = num14;
				}
			}
		}
		float num15 = 0.5f * (num5 + num6);
		float num16 = 0.5f * (num7 + num8);
		float num17 = float.MaxValue;
		for (int k = 0; k < num4; k++)
		{
			Vector3 vector4 = array[k];
			num17 = Mathf.Min(num17, vector4.z - Mathf.Abs(vector4.x - num15) / num3);
			num17 = Mathf.Min(num17, vector4.z - Mathf.Abs(vector4.y - num16) / num2);
		}
		float num18 = 0.5f * (num9 + num10);
		num17 = num18 - (num18 - num17) * (padding + 1f);
		num17 = Mathf.Min(num17, num9 - cam.nearClipPlane);
		cam.transform.position = vector2 * num15 + vector3 * num16 + vector * num17;
	}

	public string GenerateThumbnail(string fileName)
	{
		cam.enabled = true;
		FrameCamera(45f, 35f, padding);
		RenderTexture targetTexture = cam.targetTexture;
		Texture2D texture2D = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA32, mipChain: true, linear: true);
		cam.Render();
		cam.Render();
		RenderTexture.active = targetTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, targetTexture.width, targetTexture.height), 0, 0);
		texture2D.Apply();
		RenderTexture.active = null;
		string text = Path.Combine(Path.Combine(Path.Combine(Application.persistentDataPath, "Plane Designs"), fileName), fileName + ".jpg");
		byte[] bytes = texture2D.EncodeToJPG(100);
		File.WriteAllBytes(text, bytes);
		WebGLFileSync.Flush();
		UnityEngine.Object.Destroy(texture2D);
		cam.enabled = false;
		return text;
	}
}
