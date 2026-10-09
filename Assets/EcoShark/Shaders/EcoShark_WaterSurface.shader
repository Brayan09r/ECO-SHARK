// ECO SHARK — Superficie del mar con olas animadas (pipeline Built-in).
// Es una franja vertical en el plano del juego (detrás del tiburón): su borde de arriba es la línea del agua.
//  - Olas: suma de 3 ondas que viajan + ondas extra alrededor del tiburón cuando está cerca de la superficie.
//  - Línea de espuma blanca en la cresta y una banda de luz debajo (como en los juegos móviles).
// Los parámetros de las olas los pone OceanSurface.cs como variables globales (_EcoWave*),
// así el barco (FloatOnWaves.cs) puede flotar exactamente sobre las mismas olas.
Shader "EcoShark/WaterSurface"
{
    Properties
    {
        _FoamColor ("Espuma", Color) = (0.95, 1, 1, 1)
        _LightColor ("Luz bajo la superficie", Color) = (0.55, 0.88, 0.98, 1)
        _WaterTop ("Agua (igual que el fondo)", Color) = (0.1, 0.42, 0.6, 1)
        _FoamWidth ("Grosor de la espuma", Range(0.01, 0.4)) = 0.09
        _LightDepth ("Alto de la banda de luz", Range(0.1, 3)) = 1.3
        _Depth ("Alto de la franja (lo pone el script)", Float) = 2.2
        _SolidPart ("Parte sólida desde la cresta (0-1)", Range(0.1, 1)) = 0.42
    }

    SubShader
    {
        // Transparente: arriba es sólida (tapa el cielo entre las olas) y abajo se funde con el agua,
        // así no corta en línea recta las montañas ni los rayos de sol que están detrás.
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _EcoWaveTime;
            float4 _EcoWaveAmp;    // amplitudes de las 3 ondas
            float4 _EcoWaveFreq;   // números de onda (2π / largo)
            float4 _EcoWaveSpeed;  // velocidades (radianes por segundo)
            float4 _EcoRipple;     // x del tiburón, fuerza, ancho, número de onda

            fixed4 _FoamColor;
            fixed4 _LightColor;
            fixed4 _WaterTop;
            float _FoamWidth;
            float _LightDepth;
            float _Depth;
            float _SolidPart;

            // Debe ser IGUAL a OceanSurface.HeightAt (C#).
            float EcoWaveHeight(float x)
            {
                float t = _EcoWaveTime;
                float h = _EcoWaveAmp.x * sin(x * _EcoWaveFreq.x - t * _EcoWaveSpeed.x)
                        + _EcoWaveAmp.y * sin(x * _EcoWaveFreq.y + t * _EcoWaveSpeed.y + 1.3)
                        + _EcoWaveAmp.z * sin(x * _EcoWaveFreq.z - t * _EcoWaveSpeed.z + 2.1);
                float d = abs(x - _EcoRipple.x);
                float w = max(_EcoRipple.z, 0.0001);
                h += _EcoRipple.y * exp(-(d * d) / (w * w)) * sin(d * _EcoRipple.w - t * 6.0);
                return h;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                if (v.uv.y > 0.5) wp.y += EcoWaveHeight(wp.x);   // solo el borde de arriba forma las olas
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1.0));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float fromTop = (1.0 - i.uv.y) * _Depth;                      // distancia a la cresta (unidades)
                fixed3 col = lerp(_LightColor.rgb, _WaterTop.rgb, smoothstep(0.0, _LightDepth, fromTop));
                float foam = 1.0 - smoothstep(_FoamWidth * 0.6, _FoamWidth, fromTop);
                col = lerp(col, _FoamColor.rgb, foam);
                float alpha = 1.0 - smoothstep(_Depth * _SolidPart, _Depth, fromTop);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
}
