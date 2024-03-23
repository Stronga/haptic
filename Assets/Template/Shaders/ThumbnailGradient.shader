Shader "Unlit/ThumbnailGradient"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags {"RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100
		ztest always
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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
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
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float fade = saturate(1-((i.uv.x - 0.8125)*(1.0/0.1875)));
				return tex2D(_MainTex, i.uv)*float4(fade,fade,fade,1);
			}
			ENDCG
		}
	}
}
