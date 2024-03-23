Shader "Unlit/CurvedFrosted"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Mask("Mask",2D) = "white"{}
		_Refraction("Refraction",Range(0,1)) = .05
	}
	CGINCLUDE
	#include "UnityCG.cginc"
	struct appdata
	{
		float2 uv: TEXCOORD0;
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};
	struct v2f
	{
		float4 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float3 rim : TEXCOORD1;
	};
	sampler2D _PASS4;
	float4 _PASS4_TexelSize;
	sampler2D _MainTex;
	float4 _MainTex_ST;

 
	v2f vert(appdata v)
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
		float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
		o.rim = float3(pow(saturate(1.0 - dot(v.normal, viewDir)), 4), TRANSFORM_TEX(v.uv, _MainTex));
		o.uv = ComputeScreenPos(o.vertex);
		return o;
	}

	sampler2D _Mask, _PASS1;
	float4 _PASS1_TexelSize;
	half _Refraction;
	half4 singlePass(v2f i) : SV_Target
	{
		float2 uv = i.uv.xy / i.uv.w + ((UnpackNormal(tex2D(_MainTex, i.rim.yz)).rg)*_Refraction);
		return saturate((tex2D(_PASS1, uv)+0.1))*float4(1,1,1,tex2D(_Mask, i.rim.yz).a);
	}
	ENDCG
	SubShader
	{
		Tags{ "RenderType" = "Geometry" "Queue" = "Geometry+1" }
		LOD 100
		zwrite off
		blend srcAlpha oneMinusSrcAlpha
		GrabPass
		{
			"_PASS1"
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment singlePass
			#pragma multi_compile CURVED_OFF CURVED_ON
			#pragma target 3.0
			ENDCG
		}
	}
}
