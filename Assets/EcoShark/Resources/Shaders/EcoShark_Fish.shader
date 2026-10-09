// (Claude) Peces de los cardúmenes (FishSchools.cs): color por vértice, sin luz, con niebla submarina.
// Está en Resources/Shaders para que entre en la build (el material se crea por código).
Shader "EcoShark/Fish"
{
    Properties
    {
        _Brightness ("Brillo", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(0)
            };

            float _Brightness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = fixed4(i.color.rgb * _Brightness, 1);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
