// HighlightRenderer blur: dilates the pre-pass silhouette by _OutlineThickness pixels.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Highlight/OutlineBlur" {
	Properties {
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		_Color ("Test Color", Color) = (1,1,1,1)
		_OutlineThickness ("Outline Sample Distance", Float) = 2
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull Off
		ZWrite Off
		ZTest Always
		Pass {
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _OutlineThickness;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 result = tex2D(_MainTex, i.uv);
				float2 stepUV = _MainTex_TexelSize.xy * _OutlineThickness;
				// 16 taps on two rings; keep the strongest sample so the outline stays crisp.
				[unroll]
				for (int k = 0; k < 8; k++)
				{
					float a = k * 0.78539816;
					float2 o = float2(cos(a), sin(a));
					float4 s1 = tex2D(_MainTex, i.uv + o * stepUV);
					float4 s2 = tex2D(_MainTex, i.uv + o * stepUV * 0.5);
					result = (s1.a > result.a) ? s1 : result;
					result = (s2.a > result.a) ? s2 : result;
				}
				return result;
			}
			ENDCG
		}
	}
}
