Shader "Hidden/Headjack/Subtitles-1"
{
	Properties {
		_MainTex ("Font Texture", 2D) = "white" {}
		_Color ("Text Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry+2" }
		LOD 100
		Lighting Off Cull Off ZWrite Off
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
			};
			sampler2D _MainTex;
			float4 _MainTex_ST;
			v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = TRANSFORM_TEX(uv, _MainTex);
				return o;
			}
			fixed4 _Color;
			fixed4 frag (v2f i) : SV_Target
			{
				clip(tex2D(_MainTex, i.uv).a-0.01);
				return _Color*tex2D(_MainTex, i.uv).a;
			}
			ENDCG
		}
	}
}
