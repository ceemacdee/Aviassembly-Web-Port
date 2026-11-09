// Replacement shader used by DragSimulator.RenderDragTexture(). The drag camera (orthographic, "Plane Part"
// layer only, cleared to black) renders the plane as seen by the oncoming air; DragSimulator averages the red
// channel, so each covered pixel writes how much drag that surface produces (1 - _DragFactor).
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Drag/DragFactorReplacement" {
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
		float3 worldPos : TEXCOORD0;
	};

	v2f vert(appdata_base v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		return o;
	}

	float4 fragDrag(v2f i) : SV_Target
	{
		return float4(1.0 - _DragFactor, 0, 0, 1);
	}

	float4 fragWheel(v2f i) : SV_Target
	{
		// Retracted gear (_DragFactor = 1) is tucked away: it neither adds drag nor hides parts behind it.
		clip(0.999 - _DragFactor);
		if (_WorldToLocal[3][3] != 0.0)
		{
			clip(_RefrenceHeight - mul(_WorldToLocal, float4(i.worldPos, 1.0)).y);
		}
		return float4(1.0 - _DragFactor, 0, 0, 1);
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
