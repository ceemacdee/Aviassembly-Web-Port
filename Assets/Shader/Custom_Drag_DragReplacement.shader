// Replacement shader set on the building camera by DragOverlayRenderer.ToggleOverlay() ("Drag overlay").
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Drag/DragReplacement" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Replacement"="Drag" }
		Pass {
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex OverlayVert
			#pragma fragment OverlayFrag
			#pragma multi_compile_fwdbase
			#include "AviassemblyDragOverlay.cginc"
			ENDCG
		}
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Replacement"="Wheel" }
		Pass {
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex OverlayVert
			#pragma fragment OverlayFrag
			#pragma multi_compile_fwdbase
			#define AVI_WHEEL_CLIP
			#include "AviassemblyDragOverlay.cginc"
			ENDCG
		}
	}
}
