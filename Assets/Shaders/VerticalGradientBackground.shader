
Shader "Shader Graphs/VerticalGradientBackground"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.7, 0.9, 1.0, 1)
        _BottomColor ("Bottom Color", Color) = (1.0, 0.9, 0.85, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _TopColor;
            float4 _BottomColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float t = saturate((i.worldPos.y - 0.0) / 5.0);
                return lerp(_BottomColor, _TopColor, t);
            }
            ENDHLSL
        }
    }
}
