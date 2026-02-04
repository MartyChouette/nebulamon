Shader "Nebula/ScanlineOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LineWidth ("Line Width (pixels)", Float) = 2
        _Darkness ("Darkness", Range(0, 1)) = 0.3
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
            float4 _MainTex_TexelSize;
            float _LineWidth;
            float _Darkness;

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

                // Screen-space Y in pixels
                float screenY = i.uv.y * _MainTex_TexelSize.w;
                float lineWidth = max(1.0, _LineWidth);

                // Every other band of lineWidth pixels is darkened
                float band = fmod(floor(screenY / lineWidth), 2.0);
                float darken = lerp(1.0, 1.0 - _Darkness, band);

                col.rgb *= darken;
                return col;
            }
            ENDCG
        }
    }
}
