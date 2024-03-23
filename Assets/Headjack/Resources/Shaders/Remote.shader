Shader "Hidden/Headjack/ControllerVideo" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200
		GrabPass { "_MyGrabTexture" }
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0
		sampler2D _MainTex, _MyGrabTexture;
		float4 _MyGrabTexture_TexelSize;
		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float4 screenPos;
		};
		uniform half _ShowController;
		void surf (Input IN, inout SurfaceOutputStandard o) {
			half2 ScreenUV=half2(IN.screenPos.x,IN.screenPos.y)/IN.screenPos.w;
			fixed3 c=fixed3(0,0,0);
			for(int i=0;i<16;++i)
			{
				if (i<8)
				{
				 c+= tex2D (_MyGrabTexture, ScreenUV+half2(sin(0.785*i)*_MyGrabTexture_TexelSize.x*(4*(1-_ShowController)),cos(0.785*i)*_MyGrabTexture_TexelSize.y*(4*(1-_ShowController))));
				 }else{
				 c+= tex2D (_MyGrabTexture, ScreenUV+half2(sin(0.785*i)*_MyGrabTexture_TexelSize.x*(8*(1-_ShowController)),cos(0.785*i)*_MyGrabTexture_TexelSize.y*(8*(1-_ShowController))));
				 }
			}
			c=c/16;
			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), o.Normal));
			fixed3 Rimm= fixed3(0.2, 0.2, 0.2) * pow (rim, 5);
			o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb;
			o.Emission = lerp( c.rgb+ Rimm,tex2D(_MainTex,IN.uv_MainTex).rgb+Rimm,_ShowController);
			o.Metallic = 0.7;
			o.Smoothness = 0.2;
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
