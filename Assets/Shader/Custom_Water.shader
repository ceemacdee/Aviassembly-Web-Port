// Stylized water: colour from shallow to deep by the depth of the scene behind the surface (camera depth
// texture, enabled by Fog.cs), scrolling noise caustics and shoreline foam.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Water" {
	Properties {
		_ShallowColor ("Shallow Color", Color) = (0,0,0,0)
		_DeepColor ("Deep Color", Color) = (0,0,0,0)
		_MaxDepth ("Max Depth", Float) = 1
		_NoiseScrollSpeed ("ScrollSpeed", Float) = 1
		_FoamDistance ("Foam Distance", Float) = 1
		_FoamColor ("Foam Color", Color) = (0,0,0,0)
		_SurfaceNoise ("Surface Noise", 2D) = "white" {}
		_SurfaceNoiseCutoff ("Surface Noise Cutoff", Range(0, 1)) = 0.777
		_SurfaceNoiseColor ("Surface Noise Color", Color) = (0,0,0,0)
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "AviassemblyCommon.cginc"

			float4 _ShallowColor;
			float4 _DeepColor;
			float _MaxDepth;
			float _NoiseScrollSpeed;
			float _FoamDistance;
			float4 _FoamColor;
			sampler2D _SurfaceNoise;
			float4 _SurfaceNoise_ST;
			float _SurfaceNoiseCutoff;
			float4 _SurfaceNoiseColor;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 screenPos : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.pos);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
				float depthDifference = max(sceneDepth - i.screenPos.w, 0.0);

				float4 water = lerp(_ShallowColor, _DeepColor, saturate(depthDifference / max(_MaxDepth, 0.0001)));

				float2 noiseUV = i.worldPos.xz * 0.01 * _SurfaceNoise_ST.xy + _SurfaceNoise_ST.zw;
				noiseUV += _Time.y * _NoiseScrollSpeed * float2(1.0, 0.41);
				float noise = tex2D(_SurfaceNoise, noiseUV).r;

				float foam = saturate(depthDifference / max(_FoamDistance, 0.0001));
				float cutoff = foam * _SurfaceNoiseCutoff;
				float surfaceNoise = smoothstep(cutoff - 0.01, cutoff + 0.01, noise);

				float3 col = water.rgb + _SurfaceNoiseColor.rgb * surfaceNoise + _FoamColor.rgb * (1.0 - foam);
				col = AviApplyFog(col, i.worldPos);
				return float4(col, water.a);
			}
			ENDCG
		}
	}
}
