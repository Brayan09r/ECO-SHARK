// ECO SHARK — Decoración del fondo marino estilo cartoon (pipeline Built-in).
//  - Luz "toon" en bandas + borde luminoso suave, con niebla submarina.
//  - Vaivén opcional (algas, abanicos de coral): la punta se mueve más que la base,
//    cada objeto con su propio ritmo según dónde está. Con _SwayAmp = 0 no se mueve (rocas).
Shader "EcoShark/ToonEnv"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Color de la sombra", Color) = (0.45, 0.55, 0.75, 1)
        _ShadeThreshold ("Límite luz/sombra", Range(-1, 1)) = 0.1
        _ShadeSoftness ("Suavidad del límite", Range(0.001, 0.5)) = 0.12
        _RimColor ("Borde luminoso", Color) = (0.5, 0.85, 1, 1)
        _RimPower ("Suavidad del borde", Range(0.5, 8)) = 3
        _RimStrength ("Intensidad del borde", Range(0, 1)) = 0.25
        _Emission ("Brillo propio", Range(0, 1)) = 0

        [Header(Vaiven)]
        _SwayAmp ("Amplitud (0 = quieto)", Float) = 0
        _SwaySpeed ("Velocidad", Float) = 1.2
        _SwayBase ("Base (y local)", Float) = 0
        _SwayHeight ("Alto (y local)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Toon vertex:vert addshadow
        #pragma target 3.0

        fixed4 _Color, _ShadowColor, _RimColor;
        half _ShadeThreshold, _ShadeSoftness, _RimPower, _RimStrength, _Emission;
        float _SwayAmp, _SwaySpeed, _SwayBase, _SwayHeight;

        struct Input
        {
            float3 viewDir;
        };

        void vert (inout appdata_full v)
        {
            if (_SwayAmp > 0.0)
            {
                float h = saturate((v.vertex.y - _SwayBase) / max(_SwayHeight, 0.001));
                float3 origin = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float phase = _Time.y * _SwaySpeed + origin.x * 0.35 + origin.z * 0.5;
                float bend = sin(phase) + 0.35 * sin(phase * 2.3 + v.vertex.y * 0.8);
                v.vertex.x += _SwayAmp * pow(h, 1.5) * bend;
            }
        }

        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir);
            half lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndl) * atten;
            half3 shade = lerp(_ShadowColor.rgb, half3(1, 1, 1), lit);
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * shade;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            half rim = 1.0 - saturate(abs(dot(normalize(IN.viewDir), o.Normal)));
            o.Emission = _RimColor.rgb * pow(rim, _RimPower) * _RimStrength + o.Albedo * _Emission;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
