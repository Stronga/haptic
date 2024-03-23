// Unlit shader. Simplest possible textured shader.
// - SUPPORTS lightmap
// - no lighting
// - no per-material color
Shader "Hidden/Headjack/Video Default"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "black" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "Queue" = "Background" }
		LOD 100
		cull back
		ZWrite off
		Pass
		{
			Lighting Off
			Cull back
			SetTexture[_MainTex]{ combine texture }
		}
	}
}
