// Volumetric cloud image effect for CloudMaster.cs. CloudMaster and its NoiseGenerator (compute shaders) are
// disabled in the shipped Flying scene, so this is a pass-through blit; WebGL cannot run the compute noise.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Unlit/Clouds" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
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

			float4 frag(v2f_img i) : SV_Target
			{
				return tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}
