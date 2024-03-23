Shader "Unlit/LaserPointer"
{
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Overlay" }
		LOD 100
		blend srcAlpha oneMinusSrcAlpha
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 laser : SV_Target;
				float4 vertex : SV_POSITION;
			};
			uniform fixed4 _LaserColor;
			uniform float _LaserLength;
			v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(vertex);
				o.laser=fixed4(_LaserColor.rgb,uv.y*_LaserLength*3.3333);
				return o;
			}
			fixed4 frag (v2f i) : SV_Target
			{
				return saturate(i.laser);
			}
			ENDCG
		}
	}
}
