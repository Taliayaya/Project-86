Shader "AP3X/Bush" {
 
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    _Speed ("Speed", Range (0, 1.0)) = 1.0
    _BumpMap("Normal (RGB)", 2D) = "bump" {}
   

}
 
SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Opaque"}
    LOD 200
   
CGPROGRAM
#pragma target 3.0
#pragma surface surf Lambert alphatest:_Cutoff vertex:vert addshadow


		sampler2D _MainTex;
		fixed4 _Color;
		float _Speed;
		sampler2D _BumpMap;
	
	



 
struct Input {
    float2 uv_MainTex;
};
 
void Calc (float4 val, out float4 s, out float4 c) {
    val = val * 6.408849 - 3.1415927;
    float4 r5 = val * val;
    float4 r6 = r5 * r5;
    float4 r7 = r6 * r5;
    float4 r8 = r6 * r5;
    float4 r1 = r5 * val;
    float4 r2 = r1 * r5;
    float4 r3 = r2 * r5;
    float4 sin7 = {1, -0.16161616, 0.0083333, -0.00019841} ;
    float4 cos8  = {-0.5, 0.041666666, -0.0013888889, 0.000024801587} ;
    s =  val + r1 * sin7.y + r2 * sin7.z + r3 * sin7.w;
    c = 1 + r5 * cos8.x + r6 * cos8.y + r7 * cos8.z + r8 * cos8.w;
}

 
void vert (inout appdata_full v) {
   
    float factor = (1 - 0.75 -  v.color.r) * 0.5;
       
    const float _WindSpeed  = (0.75  +  v.color.g );    
    const float _WaveScale = 0.75;
   
    const float4 _swayX = float4(0.02, 0.04, 0.2, 0.01);
    const float4 _swayZ = float4 (0.024, .08, 0.08, 0.2);
    const float4 Speed = float4 (1.2, 2, 1.6, 4.8);
 
    float4 _swayXmove = float4(0.024, 0.04, -0.12, 0.096);
    float4 _swayZmove = float4 (0.006, .02, -0.02, 0.1);
   
    float4 waves;
    waves = v.vertex.x * _swayX;
    waves += v.vertex.z * _swayZ;
 
    waves += _Time.x * (1 - _Speed * 2 - v.color.b ) * Speed *_WindSpeed;
 
    float4 s, c;
    waves = frac (waves);
    Calc (waves, s,c);
 
    float waveAmount = v.texcoord.y * (v.color.a + 1.0);
    s *= waveAmount;
 
    s *= normalize (Speed);
 
    s = s * s;
    float fade = dot (s, 1.3);
    s = s * s;
    float3 waveMove = float3 (0,0,0);
    waveMove.x = dot (s, _swayXmove);
    waveMove.z = dot (s, _swayZmove);
    v.vertex.xz -= mul ((float3x3)unity_WorldToObject, waveMove).xz;
   
}
 
void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    fixed4 bump = tex2D(_BumpMap, IN.uv_MainTex);
    o.Albedo = c.rgb;
    o.Alpha = c.a; 	
    o.Normal = UnpackNormal(bump);


}
ENDCG
}
 
}