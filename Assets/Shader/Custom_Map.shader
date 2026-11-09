// World map continent backgrounds (MapBackground.cs). The texture stores terrain height as grey
// ((height + 13) / 200), which is mapped to sand near sea level and _Ground inland; sea is left transparent
// so the map's own ocean background shows through. UI-compatible (stencil/clip rect/colour mask).
// Reconstructed for the WebGL port (original source not recoverable from the build).
Shader "Custom/Map" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_Ground ("Ground", Color) = (1,1,1,1)
		_Water ("Water", Color) = (1,1,1,1)
		_Sand ("Sand", Color) = (1,1,1,1)
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct appdata_t
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed4 _Ground;
			fixed4 _Water;
			fixed4 _Sand;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.color = v.color * _Color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// Height texture is written with Texture2D.SetPixels (sRGB texture) so read back the stored value.
				float stored = tex2D(_MainTex, i.texcoord).r;
				#ifndef UNITY_COLORSPACE_GAMMA
				stored = LinearToGammaSpace(float3(stored, stored, stored)).r;
				#endif
				float height = stored * 200.0 - 13.0;
				fixed4 col = (height < 0.0) ? fixed4(_Water.rgb, 0.0) : ((height < 6.0) ? _Sand : _Ground);
				// Higher ground gets slightly lighter, like a relief map.
				col.rgb *= (height < 6.0) ? 1.0 : lerp(0.92, 1.12, saturate(height / 160.0));
				col *= i.color;
				#ifdef UNITY_UI_CLIP_RECT
				col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
				#endif
				#ifdef UNITY_UI_ALPHACLIP
				clip(col.a - 0.001);
				#endif
				return col;
			}
			ENDCG
		}
	}
}
