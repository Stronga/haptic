Shader "Headjack/EquiToCubeProjection" {
	Properties {
		_MainTex("Albedo (RGB)", 2D) = "black" {}
		_StereoIndex ("Stereo Index", Int) = 0
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
			#pragma multi_compile MESH PROJECT LATLONG FISHEYE
			#pragma multi_compile ___ GAMMA_TO_LINEAR
			#pragma multi_compile __ FORCE_TEXTURE_FLIP
			#pragma multi_compile _ MANUAL_STEREO_INDEX
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_Stereo_ST[2];
			int _StereoIndex;

			uniform float _MinBlackLevel;

#if MESH
			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
#else
			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
#endif

#if GAMMA_TO_LINEAR
			fixed3 GammaToLinear(fixed3 col)
			{
				return pow(col, fixed3(2.2, 2.2, 2.2));
			}
#endif

#if MESH
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);

#if MANUAL_STEREO_INDEX
				int stereoEyeIndex = _StereoIndex;
#else 
				int stereoEyeIndex = unity_StereoEyeIndex;
#endif

				o.uv = (v.uv * _MainTex_Stereo_ST[stereoEyeIndex].xy) + _MainTex_Stereo_ST[stereoEyeIndex].zw;

#if FORCE_TEXTURE_FLIP
				o.uv.y = 1 - o.uv.y;
#endif

				return o;
			}
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
#if GAMMA_TO_LINEAR
				col.rgb = GammaToLinear(col.rgb);
#endif
				return col;
			}
#endif
#if PROJECT
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 pos : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.pos = v.vertex;
				return o;
			}
			float2 WorldToEqui(float3 wp)
			{
				return float2(atan2(wp.x, wp.z)*0.15915495087, asin(wp.y)*0.3183099524);
			}
			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 uv = (WorldToEqui(normalize(i.pos))) + 0.5;

#if MANUAL_STEREO_INDEX
				int stereoEyeIndex = _StereoIndex;
#else 
				int stereoEyeIndex = unity_StereoEyeIndex;
#endif

				uv = uv*_MainTex_Stereo_ST[stereoEyeIndex].xy + _MainTex_Stereo_ST[stereoEyeIndex].zw;
#if FORCE_TEXTURE_FLIP
				uv.y = 1 - uv.y;
#endif
				fixed4 col = tex2D(_MainTex, uv);
#if GAMMA_TO_LINEAR
				col.rgb = GammaToLinear(col.rgb);
#endif

				return max(_MinBlackLevel, col);
			}
#endif
#if LATLONG
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 pos : TEXCOORD0;
				float2 latlong : TEXCOORD1;

				UNITY_VERTEX_OUTPUT_STEREO
			};
			uniform float _PROJ_LAT, _PROJ_LONG;
			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.pos = v.vertex;
				o.latlong = float2(360.0 / _PROJ_LAT, 180.0 / _PROJ_LONG);
				return o;
			}

			float2 WorldToEqui(float3 wp)
			{
				return float2(atan2(wp.x, wp.z)*0.15915495087, asin(wp.y)*0.3183099524);
			}
			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 uv = (WorldToEqui(normalize(i.pos)));
				uv = uv * i.latlong + 0.5;
				float shown = (1 - any(floor(uv)));

#if MANUAL_STEREO_INDEX
				int stereoEyeIndex = _StereoIndex;
#else 
				int stereoEyeIndex = unity_StereoEyeIndex;
#endif

				uv = uv * _MainTex_Stereo_ST[stereoEyeIndex].xy + _MainTex_Stereo_ST[stereoEyeIndex].zw;
#if FORCE_TEXTURE_FLIP
				uv.y = 1 - uv.y;
#endif
				fixed4 col = tex2D(_MainTex, uv);
#if GAMMA_TO_LINEAR
				col.rgb = GammaToLinear(col.rgb);
#endif
				return col * shown;
			}
#endif
#if FISHEYE
			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 pos : TEXCOORD0;
				float3 data : TEXCOORD1; //(x=Aspect) (y=Aperture) (z=AngleInRadians)

				UNITY_VERTEX_OUTPUT_STEREO
			};
			half _PROJ_FADE, _PROJ_ANGLE, _PROJ_HEIGHT;
			float4 _MainTex_TexelSize;
			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.pos = mul(unity_ObjectToWorld, v.vertex);
				o.data.x = _MainTex_TexelSize.y / _MainTex_TexelSize.x;
				float width = (_PROJ_HEIGHT * _MainTex_TexelSize.w) / _MainTex_TexelSize.z;
				o.data.y = 1.0 / ((_PROJ_ANGLE / 360.0 * 3.1415 * 2) / width);
				o.data.z = (_PROJ_ANGLE / 360.0) * 3.1415f;
				return o;
			}
			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				i.pos = normalize(i.pos);
				fixed2 r = float2(atan2(length(i.pos.xy),i.pos.z)*i.data.y,atan2(i.pos.y,i.pos.x));
				fixed2 uv = float2(r.x*cos(r.y),r.x*sin(r.y)*i.data.x) + _MainTex_ST.zw;
				float angle = acos(dot(i.pos, float3(0, 0, 1)));
				float border = 1 - pow(1 - saturate(i.data.z - angle), _PROJ_FADE * 16);
				fixed4 c = tex2D(_MainTex,uv + 0.5)*border;
#if GAMMA_TO_LINEAR
				c.rgb = GammaToLinear(c.rgb);
#endif
				return max(_MinBlackLevel, c);
			}
#endif
			ENDCG
		}
	}
}
 