Shader "Unlit/Transform Horizontal Cubemap Video" {
	Properties{
		_Tint("Tint Color", Color) = (.5, .5, .5, .5)
		[Gamma] _Exposure("Exposure", Range(0, 8)) = 1.0
		_Rotation("Rotation", Range(0, 360)) = 0
		_MainTex("Transform Format", 2D) = "grey" {}
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Opaque" "PreviewType"="Plane" }
	Cull Off ZWrite Off
	
	CGINCLUDE
	#include "UnityCG.cginc"

	half4 _Tint;
	half _Exposure;
	float _Rotation;
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
	v2f fb_vert (appdata_t v)
	{
		v2f o;
		//float3 rotated = RotateAroundXInDegrees(v.vertex, 45); // for normal videoplayer (instead of silly Gst)
		//float3 rotated = RotateAroundXInDegrees(v.vertex, 135);
		float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
		o.vertex = UnityObjectToClipPos(rotated);

		//o.texcoord = v.texcoord;
		o.texcoord = RotateUVInDegrees(v.texcoord, 90);

		//float2 mid = float2(_MainTex_ST.z + (0.25 + trans.x * 0.5) * _MainTex_ST.x, _MainTex_ST.w + (0.166667 + trans.y * 0.333333) * _MainTex_ST.y);
		//float2 scale = float2(_Squeeze * 0.5 * _MainTex_ST.x, _Squeeze * 0.33333 * _MainTex_ST.y);
		//o.texcoord = o.texcoord * scale + mid;
		o.texcoord = o.texcoord * float2(_MainTex_ST.x, _MainTex_ST.y) + float2(_MainTex_ST.z, _MainTex_ST.w);

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
		v2f vert(appdata_t v) { return fb_vert(v); }
		ENDCG 
	} 
}
}
