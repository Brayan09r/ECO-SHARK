// ECO SHARK — Rayo de sol bajo el agua ("god ray"), aditivo y semitransparente (pipeline Built-in).
// Fuerte arriba (superficie) y se desvanece hacia abajo; bordes suaves y vetas que se mueven.
// GodRays.cs anima la intensidad y el vaivén de cada rayo.
Shader "EcoShark/GodRay"
{
    Properties
    {
        _Color ("Color", Color) = (0.6, 0.92, 1, 1)
        _Intensity ("Intensidad (la pone el script)", Range(0, 2)) = 0.25
        _Falloff ("Qué tan rápido se apaga hacia abajo", Range(0.3, 4)) = 1.6
        _Seed ("Semilla (la pone el script)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Intensity, _Falloff, _Seed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float s = sin(i.uv.x * 3.14159);
                float edge = s * s;                                        // bordes suaves
                float fall = pow(saturate(i.uv.y), _Falloff);             // uv.y = 1 arriba
                fall *= saturate((1.0 - i.uv.y) / 0.12);                  // entrada suave (sin borde recto arriba)
                float streak = 0.7 + 0.3 * sin(i.uv.x * 19.0 + _Time.y * 0.6 + _Seed * 7.0)
                                   * sin(i.uv.x * 7.0 - _Time.y * 0.35 + _Seed);
                return fixed4(_Color.rgb * (_Intensity * edge * fall * streak), 1);
            }
            ENDCG
        }
    }
}
