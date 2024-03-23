Shader "Unlit/EssentialsColorRoom2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DetailTex ("Detail Texture", 2D) = "white" {}
		_Color1 ("Color 1", Color) = (1, 0, 0, 1)
		_Color2 ("Color 2", Color) = (0, 1, 0, 1)
		_Color3 ("Color 3", Color) = (0, 0, 1, 1)
        _Color4 ("Color 4", Color) = (1, 1, 1, 1)
        _DetailIntensity ("Detail Intensity", Range(0, 1)) = 0
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
                // float2 texcoord2 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 texcoord2 : TEXCOORD1;
                UNITY_FOG_COORDS(2)
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
            sampler2D _DetailTex;
            float4 _DetailTex_ST;
            float _DetailIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.texcoord2 = TRANSFORM_TEX(v.uv, _DetailTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
			float4 _Color1, _Color2, _Color3, _Color4;
            float4 frag (v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float di = _DetailIntensity;
#if GAMMA_CORRECT
                c.rgb = GammaToLinear(c.rgb);
                di = 6 * di;
#endif
                float4 d = tex2D(_DetailTex, i.texcoord2);
				float4 result = float4(0,0,0,1);
				result += _Color1 * c.r;
				result += _Color2 * c.g;
				result += _Color3 * c.b;
				result += _Color4 * c.a;
                result = (1 - di) * result + di * d;
                return result;
            }
            ENDCG
        }
    }
}
