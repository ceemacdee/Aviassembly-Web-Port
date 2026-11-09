// Depth-only "Hider" used in the Building scene: writes depth without colour so transparent geometry
// behind it (rendered later in the transparent queue) is masked out.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/HoleCutter" {
	Properties {
	}
	SubShader {
		Tags { "Queue"="Transparent-1" "RenderType"="Opaque" }
		Pass {
			ColorMask 0
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 vert(float4 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}

			float4 frag() : SV_Target
			{
				return 0;
			}
			ENDCG
		}
	}
}
