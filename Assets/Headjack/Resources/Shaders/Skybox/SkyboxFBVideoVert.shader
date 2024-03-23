Shader "Skybox/Facebook Vertical Video" {
	Properties{
		_Tint("Tint Color", Color) = (.5, .5, .5, .5)
		[Gamma] _Exposure("Exposure", Range(0, 8)) = 1.0
		_Rotation("Rotation", Range(0, 360)) = 45
		_Squeeze("Squeeze", Range(0, 1)) = 0.975
		_MainTex("Facebook Format", 2D) = "grey" {}
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off
	
	CGINCLUDE
	#include "UnityCG.cginc"

	half4 _Tint;
	half _Exposure;
	float _Rotation;
	float _Squeeze;
	float4 _MainTex_ST;

	float3 RotateAroundYInDegrees (float3 vertex, float degrees)
	{
		float alpha = degrees * UNITY_PI / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		return float3(mul(m, vertex.xz), vertex.y).xzy;
	}

	float3 RotateAroundXInDegrees(float3 vertex, float degrees)
	{
		float alpha = degrees * UNITY_PI / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		return float3(vertex.x, mul(m, vertex.yz));
	}

	float2 RotateUVInDegrees(float2 texcoord, float degrees)
	{
		float alpha = degrees * UNITY_PI / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		return float2(mul(texcoord, m));
	}
	
	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};
	struct v2f {
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};
	v2f fb_vert (appdata_t v, half2 trans, half rot)
	{
		v2f o;
		float3 rotated = RotateAroundXInDegrees(v.vertex, 45); // for normal videoplayer (instead of silly Gst)
		//float3 rotated = RotateAroundXInDegrees(v.vertex, 135);
		rotated = RotateAroundYInDegrees(rotated, _Rotation);
		o.vertex = UnityObjectToClipPos(rotated);

		o.texcoord = (v.texcoord - 0.5);
		o.texcoord = RotateUVInDegrees(o.texcoord, rot);

		float2 mid = float2(0.25 + trans.x * 0.5, 0.166667 + trans.y * 0.333333);
		float2 scale = float2(_Squeeze * 0.5 * _MainTex_ST.x, _Squeeze * 0.33333 * _MainTex_ST.y);
		o.texcoord = o.texcoord * scale + mid;

		return o;
	}

	half4 skybox_frag(v2f i, sampler2D smp, half4 smpDecode)
	{
		half4 tex = tex2D(smp, i.texcoord);
		half3 c = DecodeHDR(tex, smpDecode);
		c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
		c *= _Exposure;
		return half4(c, 1);
	}
	ENDCG
	
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0
		sampler2D _MainTex;
		half4 _MainTex_HDR;
		half4 frag (v2f i) : SV_Target { return skybox_frag(i,_MainTex, _MainTex_HDR); }
		v2f vert(appdata_t v) { return fb_vert(v, half2(0, 1), 90); }
		ENDCG 
	} 
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0
		sampler2D _MainTex;
		half4 _MainTex_HDR;
		half4 frag(v2f i) : SV_Target{ return skybox_frag(i,_MainTex, _MainTex_HDR); }
		v2f vert(appdata_t v) { return fb_vert(v, half2(1, 1), 0); }
		ENDCG 
	}
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0
		sampler2D _MainTex;
		half4 _MainTex_HDR;
		half4 frag(v2f i) : SV_Target{ return skybox_frag(i,_MainTex, _MainTex_HDR); }
		v2f vert(appdata_t v) { return fb_vert(v, half2(0, 0), 90); }
		ENDCG
	}
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0
		sampler2D _MainTex;
		half4 _MainTex_HDR;
		half4 frag(v2f i) : SV_Target{ return skybox_frag(i,_MainTex, _MainTex_HDR); }
		v2f vert(appdata_t v) { return fb_vert(v, half2(0, 2), 90); }
		ENDCG 
	}
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0
		sampler2D _MainTex;
		half4 _MainTex_HDR;
		half4 frag(v2f i) : SV_Target{ return skybox_frag(i,_MainTex, _MainTex_HDR); }
		v2f vert(appdata_t v) { return fb_vert(v, half2(1, 2), 180); }
		ENDCG 
	}
	Pass{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 2.0
		sampler2D _MainTex;
		half4 _MainTex_HDR;
		half4 frag(v2f i) : SV_Target{ return skybox_frag(i,_MainTex, _MainTex_HDR); }
		v2f vert(appdata_t v) { return fb_vert(v, half2(1, 0), 180); }
		ENDCG 
	}
}
}
