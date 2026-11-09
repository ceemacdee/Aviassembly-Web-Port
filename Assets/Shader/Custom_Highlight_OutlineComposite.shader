// HighlightRenderer composite: draws the outline (dilated silhouette minus the silhouette itself) and the
// translucent highlight fill over the camera image. Inputs are the command buffer's temporary targets.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Highlight/OutlineComposite" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("SelectionColor", Color) = (1,1,1,1)
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
			sampler2D _PrePass;
			sampler2D _BlurPass;
			sampler2D _HighlightTexture;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 src = tex2D(_MainTex, i.uv);
				float4 pre = tex2D(_PrePass, i.uv);
				float4 blur = tex2D(_BlurPass, i.uv);
				float4 highlight = tex2D(_HighlightTexture, i.uv);
				float outline = saturate((blur.a > 0.0 ? 1.0 : 0.0) - (pre.a > 0.0 ? 1.0 : 0.0));
				float3 col = lerp(src.rgb, blur.rgb, outline);
				col = lerp(col, highlight.rgb, saturate(highlight.a));
				return float4(col, src.a);
			}
			ENDCG
		}
	}
}
