Shader "DuckovCustomModel/Ysm Lit"
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
        _YsmSHAr ("Ambient R A", Vector) = (0,0,0,0)
        _YsmSHAg ("Ambient G A", Vector) = (0,0,0,0)
        _YsmSHAb ("Ambient B A", Vector) = (0,0,0,0)
        _YsmSHBr ("Ambient R B", Vector) = (0,0,0,0)
        _YsmSHBg ("Ambient G B", Vector) = (0,0,0,0)
        _YsmSHBb ("Ambient B B", Vector) = (0,0,0,0)
        _YsmSHC ("Ambient C", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "YsmForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #define _MAIN_LIGHT_SHADOWS 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float4 _YsmSHAr, _YsmSHAg, _YsmSHAb;
            float4 _YsmSHBr, _YsmSHBg, _YsmSHBb, _YsmSHC;
            float _YsmShadowMode;
            float4 _YsmSunColor, _YsmSunDirection;
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
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 glow : TEXCOORD1;
                float2 noAnimation : TEXCOORD2;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD3;
                float4 screenShadowCoord : TEXCOORD4;
                float2 uv : TEXCOORD1;
                float2 glowAndStatic : TEXCOORD2;
                float4 color : COLOR;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.screenShadowCoord = ComputeScreenPos(output.positionCS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.glowAndStatic = float2(input.glow.x, input.noAnimation.x);
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float2 uv = input.glowAndStatic.y > 0.5 ? input.uv : input.uv * _FrameUv.xy + _FrameUv.zw;
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (input.glowAndStatic.y < 0.5 && _FrameBlend > 0)
                    texel = lerp(texel, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv * _NextFrameUv.xy + _NextFrameUv.zw), _FrameBlend);
                half4 color = texel * input.color;
                if (color.a <= 0) discard;
                half3 normal = NormalizeNormalPerPixel(input.normalWS) * IS_FRONT_VFACE(facing, 1, -1);
                float4 normal4 = float4(normal, 1);
                float4 quadratic = normal.xyzz * normal.yzzx;
                half3 ambient = half3(dot(_YsmSHAr, normal4), dot(_YsmSHAg, normal4), dot(_YsmSHAb, normal4));
                ambient += half3(dot(_YsmSHBr, quadratic), dot(_YsmSHBg, quadratic), dot(_YsmSHBb, quadratic));
                ambient += _YsmSHC.rgb * (normal.x * normal.x - normal.y * normal.y);
                half3 illumination = max(half3(0.55,0.55,0.55), ambient);
                Light mainLight = GetMainLight();
                half3 mainColor = _YsmSunColor.a > 0.5 ? _YsmSunColor.rgb : mainLight.color;
                half3 mainDirection = _YsmSunColor.a > 0.5 ? normalize(_YsmSunDirection.xyz) : mainLight.direction;
                half mainDistance = _YsmSunColor.a > 0.5 ? 1 : mainLight.distanceAttenuation;
                half mainShadow = 1;
                if (_YsmShadowMode > 2.5 && _ForceOpaque > 0.5)
                    mainShadow = SampleScreenSpaceShadowmap(input.screenShadowCoord);
                else if (_YsmShadowMode > 0.5 && _MainLightShadowParams.x > 0)
                {
                    half cascade = _YsmShadowMode > 1.5 ? ComputeCascadeIndex(input.positionWS) : 0;
                    float4 shadowCoord = mul(_MainLightWorldToShadow[cascade], float4(input.positionWS, 1));
                    mainShadow = MainLightRealtimeShadow(float4(shadowCoord.xyz, 0));
                }
                illumination += mainColor * mainDistance * mainShadow
                    * saturate(dot(normal, mainDirection));
                #if defined(_ADDITIONAL_LIGHTS)
                uint additionalLights = GetAdditionalLightsCount();
                #if USE_FORWARD_PLUS
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                for (uint lightIndex = 0u; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1,1,1,1));
                    illumination += light.color * light.distanceAttenuation * light.shadowAttenuation
                        * saturate(dot(normal, light.direction));
                }
                #endif
                LIGHT_LOOP_BEGIN(additionalLights)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1,1,1,1));
                    illumination += light.color * light.distanceAttenuation * light.shadowAttenuation
                        * saturate(dot(normal, light.direction));
                LIGHT_LOOP_END
                #endif
                if (input.glowAndStatic.x >= 0) illumination = max(illumination, saturate(input.glowAndStatic.x / 15));
                color.rgb *= illumination;
                if (_ForceOpaque > 0.5) color.a = 1;
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "YsmShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma target 3.0
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "YsmDepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "YsmDepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex NormalVert
            #pragma fragment NormalFrag
            #pragma target 3.0
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Varyings NormalVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 NormalFrag(Varyings input) : SV_Target
            {
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 packed = saturate(PackNormalOctQuadEncode(normalWS) * 0.5 + 0.5);
                    return half4(PackFloat2To888(packed), 0);
                #else
                    return half4(normalWS, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
