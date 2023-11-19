// WarFX Shader
// (c) 2015 Jean Moreno

Shader "WFX/Transparent Specular"
{
	Properties
	{
		_TintColor ("Main Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
		_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Cull Off
		LOD 200
		
		CGPROGRAM
		#pragma surface surf BlinnPhong alpha noforwardadd approxview alpha:fade
		
		sampler2D _MainTex;
		fixed4 _TintColor;
		half _Shininess;
		
		struct Input
		{
			float2 uv_MainTex;
			float4 color:COLOR;
		};
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb * _TintColor.rgb * IN.color.rgb;
			o.Alpha = c.a * _TintColor.a * IN.color.a;
			o.Specular = _Shininess;
			o.Gloss = c.rgb + 0.1;
		}
		ENDCG
	}
	
	Fallback "Particles/Alpha Blended"
}
