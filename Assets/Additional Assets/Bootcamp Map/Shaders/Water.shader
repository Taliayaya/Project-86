
Shader "AP3X/Water/WScroll" {
    Properties {
    	_Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Texture (RGB)", 2D) = "white" {}
        _ScrollX ("Scroll Horizontal Speed", Float) = 1.0
        _ScrollY ("Scroll Vertical Speed", Float) = 0.0
        _Intensity ("transparency", Range(0.0, 1.0)) = 1.0
       
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Lighting Off 
        Fog { Mode Off }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        LOD 100

        CGINCLUDE
        #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4 _MainTex_ST;
        float _ScrollX;
        float _ScrollY;
        float _Intensity;
        float _Alpha;
        fixed4 _Color;

        struct v2f {
           float4 pos : SV_POSITION;
           float2 uv : TEXCOORD0;
           fixed4 color : TEXCOORD1;     
        };


        v2f vert (appdata_full v)
        {
           v2f o;
           o.pos = UnityObjectToClipPos(v.vertex);
           o.uv = TRANSFORM_TEX(v.texcoord.xy,_MainTex) + frac(float2(_ScrollX, _ScrollY) * _Time);
           o.color = fixed4(_Intensity, _Intensity, _Intensity, 1.0);
          

           return o;
        }
        ENDCG


        Pass {
           CGPROGRAM
           #pragma vertex vert
           #pragma fragment frag
           #pragma fragmentoption ARB_precision_hint_fastest     
           fixed4 frag (v2f i) : COLOR
           {
             fixed4 o;
             fixed4 tex = tex2D (_MainTex, i.uv)*_Color;
             o = tex * i.color;
             return o;
           }
           ENDCG 
        }  
    }
}


