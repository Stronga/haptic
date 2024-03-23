// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Headjack/BlurImage"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Direction("Direction",Vector)=(1,0,0,0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

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

			v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = uv;
				return o;
			}
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			half4 _Direction;
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv)* 0.2320291;
				c += tex2D(_MainTex, i.uv + _Direction.xy) * 0.1965423;
				c += tex2D(_MainTex, i.uv + (_Direction.xy * -1)) * 0.1965423;
				c += tex2D(_MainTex, i.uv + (_Direction.xy * 2)) * 0.1191992;
				c += tex2D(_MainTex, i.uv + (_Direction.xy * -2)) * 0.1191992;
				c += tex2D(_MainTex, i.uv + (_Direction.xy * 3)) * 0.0518653;
				c += tex2D(_MainTex, i.uv + (_Direction.xy * -3)) * 0.0518653;
				c += tex2D(_MainTex, i.uv + (_Direction.xy * 4)) * 0.0163785;
				c += tex2D(_MainTex, i.uv + (_Direction.xy * -4)) * 0.0163785;
				return c;
			}
			ENDCG
		}
	}
}
