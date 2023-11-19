// WarFX Shader
// (c) 2015 Jean Moreno

Shader "WFX/Scroll/Additive"
{
	Properties
	{
		_MainTex ("Looped Texture + Alpha Mask", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		
		_ScrollSpeed ("Scroll Speed", Float) = 2.0
	}
	
	Category
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend One One
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
		BindChannels
		{
			Bind "Color", color
			Bind "Vertex", vertex
			Bind "TexCoord", texcoord
		}
	
		// ---- Fragment program cards
		SubShader
		{
			Pass 
			{
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_particles
	
				#include "UnityCG.cginc"
	
				sampler2D _MainTex;
				fixed4 _TintColor;
				
				struct appdata_t
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
				};
	
				struct v2f
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD1;
					#endif
				};
	
				float4 _MainTex_ST;
				
				v2f vert (appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
					o.projPos = ComputeScreenPos (o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
					return o;
				}
	
				sampler2D _CameraDepthTexture;
				float _InvFade;
				float _ScrollSpeed;
				
				fixed4 frag (v2f i) : COLOR
				{
					#ifdef SOFTPARTICLES_ON
					float sceneZ = LinearEyeDepth (UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
					float partZ = i.projPos.z;
					float fade = saturate (_InvFade * (sceneZ-partZ));
					i.color.a *= fade;
					#endif
					
					half4 prev = tex2D(_MainTex, i.texcoord).a * i.color.a;
					i.texcoord.y -= fmod(_Time*_ScrollSpeed,1);
					prev.rgb *= tex2D(_MainTex, i.texcoord).rgb * i.color.rgb;
					return prev;
				}
				ENDCG 
			}
		}
		
		// ---- Dual texture cards
		SubShader {
			Pass {
				SetTexture [_MainTex] {
					combine texture * primary
				}
				SetTexture [_MainTex] {
					combine previous * previous alpha, previous
				}
			}
		}
		
		// ---- Single texture cards (does not do particle colors)
		SubShader {
			Pass {
				SetTexture [_MainTex] {
					combine texture * texture alpha, texture
				}
			}
		}
	}
}