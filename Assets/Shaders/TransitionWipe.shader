Shader "Nebula/TransitionWipe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _Color ("Wipe Color", Color) = (0, 0, 0, 1)
        _WipeType ("Wipe Type (0=Iris, 1=Blinds, 2=Column)", Float) = 0
        _BandCount ("Band Count (Blinds)", Float) = 8
        _ColumnCount ("Column Count", Float) = 10
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
            float _Progress;
            fixed4 _Color;
            float _WipeType;
            float _BandCount;
            float _ColumnCount;

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

                float mask = 0;

                if (_WipeType < 0.5)
                {
                    // Iris wipe: circular mask from center
                    float2 center = float2(0.5, 0.5);
                    float dist = distance(i.uv, center);
                    float radius = (1.0 - _Progress) * 0.8; // 0.8 covers full screen diagonal
                    mask = step(radius, dist);
                }
                else if (_WipeType < 1.5)
                {
                    // Blinds: horizontal bands
                    float bandCount = max(1.0, _BandCount);
                    float band = frac(i.uv.y * bandCount);
                    mask = step(1.0 - _Progress, band);
                }
                else
                {
                    // Column dissolve: staggered vertical drops
                    float colCount = max(1.0, _ColumnCount);
                    float colIdx = floor(i.uv.x * colCount);
                    // Stagger: odd columns start slightly later
                    float stagger = fmod(colIdx, 2.0) * 0.15;
                    float localProgress = saturate((_Progress - stagger) / (1.0 - stagger));
                    mask = step(1.0 - localProgress, i.uv.y);
                }

                return lerp(col, _Color, mask);
            }
            ENDCG
        }
    }
}
