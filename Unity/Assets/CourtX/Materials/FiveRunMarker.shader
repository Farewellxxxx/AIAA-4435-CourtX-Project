Shader "CourtX/Running marker"
{
    Properties
    {
        _Fill ("Fill", Color) = (0.53,0.96,0.72,1)
        _Edge ("Edge", Color) = (0.07,0.30,0.23,1)
        _GroundRing ("Ground ring", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 local : TEXCOORD0;
            };
            fixed4 _Fill, _Edge;
            float _GroundRing;

            Varyings vert(float4 vertex : POSITION)
            {
                Varyings o;
                o.position = UnityObjectToClipPos(vertex);
                o.local = vertex.xy * 2.0;
                return o;
            }

            fixed4 frag(Varyings i) : SV_Target
            {
                float2 p = i.local;
                // One filled silhouette. The shaft overlaps the triangular head.
                float head = max(p.y - 0.08, (abs(p.x)*0.98 - 0.8*(p.y + 0.90))*0.79);
                float2 stem = abs(p - float2(0,0.38)) - float2(0.265,0.51);
                float arrow = min(head, max(stem.x, stem.y));
                float ring = abs(length(p) - 0.82) - 0.021;
                float d = _GroundRing > 0.5 ? ring : arrow;
                float pixel = max(fwidth(d), 0.0001);
                float border = _GroundRing > 0.5 ? 0.004 : 0.075;
                fixed3 ink = lerp(_Fill.rgb, _Edge.rgb, saturate(0.5 + (d + border)/pixel));
                return fixed4(ink, saturate(0.5 - d/pixel));
            }
            ENDCG
        }
    }
}
