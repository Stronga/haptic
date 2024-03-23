Shader "Unlit/EssentialsColorRoom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Color1("Color 1", Color) = (1, 0, 0, 1)
		_Color2("Color 2", Color) = (0, 1, 0, 1)
		_Color3("Color 3", Color) = (0, 0, 1, 1)
		_Power1("Intensity 1",Range(0,8))=1
		_Power2("Intensity 2",Range(0,8)) = 1
		_Power3("Intensity 3",Range(0,8)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ GAMMA_CORRECT

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

#if GAMMA_CORRECT
			float3 GammaToLinear(float3 col)
			{
				return pow(col, float3(1.4,1.4,1.4));
			}
#endif

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
			half3 _Color1, _Color2, _Color3;
			half _Power1, _Power2, _Power3;
            float4 frag (v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
#if GAMMA_CORRECT
                c.rgb = GammaToLinear(c.rgb);
#endif
				float3 result = (float3)0;
				result += _Color1 * c.r;
				result += _Color2 * c.g;
				result += _Color3 * c.b;
				result += c.a;
                return float4(result,1);
            }
            ENDCG
        }
    }
}
