// ECO SHARK — Sombreador para modelos pintados con "vertex colors" (Blender -> Unity).
// Pipeline: Built-in (la del proyecto). Con luz, sombras y niebla del agua.
//  - Albedo  = color del vértice (lo pinta SharkColorizer).
//  - Alfa del vértice = cuánto brilla esa zona (marcas bioluminiscentes).
//  - Borde luminoso (rim) para que el tiburón se distinga del fondo azul.
//  - Luz propia suave para que el vientre crema no se vea gris en la sombra.
Shader "EcoShark/VertexColorLit"
{
    Properties
    {
        _Tint ("Tinte general", Color) = (1,1,1,1)
        _Glossiness ("Brillo de piel", Range(0,1)) = 0.35
        _Metallic ("Metálico", Range(0,1)) = 0
        _GlowColor ("Color de las marcas", Color) = (0.36,0.89,1,1)
        _GlowStrength ("Intensidad de las marcas", Range(0,5)) = 1.6
        _RimColor ("Color del borde", Color) = (0.45,0.8,1,1)
        _RimPower ("Suavidad del borde", Range(0.5,8)) = 3
        _RimStrength ("Intensidad del borde", Range(0,2)) = 0.3
        _SelfLight ("Luz propia (aclara zonas en sombra)", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Back
        ZWrite On

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float4 color : COLOR;
            float3 viewDir;
        };

        fixed4 _Tint;
        half _Glossiness;
        half _Metallic;
        fixed4 _GlowColor;
        half _GlowStrength;
        fixed4 _RimColor;
        half _RimPower;
        half _RimStrength;
        half _SelfLight;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb * _Tint.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            // _SelfLight: el vientre mira hacia abajo y casi no recibe sol; esto lo mantiene claro como en el diseño.
            o.Emission = _GlowColor.rgb * IN.color.a * _GlowStrength
                       + _RimColor.rgb * pow(rim, _RimPower) * _RimStrength
                       + o.Albedo * _SelfLight;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
