// Shared code for the reconstructed Aviassembly shaders (WebGL port).
// The original shader sources were not recoverable from the shipped build (AssetRipper only exported
// placeholders), so these are reconstructions driven by the material properties, the global values set by
// CustomFog.cs / DragSimulator.cs / HighlightRenderer.cs, and the way the game scripts use each shader.
#ifndef AVIASSEMBLY_COMMON_INCLUDED
#define AVIASSEMBLY_COMMON_INCLUDED

#include "UnityCG.cginc"

// ---- Globals written by CustomFog.cs -----------------------------------------------------------
uniform float4 _CustomFogColor;        // colour at the horizon
uniform float4 _CustomSkyColor;        // colour at the zenith
uniform float _CustomFogFactor;        // fog end distance (= draw distance), 0 when fog is disabled
uniform float _CustomFogOffset;        // fog start distance
uniform float _CustomHeightRefrence;   // altitude at which the horizon band has fully thinned out
uniform float _CustomExponent;         // sharpness of the horizon -> zenith gradient

// Sky colour for a view direction: horizon fog colour blending into the sky colour towards the zenith.
float3 AviSkyGradient(float3 dir, float cameraHeight)
{
	float exponent = max(_CustomExponent, 0.0001);
	// The horizon band dips slightly as the player climbs.
	float heightShift = (_CustomHeightRefrence > 0.0) ? saturate(cameraHeight / _CustomHeightRefrence) * 0.15 : 0.0;
	float up = saturate(dir.y + heightShift);
	float horizon = pow(1.0 - up, exponent);
	return lerp(_CustomSkyColor.rgb, _CustomFogColor.rgb, horizon);
}

// 0 = no fog, 1 = fully fogged. Distance based, starts at _CustomFogOffset and ends at the draw distance.
float AviFogAmount(float distance)
{
	if (_CustomFogFactor <= 0.0)
	{
		return 0.0;
	}
	float range = max(_CustomFogFactor - _CustomFogOffset, 1.0);
	float f = saturate((distance - _CustomFogOffset) / range);
	return f * f * (3.0 - 2.0 * f);
}

// Fog for forward-rendered transparent geometry (opaque geometry is fogged by the Custom/Fog image effect).
float3 AviApplyFog(float3 color, float3 worldPos)
{
	float3 toPixel = worldPos - _WorldSpaceCameraPos;
	float distance = length(toPixel);
	float3 dir = toPixel / max(distance, 0.0001);
	return lerp(color, AviSkyGradient(dir, _WorldSpaceCameraPos.y), AviFogAmount(distance));
}

// ---- Stylized lighting ------------------------------------------------------------------------
// _Shade      : brightness of the side facing away from the light (1 = no shading).
// _Smoothness : softness of the light -> shade transition (0 = hard toon edge, 1 = very soft).
float AviLightRamp(float ndotl, float smoothness)
{
	float w = max(smoothness, 0.02);
	return smoothstep(-w, w, ndotl);
}

float3 AviStylizedMainLight(float3 albedo, float3 normalWS, float3 lightDirWS, float3 lightColor, float atten, float shade, float smoothness)
{
	float ramp = AviLightRamp(dot(normalWS, lightDirWS), smoothness) * atten;
	return albedo * lightColor * lerp(shade, 1.0, ramp);
}

float3 AviStylizedAddLight(float3 albedo, float3 normalWS, float3 lightDirWS, float3 lightColor, float atten, float smoothness)
{
	float ramp = AviLightRamp(dot(normalWS, lightDirWS), smoothness) * atten;
	return albedo * lightColor * ramp;
}

// _TextureOpacity blends between a flat colour (0) and the full texture (1).
float3 AviAlbedo(float4 color, float4 tex, float textureOpacity)
{
	return color.rgb * lerp(float3(1, 1, 1), tex.rgb, textureOpacity);
}

// ---- Drag simulation (DragSimulator.cs / DragOverlayRenderer.cs) ------------------------------
// _DragFactor is the part's "aerodynamic-ness": the drag camera accumulates (1 - _DragFactor) in the red
// channel, so retracted gear (_DragFactor = 1, see RetractableLandingGear.SetRetracted) adds no drag.
#define AVI_DRAG_DEPTH_RANGE 2048.0

uniform sampler2D _DragDepthTexture;
uniform float4x4 _DragViewMatrix;
uniform float4x4 _DragProjectionMatrix;
uniform float _DragMainTexVisibility;

// The drag render textures are default ARGB32 targets which, in this linear colour space project, are sRGB.
// Exact sRGB conversions let us store bytes losslessly through the hardware encode/decode.
float AviSRGBToLinear(float c)
{
	return (c <= 0.04045) ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4);
}

float AviLinearToSRGB(float c)
{
	return (c <= 0.0031308) ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

// View depth packed as 16 bits (R = high byte, G = low byte), B = 1 marks covered pixels.
float4 AviEncodeDragDepth(float viewDepth)
{
	float v = clamp(viewDepth / AVI_DRAG_DEPTH_RANGE, 0.0, 1.0) * 65535.0;
	float hi = floor(v / 256.0);
	float lo = floor(v - hi * 256.0);
	float3 bytes = float3(hi, lo, 255.0) / 255.0;
#ifndef UNITY_COLORSPACE_GAMMA
	bytes = float3(AviSRGBToLinear(bytes.x), AviSRGBToLinear(bytes.y), 1.0);
#endif
	return float4(bytes, 1.0);
}

// Returns view depth in metres, or -1 when nothing was rendered at that pixel.
float AviDecodeDragDepth(float4 sampled)
{
	float3 bytes = sampled.rgb;
#ifndef UNITY_COLORSPACE_GAMMA
	bytes = float3(AviLinearToSRGB(bytes.x), AviLinearToSRGB(bytes.y), AviLinearToSRGB(bytes.z));
#endif
	bytes = round(bytes * 255.0);
	if (bytes.z < 128.0)
	{
		return -1.0;
	}
	return (bytes.x * 256.0 + bytes.y) / 65535.0 * AVI_DRAG_DEPTH_RANGE;
}

// Returns 1 when worldPos is the surface the airflow hits first (visible from the drag camera).
float AviDragExposure(float3 worldPos)
{
	float4 dragView = mul(_DragViewMatrix, float4(worldPos, 1.0));
	float4 dragClip = mul(_DragProjectionMatrix, dragView);
	float2 ndc = dragClip.xy / max(abs(dragClip.w), 0.00001);
	if (any(abs(ndc) > 1.0))
	{
		return 0.0;
	}
	float2 uv = ndc * 0.5 + 0.5;
	float storedDepth = AviDecodeDragDepth(tex2Dlod(_DragDepthTexture, float4(uv, 0, 0)));
	if (storedDepth < 0.0)
	{
		return 0.0;
	}
	float myDepth = -dragView.z;
	return (myDepth - storedDepth) < 0.08 ? 1.0 : 0.0;
}

// Colour used by the drag overlay: blue for low drag through to orange-red for high drag.
float3 AviDragHeatColor(float drag)
{
	float3 low = float3(0.25, 0.62, 0.86);
	float3 mid = float3(0.98, 0.84, 0.35);
	float3 high = float3(0.86, 0.29, 0.17);
	return drag < 0.5 ? lerp(low, mid, drag * 2.0) : lerp(mid, high, drag * 2.0 - 1.0);
}

#endif
