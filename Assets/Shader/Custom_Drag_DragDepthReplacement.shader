// Replacement shader used by DragSimulator.RenderDepthTexture(): stores the view depth of the first surface
// the airflow hits, which Custom/DragOverlaySample reads (as _DragDepthTexture) to colour exposed surfaces.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Drag/DragDepthReplacement" {
	Properties {
	}
	CGINCLUDE
	#include "AviassemblyCommon.cginc"
	float _DragFactor;
	float4x4 _WorldToLocal;
	float _RefrenceHeight;

	struct v2f
	{
		float4 pos : SV_POSITION;
		float viewDepth : TEXCOORD0;
		float3 worldPos : TEXCOORD1;
	};

	v2f vert(appdata_base v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.viewDepth = -UnityObjectToViewPos(v.vertex).z;
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		return o;
	}

	float4 fragDrag(v2f i) : SV_Target
	{
		return AviEncodeDragDepth(i.viewDepth);
	}

	float4 fragWheel(v2f i) : SV_Target
	{
		clip(0.999 - _DragFactor);
		if (_WorldToLocal[3][3] != 0.0)
		{
			clip(_RefrenceHeight - mul(_WorldToLocal, float4(i.worldPos, 1.0)).y);
		}
		return AviEncodeDragDepth(i.viewDepth);
	}
	ENDCG

	SubShader {
		Tags { "Replacement"="Drag" }
		Pass {
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragDrag
			ENDCG
		}
	}
	SubShader {
		Tags { "Replacement"="Wheel" }
		Pass {
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragWheel
			ENDCG
		}
	}
}
