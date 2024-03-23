Shader "Unlit/SeekBar"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Value("Value",Range(0,1)) = 0.5
		_ColorBar("Color Bar Full",Color)=(1,0,0,1)
		_ColorBarEmpty("Color Bar Empty",Color)=(1,0,0,1)
		_ColorOutline("Color Outline",Color) = (0,1,0,1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent""Queue"="Transparent" }
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
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}
			half _Value;
			fixed4 _ColorBar, _ColorOutline, _ColorBarEmpty;
			fixed4 frag (v2f i) : SV_Target
			{
				fixed3 bar = tex2D(_MainTex, i.uv).rga;
				float outline = bar.g;
				float mask = bar.r;
				float value = tex2D(_MainTex, i.uv - float2(_Value*.936, 0)).b * mask;
				float4 result = mask*_ColorBarEmpty;
				result = lerp(result, value*_ColorBar, value);
				result = lerp(result, outline*_ColorOutline, outline);
				return result*i.color;
			}
			ENDCG
		}
	}
}
