// Procedural terrain chunks (TerrainGenerationManager). Colour is chosen from world height and slope:
// sand at the shoreline, grass on flat ground, dirt on slopes and rock on steep faces, with the noise
// texture breaking up the boundaries (_NoiceStrength). Lit with the same stylized ramp as the other assets.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Terrain" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_NoiceTex ("Texture", 2D) = "white" {}
		_NoiceStrength ("Noice Strength", Float) = 0
		_GrassColor ("Grass", Color) = (0,0,0,0)
		_SandColor ("Sand", Color) = (0,0,0,0)
		_DirtColor ("Dirt", Color) = (0,0,0,0)
		_RockColor ("Rock", Color) = (0,0,0,0)
		_Smoothness ("Smoothness", Range(0, 1)) = 0
		_Shade ("Shade", Range(0, 1)) = 0
		_TextureOpacity ("Texture Opacity", Range(0, 1)) = 0
	}
	CGINCLUDE
	#include "AviassemblyCommon.cginc"
	#include "Lighting.cginc"
	#include "AutoLight.cginc"

	sampler2D _MainTex;
	float4 _MainTex_ST;
	sampler2D _NoiceTex;
	float _NoiceStrength;
	float4 _GrassColor;
	float4 _SandColor;
	float4 _DirtColor;
	float4 _RockColor;
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

	float3 TerrainAlbedo(v2f i, float3 n)
	{
		float noise = tex2D(_NoiceTex, i.worldPos.xz * 0.0025).r - 0.5;
		float jitter = noise * _NoiceStrength;
		float slope = 1.0 - saturate(n.y);
		float height = i.worldPos.y;

		float3 col = _GrassColor.rgb;
		col = lerp(col, _DirtColor.rgb, step(0.12 - jitter, slope));
		col = lerp(col, _RockColor.rgb, step(0.3 - jitter, slope));
		col = lerp(_SandColor.rgb, col, step(2.5 + jitter * 40.0, height));
		col *= 1.0 + jitter;
		return col * lerp(float3(1, 1, 1), tex2D(_MainTex, i.uv).rgb, _TextureOpacity);
	}
	ENDCG

	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		LOD 200

		Pass {
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase

			float4 frag(v2f i) : SV_Target
			{
				float3 n = normalize(i.worldNormal);
				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				float3 col = AviStylizedMainLight(TerrainAlbedo(i, n), n, normalize(_WorldSpaceLightPos0.xyz), _LightColor0.rgb, atten, _Shade, _Smoothness);
				return float4(col, 1);
			}
			ENDCG
		}

		Pass {
			Tags { "LightMode"="ForwardAdd" }
			Blend One One
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdadd_fullshadows

			float4 frag(v2f i) : SV_Target
			{
				float3 n = normalize(i.worldNormal);
				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				return float4(AviStylizedAddLight(TerrainAlbedo(i, n), n, lightDir, _LightColor0.rgb, atten, _Smoothness), 0);
			}
			ENDCG
		}

		Pass {
			Tags { "LightMode"="ShadowCaster" }
			CGPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow
			#pragma multi_compile_shadowcaster

			struct v2fShadow
			{
				V2F_SHADOW_CASTER;
			};

			v2fShadow vertShadow(appdata_base v)
			{
				v2fShadow o;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 fragShadow(v2fShadow i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
	Fallback Off
}
