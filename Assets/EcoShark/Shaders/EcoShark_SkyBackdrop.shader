// ECO SHARK — Fondo lejano: cielo con nubes arriba y agua con degradado abajo (pipeline Built-in).
// Va en una placa pegada a la cámara (SkyBackdrop.cs), detrás de todo y sin niebla.
// SkyBackdrop.cs mueve la línea del horizonte (_SplitY) para que coincida con la superficie del juego.
Shader "EcoShark/SkyBackdrop"
{
    Properties
    {
        [Header(Cielo)]
        _SkyHorizon ("Cielo en el horizonte", Color) = (0.8, 0.93, 1, 1)
        _SkyTop ("Cielo arriba", Color) = (0.3, 0.62, 0.95, 1)
        _CloudColor ("Nubes", Color) = (1, 1, 1, 1)
        _CloudShadow ("Sombra de las nubes", Color) = (0.76, 0.86, 0.97, 1)
        _CloudCover ("Cantidad de nubes (menos = más nubes)", Range(0.3, 0.8)) = 0.56
        _CloudSpeed ("Velocidad de las nubes", Float) = 0.015
        _Parallax ("Paralaje de las nubes", Float) = 1.5

        [Header(Agua)]
        _WaterTop ("Agua cerca de la superficie", Color) = (0.1, 0.42, 0.6, 1)
        _WaterDeep ("Agua profunda", Color) = (0.02, 0.14, 0.25, 1)

        [Header(Lo pone el script)]
        _SplitY ("Horizonte (y local)", Float) = 0
        _GradStartY ("Inicio del degradado del agua (y local)", Float) = 0
        _Scale ("Media altura visible (local)", Float) = 70
        _CamX ("X de la cámara", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Plane" }
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _SkyHorizon, _SkyTop, _CloudColor, _CloudShadow, _WaterTop, _WaterDeep;
            float _CloudCover, _CloudSpeed, _Parallax;
            float _SplitY, _GradStartY, _Scale, _CamX;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float2 obj : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.obj = v.vertex.xy;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float s = 0.0;
                float a = 0.5;
                [unroll]
                for (int k = 0; k < 4; k++)
                {
                    s += a * vnoise(p);
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return s;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float scale = max(_Scale, 0.001);
                float y = i.obj.y;

                if (y < _SplitY)
                {
                    // Bajo el agua: claro cerca de la superficie, oscuro en lo profundo.
                    float deep = saturate((_GradStartY - y) / (scale * 2.0));
                    return fixed4(lerp(_WaterTop.rgb, _WaterDeep.rgb, deep), 1);
                }

                // Cielo
                float h = (y - _SplitY) / scale;                       // 0 = horizonte
                fixed3 sky = lerp(_SkyHorizon.rgb, _SkyTop.rgb, saturate(pow(saturate(h * 0.9), 0.7)));

                // Nubes estilo cartoon: bordes definidos, más estiradas a lo ancho, se mueven solas.
                float2 p = float2((i.obj.x + _CamX * _Parallax) / (scale * 0.9), (y - _SplitY) / (scale * 0.32));
                p.x += _Time.y * _CloudSpeed;
                float n = fbm(p);
                float band = smoothstep(0.08, 0.3, h) * (1.0 - smoothstep(1.3, 1.9, h));
                float cloud = smoothstep(_CloudCover, _CloudCover + 0.035, n) * band;
                float lit = smoothstep(_CloudCover + 0.02, _CloudCover + 0.14, n);
                fixed3 cloudCol = lerp(_CloudShadow.rgb, _CloudColor.rgb, lit);
                sky = lerp(sky, cloudCol, cloud);

                return fixed4(sky, 1);
            }
            ENDCG
        }
    }
}
