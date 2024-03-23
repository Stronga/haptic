Shader "Unlit/CurvedUI"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color)=(1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 100
		blend srcAlpha oneMinusSrcAlpha
		zwrite off
		cull off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0, float4 color : COLOR)
			{
				v2f o;
				float3 vertexWorld = mul(unity_ObjectToWorld, vertex).xyz;
				float angle = vertexWorld.x / (2*3.1415*vertexWorld.z)*(2 * 3.1415);
				float3 curvedPos = float3(sin(angle)*vertexWorld.z, vertexWorld.y, cos(angle)*vertexWorld.z);
				o.vertex = UnityObjectToClipPos(mul(unity_WorldToObject, float4(curvedPos,1)));
				o.uv = TRANSFORM_TEX(uv, _MainTex);
				o.color = color;
				return o;
			}
			fixed4 _Color;
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col*i.color;
			}
			ENDCG
		}
	}
}
