// Full-screen distance fog + sun used by Fog.cs (Menu and Flying cameras). Fog.cs passes the frustum corner
// rays (scaled to the far plane) in TEXCOORD1, so world position = camera + ray * Linear01Depth.
// Fog colour follows the same horizon/zenith gradient as Custom/Skybox, so terrain fades into the sky.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Fog" {
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
			#pragma vertex vert
			#pragma fragment frag
			#include "AviassemblyCommon.cginc"

			sampler2D _MainTex;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float4 _SunColor;
			float4 _SunGlowColor;
			float _SunGlowStrength;
			float _SunRadius;
			float4 _SunDirection; // set by Fog.cs: direction towards the sun

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 ray : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 ray : TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.ray = v.ray;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 src = tex2D(_MainTex, i.uv);
				float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float depth01 = Linear01Depth(rawDepth);
				float3 dir = normalize(i.ray);
				bool isSky = depth01 > 0.9999;

				float sunDot = dot(dir, normalize(_SunDirection.xyz));
				float glow = pow(saturate(sunDot), 24.0) * _SunGlowStrength;
				float3 glowColor = _SunGlowColor.rgb * glow;

				if (isSky)
				{
					float disc = smoothstep(1.0 - _SunRadius * 1.25, 1.0 - _SunRadius, sunDot);
					float3 col = src.rgb + glowColor;
					col = lerp(col, _SunColor.rgb, disc);
					return float4(col, src.a);
				}

				float distance = length(i.ray * depth01);
				float fog = AviFogAmount(distance);
				float3 fogColor = AviSkyGradient(dir, _WorldSpaceCameraPos.y) + glowColor;
				return float4(lerp(src.rgb, fogColor, fog), src.a);
			}
			ENDCG
		}
	}
}
