Shader "Custom/ToonWithOutline"
{
    Properties
    {
        _MainTex    ("Base (RGB)", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // ─────────── Outline Pass ───────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Always" }
            Cull Front               // draw backfaces
            ZWrite On
            ZTest LEqual
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #include "UnityCG.cginc"

            uniform float _OutlineWidth;
            uniform float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vertOutline(appdata v)
            {
                v2f o;
                // Extrude along normal in object space
                float3 n = normalize(v.normal);
                float4 extruded = v.vertex + float4(n * _OutlineWidth, 0);
                o.pos = UnityObjectToClipPos(extruded);
                o.color = _OutlineColor;
                return o;
            }

            float4 fragOutline(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }

        // ─────────── Main Toon Pass ───────────
        Pass
        {
            Name "BASE"
            Tags { "LightMode" = "UniversalForward" } // or your pipeline’s base pass
            Cull Back
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // your existing toon shading here
                float4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
