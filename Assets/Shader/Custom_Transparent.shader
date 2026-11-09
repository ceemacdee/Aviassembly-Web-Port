// Glass (cockpit windows, lighthouse): stylized lit tint with a Fresnel rim that also increases opacity.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Transparent" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		[PowerSlider(4)] _FresnelExponent ("Fresnel Exponent", Range(0.25, 4)) = 1
		_FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0
		_DragFactor ("Drag Factor", Range(0, 1)) = 0
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "Replacement"="Drag" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass {
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "AviassemblyCommon.cginc"
			#include "Lighting.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _FresnelExponent;
			float _FresnelStrength;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 n = normalize(i.worldNormal);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
				float fresnel = pow(1.0 - saturate(dot(n, viewDir)), _FresnelExponent) * _FresnelStrength;
				float4 tex = tex2D(_MainTex, i.uv);
				float3 col = AviStylizedMainLight(_Color.rgb * tex.rgb, n, normalize(_WorldSpaceLightPos0.xyz), _LightColor0.rgb, 1.0, 0.8, 0.5);
				col = lerp(col, _LightColor0.rgb, fresnel);
				float alpha = saturate(_Color.a * tex.a * 0.75 + fresnel);
				return float4(AviApplyFog(col, i.worldPos), alpha);
			}
			ENDCG
		}
	}
}
