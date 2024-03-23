Shader "Unlit/Thumbnail"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Mask("Mask",2D) = "black"{}
		_Alpha("Alpha",Range(0,1))=1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		LOD 100
		blend srcAlpha oneMinusSrcAlpha
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
				float2 uvMask : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex, _Mask;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvMask = v.uv;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			half _Alpha;
			fixed4 frag (v2f i) : SV_Target
			{
				float2 mask = tex2D(_Mask,i.uvMask);
				fixed3 col = lerp(1,tex2D(_MainTex, i.uv).rgb,mask.r);
				return fixed4(col.rgb, mask.g*_Alpha);
			}
			ENDCG
		}
	}
}
