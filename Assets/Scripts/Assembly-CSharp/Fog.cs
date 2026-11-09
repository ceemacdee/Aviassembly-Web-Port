using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Fog : MonoBehaviour
{
	public Shader fogShader;

	[Space(15f)]
	[Header("Sun")]
	public Color sunColor;

	public Color sunGlowColor;

	[Range(0f, 1f)]
	public float sunGlowStrength;

	public float sunRadius;

	private Material fogMat;

	private Camera cam;

	private void Start()
	{
		if (fogMat == null)
		{
			fogMat = new Material(fogShader);
			fogMat.hideFlags = HideFlags.HideAndDontSave;
		}
		cam = GetComponent<Camera>();
		cam.depthTextureMode |= DepthTextureMode.Depth;
		fogMat.SetVector("_SunColor", sunColor);
		fogMat.SetVector("_SunGlowColor", sunGlowColor);
		fogMat.SetFloat("_SunGlowStrength", sunGlowStrength);
		fogMat.SetFloat("_SunRadius", sunRadius);
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		RaycastCornerBlit(source, destination, fogMat);
	}

	private void RaycastCornerBlit(RenderTexture source, RenderTexture dest, Material mat)
	{
		float farClipPlane = cam.farClipPlane;
		float fieldOfView = cam.fieldOfView;
		float aspect = cam.aspect;
		float num = fieldOfView * 0.5f;
		Vector3 vector = cam.transform.right * Mathf.Tan(num * (MathF.PI / 180f)) * aspect;
		Vector3 vector2 = cam.transform.up * Mathf.Tan(num * (MathF.PI / 180f));
		Vector3 v = cam.transform.forward - vector + vector2;
		float num2 = v.magnitude * farClipPlane;
		v.Normalize();
		v *= num2;
		Vector3 v2 = cam.transform.forward + vector + vector2;
		v2.Normalize();
		v2 *= num2;
		Vector3 v3 = cam.transform.forward + vector - vector2;
		v3.Normalize();
		v3 *= num2;
		Vector3 v4 = cam.transform.forward - vector - vector2;
		v4.Normalize();
		v4 *= num2;
		RenderTexture.active = dest;
		mat.SetTexture("_MainTex", source);
		mat.SetVector("_SunDirection", GetSunDirection());
		GL.PushMatrix();
		GL.LoadOrtho();
		mat.SetPass(0);
		GL.Begin(7);
		GL.MultiTexCoord2(0, 0f, 0f);
		GL.MultiTexCoord(1, v4);
		GL.Vertex3(0f, 0f, 0f);
		GL.MultiTexCoord2(0, 1f, 0f);
		GL.MultiTexCoord(1, v3);
		GL.Vertex3(1f, 0f, 0f);
		GL.MultiTexCoord2(0, 1f, 1f);
		GL.MultiTexCoord(1, v2);
		GL.Vertex3(1f, 1f, 0f);
		GL.MultiTexCoord2(0, 0f, 1f);
		GL.MultiTexCoord(1, v);
		GL.Vertex3(0f, 1f, 0f);
		GL.End();
		GL.PopMatrix();
	}

	private Light sun;

	private int nextSunSearchFrame;

	// Added for the reconstructed Custom/Fog shader: direction towards the main directional light.
	private Vector3 GetSunDirection()
	{
		if ((sun == null || !sun.isActiveAndEnabled) && Time.frameCount >= nextSunSearchFrame)
		{
			nextSunSearchFrame = Time.frameCount + 60;
			sun = RenderSettings.sun;
			if (sun == null)
			{
				Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
				for (int i = 0; i < lights.Length; i++)
				{
					if (lights[i].type == LightType.Directional && lights[i].isActiveAndEnabled)
					{
						sun = lights[i];
						break;
					}
				}
			}
		}
		if (sun == null)
		{
			return Vector3.up;
		}
		return -sun.transform.forward;
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(fogMat);
	}
}
