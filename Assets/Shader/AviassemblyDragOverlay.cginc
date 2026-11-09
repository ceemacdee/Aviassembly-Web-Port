// Drag overlay pass used by Custom/Drag/DragReplacement (DragOverlayRenderer's replacement shader on the
// building camera). Surfaces hit first by the airflow (according to _DragDepthTexture from DragSimulator) are
// tinted by how much drag they produce; everything else is shown darkened. _DragMainTexVisibility fades back
// to the normal stylized look when the overlay is switched off.
#ifndef AVIASSEMBLY_DRAG_OVERLAY_INCLUDED
#define AVIASSEMBLY_DRAG_OVERLAY_INCLUDED

#include "AviassemblyCommon.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;
float _Shade;
float _Smoothness;
float _TextureOpacity;
float _DragFactor;
float4x4 _WorldToLocal;
float _RefrenceHeight;

struct OverlayV2F
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 worldNormal : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
	UNITY_LIGHTING_COORDS(3, 4)
};

OverlayV2F OverlayVert(appdata_base v)
{
	OverlayV2F o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
	o.worldNormal = UnityObjectToWorldNormal(v.normal);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	UNITY_TRANSFER_LIGHTING(o, v.texcoord.xy);
	return o;
}

float4 OverlayFrag(OverlayV2F i) : SV_Target
{
#ifdef AVI_WHEEL_CLIP
	if (_WorldToLocal[3][3] != 0.0)
	{
		clip(_RefrenceHeight - mul(_WorldToLocal, float4(i.worldPos, 1.0)).y);
	}
#endif
	float3 n = normalize(i.worldNormal);
	float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
	UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
	float3 albedo = AviAlbedo(_Color, tex2D(_MainTex, i.uv), _TextureOpacity);
	float3 normalLook = AviStylizedMainLight(albedo, n, lightDir, _LightColor0.rgb, atten, _Shade, _Smoothness);

	float lit = lerp(0.65, 1.0, AviLightRamp(dot(n, lightDir), 0.5));
	float exposure = AviDragExposure(i.worldPos);
	float3 heat = AviDragHeatColor(saturate(1.0 - _DragFactor)) * lit;
	float3 shielded = lerp(float3(0.35, 0.36, 0.38), normalLook, 0.25) * lit;
	float3 overlay = lerp(shielded, heat, exposure);
	return float4(lerp(overlay, normalLook, saturate(_DragMainTexVisibility)), 1);
}

#endif
