// Paneles de las pantallas: color por vertice (degradado) multiplicado por _Color, sin iluminacion,
// y un patron opcional (anillos hexagonales, como moleculas) mezclado hacia _PatternColor.
Shader "Quimikaze/Panel"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Pattern ("Patron (alfa)", 2D) = "black" {}
        _PatternColor ("Color del patron", Color) = (1,1,1,1)
        _PatternStrength ("Intensidad del patron", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _Pattern;
            float4 _Pattern_ST;
            fixed4 _Color;
            fixed4 _PatternColor;
            half _PatternStrength;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Pattern);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = i.color;
                fixed p = tex2D(_Pattern, i.uv).a * _PatternStrength;
                c.rgb = lerp(c.rgb, _PatternColor.rgb, p);
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
}
