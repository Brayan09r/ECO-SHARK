// ECO SHARK — Tiburón cartoon (pipeline Built-in).
//  - Textura de color (Albedo) + mapa de brillo (Emission) horneados por Antigravity en Blender.
//  - Sombreado "toon": luz y sombra en bandas limpias, como en dibujos animados.
//  - Contorno oscuro (técnica de casco invertido).
//  - NADO SIN HUESOS: el cuerpo se dobla de lado a lado en el vertex shader (cada parte gira, no solo se desplaza);
//    la onda va de la nariz a la cola, la cabeza casi quieta y la cola con más movimiento.
//    SharkSwimWave.cs controla la fase y la amplitud.
Shader "EcoShark/CartoonSwim"
{
    Properties
    {
        [Header(Color)]
        _MainTex ("Textura de color (Albedo)", 2D) = "white" {}
        _Tint ("Tinte", Color) = (1,1,1,1)
        _EmissionMap ("Mapa de brillo (Emission)", 2D) = "black" {}
        _EmissionColor ("Color del brillo", Color) = (1,1,1,1)
        _EmissionStrength ("Intensidad del brillo", Range(0,5)) = 1.5

        [Header(Sombreado cartoon)]
        _ShadowColor ("Color de la sombra", Color) = (0.55,0.66,0.82,1)
        _ShadeThreshold ("Límite luz/sombra", Range(-1,1)) = 0.05
        _ShadeSoftness ("Suavidad del límite", Range(0.001,0.5)) = 0.08
        _SelfLight ("Luz ambiente propia (neutra)", Range(0,1)) = 0.4
        _LightTint ("Cuánto tiñe el color de la luz", Range(0,1)) = 0.3
        _RimColor ("Color del borde luminoso", Color) = (0.55,0.85,1,1)
        _RimPower ("Suavidad del borde", Range(0.5,8)) = 3.5
        _RimStrength ("Intensidad del borde", Range(0,2)) = 0.35

        [Header(Contorno)]
        _OutlineColor ("Color del contorno", Color) = (0.04,0.1,0.18,1)
        _OutlineWidth ("Grosor del contorno", Range(0,0.08)) = 0.025

        [Header(Nado controlado por SharkSwimWave)]
        _SwimAmp ("Amplitud", Float) = 0
        _SwimPhase ("Fase", Float) = 0
        _WaveNumber ("Número de onda", Float) = 2.5
        _HeadPos ("Posición de la nariz", Float) = 1.5
        _BodyLength ("Largo del cuerpo", Float) = 3
        _SwimForward ("Eje adelante (objeto)", Vector) = (0,0,1,0)
        _SwimSide ("Eje lateral (objeto)", Vector) = (1,0,0,0)
        _SwimUp ("Eje arriba (objeto)", Vector) = (0,1,0,0)
        _SwimVertical ("Sube y baja extra (0 = solo lateral)", Range(0,1)) = 0
        _BendDir ("Curva al girar (objeto)", Vector) = (0,0,0,0)
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    float _SwimAmp;
    float _SwimPhase;
    float _WaveNumber;
    float _HeadPos;
    float _BodyLength;
    float4 _SwimForward;
    float4 _SwimSide;
    float4 _SwimUp;
    float _SwimVertical;
    float4 _BendDir;

    // Curva del cuerpo en un punto de la columna ("u" = posición a lo largo del cuerpo, nariz = _HeadPos).
    // y = desplazamiento lateral; slope = pendiente (qué tanto gira esa parte del cuerpo).
    // La onda viaja de la NARIZ hacia la COLA, como en un pez real.
    void SwimCurve(float u, out float y, out float slope)
    {
        float len = max(_BodyLength, 0.0001);
        float raw = (_HeadPos - u) / len;
        float t = saturate(raw);                      // 0 = nariz, 1 = cola
        float A = _SwimAmp * (0.08 + t * t);          // la cola se mueve más
        float dA = (raw > 0.0 && raw < 1.0) ? -_SwimAmp * 2.0 * t / len : 0.0;
        float arg = _SwimPhase + u * _WaveNumber;
        float sn = sin(arg);
        float cs = cos(arg);
        y = A * sn;
        slope = dA * sn + A * _WaveNumber * cs;
    }

    // Dobla el cuerpo de lado a lado como una columna flexible (no solo lo desplaza):
    //  - cada "rodaja" del cuerpo GIRA siguiendo la curva, así la aleta de la cola se ve de canto o de frente
    //    según hacia dónde va (eso es lo que se nota desde la cámara de costado);
    //  - el cuerpo no se estira: al curvarse, la cola se acerca un poco a la cabeza;
    //  - las normales también giran, así cambia la luz sobre la cola al aletear.
    void SwimDeform(inout float3 pos, inout float3 nrm)
    {
        float3 F = _SwimForward.xyz;
        float3 S = _SwimSide.xyz;
        float3 U = _SwimUp.xyz;

        float along = dot(pos, F);
        float side = dot(pos, S);
        float up = dot(pos, U);

        float y, slope;
        SwimCurve(along, y, slope);

        // Largo constante: sumar lo que "se pierde" al curvarse entre este punto y la nariz.
        float span = max(_HeadPos - along, 0.0);
        float du = span / 8.0;
        float pull = 0.0;
        [unroll]
        for (int i = 0; i < 8; i++)
        {
            float yy, ss;
            SwimCurve(along + (i + 0.5) * du, yy, ss);
            pull += (1.0 - rsqrt(1.0 + ss * ss)) * du;
        }

        // Giro de esta rodaja alrededor del eje del lomo.
        float c = rsqrt(1.0 + slope * slope);
        float s = slope * c;
        float3 F2 = F * c + S * s;
        float3 S2 = S * c - F * s;

        float len = max(_BodyLength, 0.0001);
        float t = saturate((_HeadPos - along) / len);

        pos = F * (along + pull) + S * y + S2 * side + U * up
            + U * (y * _SwimVertical)          // opcional: un poco de sube y baja (0 = solo de lado a lado)
            + _BendDir.xyz * (t * t);          // curva al girar: la cola se queda atrás

        float nf = dot(nrm, F);
        float ns = dot(nrm, S);
        float nu = dot(nrm, U);
        nrm = nf * F2 + ns * S2 + nu * U;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        // ---------- Pasada 1: contorno ----------
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 p = v.vertex.xyz;
                float3 n = v.normal;
                SwimDeform(p, n);
                p += normalize(n) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(p, 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // ---------- Pasada 2: cuerpo con luz cartoon ----------
        CGPROGRAM
        // noambient: usamos una luz ambiente neutra propia (_SelfLight) para que el azul del agua no tiña el vientre crema.
        // nofog: el protagonista no se pierde en la niebla (se lee mejor, como en los juegos móviles).
        #pragma surface surf Toon vertex:vert fullforwardshadows addshadow noambient nofog
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _EmissionMap;
        fixed4 _Tint;
        fixed4 _EmissionColor;
        fixed4 _ShadowColor;
        fixed4 _RimColor;
        half _EmissionStrength;
        half _ShadeThreshold;
        half _ShadeSoftness;
        half _SelfLight;
        half _LightTint;
        half _RimPower;
        half _RimStrength;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void vert (inout appdata_full v)
        {
            float3 p = v.vertex.xyz;
            float3 n = v.normal;
            SwimDeform(p, n);
            v.vertex.xyz = p;
            v.normal = n;
        }

        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir);
            half lit = smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndl) * atten;
            half3 shade = lerp(_ShadowColor.rgb, half3(1, 1, 1), lit);
            half4 c;
            half3 lightCol = lerp(dot(_LightColor0.rgb, half3(0.3, 0.59, 0.11)).xxx, _LightColor0.rgb, _LightTint);
            c.rgb = s.Albedo * lightCol * shade;
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Tint;
            o.Albedo = tex.rgb;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            fixed3 glow = tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionColor.rgb * _EmissionStrength;
            o.Emission = glow
                       + _RimColor.rgb * pow(rim, _RimPower) * _RimStrength
                       + o.Albedo * _SelfLight;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
