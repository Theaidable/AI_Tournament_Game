Shader "Custom/VisionCone"
{
    Properties
    {
        _Color("Cone Color", Color)        = (1,1,0,0.5)
        [HideInInspector]_VisionRange("Vision Range", Float) = 10
        _Gloss("Edge Softness", Range(0.1,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha One   // additive glow

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float  _VisionRange;
            float  _Gloss;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 localPos : TEXCOORD0;  // local-space position
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;     // pass local-space coords
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // distance from the cone's apex in XZ plane
                float d = length(i.localPos.xz);
                // normalized distance [0..1]
                float t = saturate(d / _VisionRange);

                // fade out toward the edge using gloss
                float alpha = smoothstep(1.0, 1.0 - _Gloss, t);

                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
