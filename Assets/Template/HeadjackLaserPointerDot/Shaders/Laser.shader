// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/Laser"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FadeStart("Fade Start",Range(0,1024)) = 0
		_FadeEnd("Fade End",Range(0,10)) = 0
		_Show("Show",Range(0,1)) = 1
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
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float3 worldViewDir : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.worldViewDir = normalize(mul((float3x3)unity_CameraToWorld, float3(0, 0, 1)));
				return o;
			}

			float3 Project(float3 v, float3 n)
			{
				return n * dot(v, n) / dot(n, n);
			}

			half _Show;
			half _FadeEnd, _FadeStart;
			fixed4 frag (v2f i) : SV_Target
			{
				fixed c = tex2D(_MainTex, i.uv).r;
				clip(c - (1.0/256.0));
				float3 projectedLocal = Project(i.worldPos - _WorldSpaceCameraPos, i.worldViewDir);
				float d = saturate((length(projectedLocal)- _ProjectionParams.y)*128);
				return fixed4(1,1,1, c * pow(1 - i.uv.y, _FadeEnd)*d*_Show);
			}
			ENDCG
		}
	}
}
