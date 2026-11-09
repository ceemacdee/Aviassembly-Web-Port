// Plane decals (DecalPlacer / Decal.prefab): alpha-blended texture tinted by the painted _Color, lit with the
// stylized ramp and pulled slightly towards the camera so it sits on top of the part surface.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Decal" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0, 1)) = 0
		_Shade ("Shade", Range(0, 1)) = 0
		_TextureOpacity ("Texture Opacity", Range(0, 1)) = 0
		_DragFactor ("Drag Factor", Range(0, 1)) = 0
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Offset -1, -1

		Pass {
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "AviassemblyCommon.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _Smoothness;
			float _Shade;
			float _TextureOpacity;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				UNITY_LIGHTING_COORDS(3, 4)
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				UNITY_TRANSFER_LIGHTING(o, v.texcoord.xy);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 tex = tex2D(_MainTex, i.uv);
				float3 n = normalize(i.worldNormal);
				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				float3 albedo = _Color.rgb * lerp(float3(1, 1, 1), tex.rgb, _TextureOpacity);
				float3 col = AviStylizedMainLight(albedo, n, normalize(_WorldSpaceLightPos0.xyz), _LightColor0.rgb, atten, _Shade, _Smoothness);
				return float4(AviApplyFog(col, i.worldPos), tex.a * _Color.a);
			}
			ENDCG
		}
	}
}
