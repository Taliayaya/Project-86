// WarFX Shader
// (c) 2015 Jean Moreno

Shader "WFX/Multiply Soft Tint"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Texture", 2D) = "white" {}
	}
	
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend DstColor SrcColor
		Cull Off Lighting Off ZWrite Off Fog { Color (0.5,0.5,0.5,0.5) }
		
		Pass
		{
			CGPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"
			
			#pragma debug
			
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};
			
			struct vdata
			{
				float4 vertex : POSITION;
				half2 texcoord : TEXCOORD0;
//				float3 normal : NORMAL;
//    			float4 texcoord : TEXCOORD0;
//			    float4 texcoord1 : TEXCOORD1;
			    fixed4 color : COLOR;
			};
			
			fixed4 _TintColor;
			sampler2D _MainTex;
			
			v2f vert (vdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.color = v.color;
				o.uv = v.texcoord;
				return o;
			}
			
			fixed4 frag (v2f i) : COLOR0
			{
//				return tex2D(_MainTex, i.uv) * i.color;
				fixed4 tex = tex2D(_MainTex, i.uv);
				tex.rgb *= i.color.rgb * _TintColor.rgb;
				tex = lerp(fixed4(0.5,0.5,0.5,0.5), tex, tex.a * i.color.a);
				return tex;
//				return lerp(fixed4(1,1,1,1), i.color * tex, i.color.a);
			}
			
			ENDCG
		}
	}
}