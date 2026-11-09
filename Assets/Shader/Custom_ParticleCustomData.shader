// Explosion mesh particles (SimpleExplosion.prefab). The particle system sends Position, Normal, Color, UV and
// Custom1.xyzw (vertex streams 0,1,3,4,31) so TEXCOORD0 = (uv, custom.xy). Custom1.x dissolves the puff over
// its lifetime against the noise texture; _Bias lifts the dark side of the lighting.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/ParticleCustomData" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Disolve ("Disolve", Range(0, 1)) = 0
		_Bias ("Bias", Range(0, 1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }

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
			float _Disolve;
			float _Bias;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color : COLOR;
				float4 texcoord0 : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float dissolve : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
				float4 color : COLOR;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord0.xy, _MainTex);
				o.dissolve = v.texcoord0.z;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.color = v.color;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float noise = tex2D(_MainTex, i.uv).r;
				clip(noise - saturate(i.dissolve + _Disolve));
				float ndotl = dot(normalize(i.worldNormal), normalize(_WorldSpaceLightPos0.xyz));
				float light = saturate(ndotl * (1.0 - _Bias) + _Bias);
				return float4(i.color.rgb * _LightColor0.rgb * light, 1);
			}
			ENDCG
		}
	}
}
