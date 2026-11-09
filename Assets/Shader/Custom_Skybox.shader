// Sky: horizon fog colour -> zenith sky colour gradient (driven by CustomFog.cs globals) with the painted cloud
// cubemap composited on top. _SkyboxStrength pulls the clouds towards the atmosphere colour.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Skybox" {
	Properties {
		_SkyboxCube ("Skybox Cubemap", Cube) = "" {}
		_SkyboxStrength ("Skybox Strength", Range(0, 1)) = 0
	}
	SubShader {
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off
		ZWrite Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "AviassemblyCommon.cginc"

			samplerCUBE _SkyboxCube;
			float _SkyboxStrength;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 dir : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.dir = v.vertex.xyz;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 dir = normalize(i.dir);
				float3 sky = AviSkyGradient(dir, _WorldSpaceCameraPos.y);
				float4 clouds = texCUBE(_SkyboxCube, dir);
				float3 cloudColor = lerp(clouds.rgb, _CustomFogColor.rgb, _SkyboxStrength);
				return float4(lerp(sky, cloudColor, clouds.a), 1);
			}
			ENDCG
		}
	}
	Fallback Off
}
