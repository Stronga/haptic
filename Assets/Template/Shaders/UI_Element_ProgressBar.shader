﻿Shader "Unlit/UI_Element_ProgressBar"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Bar("Bar", 2D) = "black" {}
		_Progress("Progress", Range(0,1)) = 0
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
			_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100
		blend srcAlpha oneMinusSrcAlpha
		//ztest Always
		zwrite off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile CURVED_OFF CURVED_ON
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
#if CURVED_ON
				float3 vertexWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
				float angle = vertexWorld.x / (2 * 3.1415*vertexWorld.z)*(2 * 3.1415);
				float3 curvedPos = float3(sin(angle)*vertexWorld.z, vertexWorld.y, cos(angle)*vertexWorld.z);
				o.vertex = UnityObjectToClipPos(mul(unity_WorldToObject, float4(curvedPos, 1)));
#else
				o.vertex = UnityObjectToClipPos(v.vertex);
#endif
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}
			half _Progress;
			sampler2D _Bar;
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex,i.uv);

				fixed progress = tex2D(_Bar, i.uv+float2(1-_Progress,0)).r*col.r;
				return i.color * float4(1,1,1,saturate( col.g + progress));
			
			}
			ENDCG
		}
	}
}
