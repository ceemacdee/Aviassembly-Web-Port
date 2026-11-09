// Not referenced by any scene, prefab or material in the shipped game (an earlier drag overlay experiment).
// Kept as a simple unlit preview of the airflow exposure so the asset and its GUID remain valid.
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/DragOverlaySample" {
	Properties {
		_ShallowColor ("Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Replacement"="Drag" }
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "AviassemblyCommon.cginc"
			float4 _ShallowColor;
			float _DragFactor;

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

			float4 frag(v2f i) : SV_Target
			{
				float exposure = AviDragExposure(i.worldPos);
				float3 col = lerp(_ShallowColor.rgb * 0.4, AviDragHeatColor(saturate(1.0 - _DragFactor)), exposure);
				return float4(col, 1);
			}
			ENDCG
		}
	}
}
