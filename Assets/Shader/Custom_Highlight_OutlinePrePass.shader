// HighlightRenderer pre-pass: draws selected/hovered parts as a flat _OutlineColor silhouette.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Highlight/OutlinePrePass" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Pass {
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			float4 _OutlineColor;

			float4 vert(float4 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}

			float4 frag() : SV_Target
			{
				return float4(_OutlineColor.rgb, max(_OutlineColor.a, 0.004));
			}
			ENDCG
		}
	}
}
