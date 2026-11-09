// Landing gear: geometry above _RefrenceHeight (gear-local space, see RetractableLandingGear.UpdateMatrix)
// is clipped so retracting gear disappears into the airframe.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Wheel Shader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0, 1)) = 0
		_Shade ("Shade", Range(0, 1)) = 0
		_TextureOpacity ("Texture Opacity", Range(0, 1)) = 0
		_RefrenceHeight ("Refrence Height", Float) = 0
		_DragFactor ("Drag Factor", Range(0, 1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "Replacement"="Wheel" }
		LOD 200

		Pass {
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex AviVert
			#pragma fragment AviFrag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_instancing
			#define AVI_WHEEL_CLIP
			#include "AviassemblyStylized.cginc"
			ENDCG
		}

		Pass {
			Tags { "LightMode"="ForwardAdd" }
			Blend One One
			ZWrite Off
			CGPROGRAM
			#pragma vertex AviVert
			#pragma fragment AviFrag
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_instancing
			#define AVI_FORWARD_ADD
			#define AVI_WHEEL_CLIP
			#include "AviassemblyStylized.cginc"
			ENDCG
		}

		Pass {
			Tags { "LightMode"="ShadowCaster" }
			CGPROGRAM
			#pragma vertex AviShadowVert
			#pragma fragment AviShadowFrag
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#define AVI_SHADOW_CASTER
			#define AVI_WHEEL_CLIP
			#include "AviassemblyStylized.cginc"
			ENDCG
		}
	}
	Fallback Off
}
