﻿Shader "Unlit/UI_CatSwitchFill"
{
	Properties
	{
		_MainTex("_",2D) = "white"{}
		_FillColor("Fill Color",Color) = (1,1,1,1)
		_AAMask("AA MAsk",2D) = "white"{}
		_Fill("Fill",Range(0,1))=0.5
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
				float2 canvasPosition : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D  _AAMask;
			
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
				o.uv = v.uv;
				o.canvasPosition = v.vertex.xy;
				o.color = v.color;
				return o;
			}
			half _Fill;
			fixed4 _FillColor;
			fixed4 frag (v2f i) : SV_Target
			{
				float mask = tex2D(_AAMask, i.uv).a;
				half alpha = mask*i.color.a;
				clip(alpha-.001);
				float fill = saturate(sign(_Fill-i.uv.y ));
				return fill*_FillColor *float4(1, 1, 1, alpha);
			}
			ENDCG
		}
	}
}