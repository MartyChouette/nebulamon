Shader "Nebula/CRTPhosphorDither"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskType ("Mask Type (0=Grille, 1=Shadow)", Float) = 0
        _MaskStrength ("Mask Strength", Range(0, 1)) = 0.4
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.3
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.15
        _PixelScale ("Pixel Scale", Float) = 3
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
            float _MaskType;
            float _MaskStrength;
            float _DitherStrength;
            float _GlowStrength;
            float _PixelScale;

            // 4x4 Bayer dither matrix (values 0..15, normalized to 0..1)
            static const float bayerMatrix[16] = {
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Sample main texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // 2. Phosphor glow -- box blur from 4 neighbors
                float2 texel = _MainTex_TexelSize.xy;
                fixed4 glowUp    = tex2D(_MainTex, i.uv + float2(0, texel.y));
                fixed4 glowDown  = tex2D(_MainTex, i.uv - float2(0, texel.y));
                fixed4 glowLeft  = tex2D(_MainTex, i.uv - float2(texel.x, 0));
                fixed4 glowRight = tex2D(_MainTex, i.uv + float2(texel.x, 0));
                fixed4 blur = (glowUp + glowDown + glowLeft + glowRight) * 0.25;
                col.rgb = lerp(col.rgb, max(col.rgb, blur.rgb), _GlowStrength);

                // 3. Ordered dithering -- 4x4 Bayer
                float2 screenPos = i.uv * _MainTex_TexelSize.zw; // pixel coords
                int bx = ((int)floor(screenPos.x)) % 4;
                int by = ((int)floor(screenPos.y)) % 4;
                // Ensure positive modulo
                bx = (bx + 4) % 4;
                by = (by + 4) % 4;
                float threshold = bayerMatrix[by * 4 + bx];
                col.rgb += _DitherStrength * (threshold - 0.5);

                // 4. Phosphor mask
                float pixelScale = max(1.0, _PixelScale);
                float cellX = floor(screenPos.x / pixelScale);
                float cellY = floor(screenPos.y / pixelScale);

                float3 mask = float3(1, 1, 1);

                if (_MaskType < 0.5)
                {
                    // Aperture grille -- vertical RGB stripes
                    int stripe = ((int)cellX) % 3;
                    // Ensure positive modulo
                    stripe = (stripe + 3) % 3;
                    if (stripe == 0)      mask = float3(1.0, 0.3, 0.3);
                    else if (stripe == 1) mask = float3(0.3, 1.0, 0.3);
                    else                  mask = float3(0.3, 0.3, 1.0);
                }
                else
                {
                    // Shadow mask -- dot triad pattern
                    int dotX = ((int)cellX) % 3;
                    int dotY = ((int)cellY) % 2;
                    dotX = (dotX + 3) % 3;
                    dotY = (dotY + 2) % 2;
                    // Offset every other row
                    int shifted = (dotX + dotY) % 3;
                    if (shifted == 0)      mask = float3(1.0, 0.3, 0.3);
                    else if (shifted == 1) mask = float3(0.3, 1.0, 0.3);
                    else                   mask = float3(0.3, 0.3, 1.0);
                }

                col.rgb *= lerp(float3(1, 1, 1), mask, _MaskStrength);

                col.rgb = saturate(col.rgb);
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
