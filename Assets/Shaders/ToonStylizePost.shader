Shader "Hidden/ToonStylizePost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Steps ("Posterize Steps", Float) = 4
        _Thickness ("Outline Thickness", Float) = 1.2
        _Strength ("Outline Strength", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Steps;
            float _Thickness;
            float _Strength;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = float4(v.vertex.xy, 0, 1); // <- Fixed line!
                o.uv = v.uv;
                return o;
            }

            // Sobel Outline
            float3 sobel(sampler2D tex, float2 uv, float2 texelSize)
            {
                float3 sample[9];
                int k = 0;
                [unroll]
                for(int y = -1; y <= 1; ++y)
                    [unroll]
                    for(int x = -1; x <= 1; ++x)
                        sample[k++] = tex2D(tex, uv + float2(x, y) * texelSize).rgb;

                float3 gx = sample[2] + 2*sample[5] + sample[8] - sample[0] - 2*sample[3] - sample[6];
                float3 gy = sample[6] + 2*sample[7] + sample[8] - sample[0] - 2*sample[1] - sample[2];

                float3 sobelVal = sqrt(gx * gx + gy * gy);
                return sobelVal;
            }

            float3 posterize(float3 c, float steps)
            {
                return floor(c * steps) / steps;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;
                float3 post = posterize(col, _Steps);

                float3 outline = sobel(_MainTex, i.uv, _MainTex_TexelSize.xy * _Thickness);
                float outlineVal = dot(outline, float3(0.333,0.333,0.333)) * _Strength;
                post = lerp(post, float3(0,0,0), saturate(outlineVal));

                return float4(post, 1);
            }
            ENDHLSL
        }
    }
}
