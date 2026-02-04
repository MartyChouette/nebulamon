Shader "Nebula/GBCPalette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color0 ("Darkest", Color) = (0.06, 0.22, 0.06, 1)
        _Color1 ("Dark", Color) = (0.19, 0.38, 0.19, 1)
        _Color2 ("Light", Color) = (0.55, 0.67, 0.06, 1)
        _Color3 ("Lightest", Color) = (0.61, 0.74, 0.06, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _Color0;
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // Quantize to 4 levels
                fixed4 result;
                if (lum < 0.25)
                    result = _Color0;
                else if (lum < 0.5)
                    result = _Color1;
                else if (lum < 0.75)
                    result = _Color2;
                else
                    result = _Color3;

                result.a = 1;
                return result;
            }
            ENDCG
        }
    }
}
