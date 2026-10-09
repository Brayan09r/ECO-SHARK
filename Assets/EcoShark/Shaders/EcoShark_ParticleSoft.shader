// ECO SHARK — Partículas sin textura (pipeline Built-in): la forma se dibuja con matemáticas.
//  _Ring = 0 -> punto suave (plancton / polvo marino).
//  _Ring = 1 -> burbuja: aro brillante con un reflejo.
// Usa el color de cada partícula (Particle System) y se suma a la escena (brilla un poco).
Shader "EcoShark/ParticleSoft"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Ring ("Forma: 0 punto, 1 burbuja", Range(0, 1)) = 0
        _MaxY ("No dibujar por encima de esta altura (superficie)", Float) = 1000
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Ring;
            float _MaxY;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float wy : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2.0 - 1.0;
                o.color = v.color * _Color;
                o.wy = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                clip(_MaxY - i.wy);   // nada de plancton ni burbujas en el cielo
                float d = length(i.uv);
                float dotShape = saturate(1.0 - d);
                dotShape *= dotShape;
                float ring = smoothstep(0.55, 0.8, d) * (1.0 - smoothstep(0.86, 1.0, d));
                float spot = saturate(1.0 - length(i.uv - float2(-0.35, 0.35)) / 0.25);
                float bubble = saturate(ring * 0.9 + spot * 0.9 + (1.0 - smoothstep(0.0, 0.9, d)) * 0.12);
                float a = lerp(dotShape, bubble, _Ring);
                return fixed4(i.color.rgb, a * i.color.a);
            }
            ENDCG
        }
    }
}
