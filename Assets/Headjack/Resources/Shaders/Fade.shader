Shader "Hidden/Headjack/Fade"
{
	Properties
	{
		_Fade("Fade",range(0,1))=0
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent+2000" }
		Cull back ZWrite Off ZTest Always
		blend srcAlpha oneMinusSrcAlpha
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			struct v2f
			{
				float4 vertex : SV_POSITION;
			};
			v2f vert (float4 vertex : POSITION)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				return o;
			}
			half _Fade;
			fixed4 frag (v2f i) : SV_Target{ return fixed4(0,0,0,_Fade); }
			ENDCG
		}
	}
}
