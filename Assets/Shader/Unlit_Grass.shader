// Grass tufts around airports/outposts: unlit, tinted texture with alpha blending, double sided.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Unlit/Grass" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "AviassemblyCommon.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv) * _Color;
				return float4(AviApplyFog(col.rgb, i.worldPos), col.a);
			}
			ENDCG
		}
	}
}
