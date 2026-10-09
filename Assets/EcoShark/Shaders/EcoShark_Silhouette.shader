// ECO SHARK — Siluetas lejanas (montañas y abismos submarinos) para la capa de paralaje del fondo.
// Color plano con degradado vertical (más claro arriba, se funde con el agua abajo). Sin luz ni niebla:
// el color ya está elegido para parecer lejano.
Shader "EcoShark/Silhouette"
{
    Properties
    {
        _TopColor ("Color arriba", Color) = (0.08, 0.3, 0.42, 1)
        _BottomColor ("Color abajo", Color) = (0.02, 0.08, 0.15, 1)
        _YTop ("Alto de la cima (y local)", Float) = 20
        _YBottom ("Base (y local)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _TopColor, _BottomColor;
            float _YTop, _YBottom;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float y : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.y = v.vertex.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = saturate((i.y - _YBottom) / max(_YTop - _YBottom, 0.001));
                return fixed4(lerp(_BottomColor.rgb, _TopColor.rgb, t * t), 1);
            }
            ENDCG
        }
    }
}
