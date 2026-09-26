Shader "DuckovCustomModel/Ysm Preview"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cull ("Cull", Float) = 2
        _SrcBlend ("Source blend", Float) = 1
        _DstBlend ("Destination blend", Float) = 0
        _ZWrite ("Depth write", Float) = 1
        _ForceOpaque ("Opaque output", Float) = 1
        _FrameUv ("Frame UV", Vector) = (1,1,0,0)
        _NextFrameUv ("Next frame UV", Vector) = (1,1,0,0)
        _FrameBlend ("Frame blend", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "YsmPreview"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _FrameUv;
                float4 _NextFrameUv;
                float _FrameBlend;
                float _ForceOpaque;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 noAnimation : TEXCOORD2;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float noAnimation : TEXCOORD1;
                float4 color : COLOR;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.noAnimation = input.noAnimation.x;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.noAnimation > 0.5 ? input.uv : input.uv * _FrameUv.xy + _FrameUv.zw;
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (input.noAnimation < 0.5 && _FrameBlend > 0)
                    texel = lerp(texel, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv * _NextFrameUv.xy + _NextFrameUv.zw), _FrameBlend);
                half4 color = texel * input.color;
                if (color.a <= 0) discard;
                if (_ForceOpaque > 0.5) color.a = 1;
                return color;
            }
            ENDHLSL
        }
    }
}
