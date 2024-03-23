Shader "Headjack/EquiProjection" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "black" {}
	}
	SubShader
	{
			Tags { "RenderType" = "Opaque" "Queue" = "Background+1" }
			LOD 100
			cull back
			ZWrite off
			Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 pos : TEXCOORD0;
			};
			v2f vert(float4 vertex : POSITION)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				o.pos = vertex;
				return o;
			}
			float2 WorldToEqui(float3 wp)
			{
				return float2(atan2(wp.x, wp.z)*0.15915495087, asin(wp.y)*0.3183099524);
			}
			fixed4 frag(v2f i) : SV_Target
			{
				float2 uv = (WorldToEqui(normalize(i.pos))) + 0.5;
#if SHADER_API_METAL
				uv.y = 1 - uv.y;
#endif
				return tex2D(_MainTex, uv*_MainTex_ST.xy + _MainTex_ST.zw);
			}
			ENDCG
		}
	}
}
 