// Forward base / forward add / shadow caster passes for the stylized lit look used by most Aviassembly
// materials. Optional features (define before including):
//   AVI_VERTEX_COLOR  multiply albedo by the vertex colour (particles)
//   AVI_WHEEL_CLIP    hide landing gear geometry above _RefrenceHeight in the gear's local space
//   AVI_FORWARD_ADD   compile the additive per-pixel light variant
//   AVI_SHADOW_CASTER compile the shadow caster / depth variant
#ifndef AVIASSEMBLY_STYLIZED_INCLUDED
#define AVIASSEMBLY_STYLIZED_INCLUDED

#include "AviassemblyCommon.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
float _TextureOpacity;
float _DragFactor;

UNITY_INSTANCING_BUFFER_START(AviProps)
	UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
	UNITY_DEFINE_INSTANCED_PROP(float, _Shade)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(AviProps)

#ifdef AVI_WHEEL_CLIP
float4x4 _WorldToLocal;
float _RefrenceHeight;

void AviWheelClip(float3 worldPos)
{
	// _WorldToLocal is only set on retractable gear; an unset (all zero) matrix disables clipping.
	if (_WorldToLocal[3][3] != 0.0)
	{
		float3 localPos = mul(_WorldToLocal, float4(worldPos, 1.0)).xyz;
		clip(_RefrenceHeight - localPos.y);
	}
}
#endif

#ifdef AVI_SHADOW_CASTER

struct AviShadowV2F
{
	V2F_SHADOW_CASTER;
#ifdef AVI_WHEEL_CLIP
	float3 worldPos : TEXCOORD1;
#endif
	UNITY_VERTEX_OUTPUT_STEREO
};

AviShadowV2F AviShadowVert(appdata_base v)
{
	AviShadowV2F o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
#ifdef AVI_WHEEL_CLIP
	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
#endif
	return o;
}

float4 AviShadowFrag(AviShadowV2F i) : SV_Target
{
#ifdef AVI_WHEEL_CLIP
	AviWheelClip(i.worldPos);
#endif
	SHADOW_CASTER_FRAGMENT(i)
}

#else

struct AviAppdata
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
#ifdef AVI_VERTEX_COLOR
	float4 color : COLOR;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AviV2F
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 worldNormal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
	UNITY_LIGHTING_COORDS(3, 4)
#ifdef AVI_VERTEX_COLOR
	float4 color : COLOR;
#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

AviV2F AviVert(AviAppdata v)
{
	AviV2F o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.uv, _MainTex);
	o.worldNormal = UnityObjectToWorldNormal(v.normal);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
#ifdef AVI_VERTEX_COLOR
	o.color = v.color;
#endif
	UNITY_TRANSFER_LIGHTING(o, v.uv);
	return o;
}

float3 AviSurfaceAlbedo(AviV2F i)
{
	float4 color = UNITY_ACCESS_INSTANCED_PROP(AviProps, _Color);
	float3 albedo = AviAlbedo(color, tex2D(_MainTex, i.uv), _TextureOpacity);
#ifdef AVI_VERTEX_COLOR
	albedo *= i.color.rgb;
#endif
	return albedo;
}

float4 AviFrag(AviV2F i) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(i);
#ifdef AVI_WHEEL_CLIP
	AviWheelClip(i.worldPos);
#endif
	float3 albedo = AviSurfaceAlbedo(i);
	float3 n = normalize(i.worldNormal);
	UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
	float shade = UNITY_ACCESS_INSTANCED_PROP(AviProps, _Shade);
	float smoothness = UNITY_ACCESS_INSTANCED_PROP(AviProps, _Smoothness);
#ifdef AVI_FORWARD_ADD
	float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
	return float4(AviStylizedAddLight(albedo, n, lightDir, _LightColor0.rgb, atten, smoothness), 0);
#else
	float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
	float3 col = AviStylizedMainLight(albedo, n, lightDir, _LightColor0.rgb, atten, shade, smoothness);
	return float4(col, 1);
#endif
}

#endif
#endif
