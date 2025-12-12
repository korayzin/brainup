Shader "Custom/BrainShrivelSciFi_UnlitShrink_V4_ScaleWithShrink"
{
    Properties
    {
        // --- TEXTURE ---
        _MainTex ("Albedo Map", 2D) = "white" {}

        // --- SHRINK (VERTEX DISPLACEMENT) ---
        _ShrinkAmount ("Shrink Progress (0 to 1)", Range(0, 1)) = 0.0
        _ShrinkScale  ("Max Shrink Depth", Range(0, 0.2)) = 0.08
        _NoiseTex     ("Noise Texture (Displacement)", 2D) = "white" {}
        _NoiseTiling  ("Noise Tiling", Range(1, 20)) = 3.0

        // --- OBJECT SCALE WITH SHRINK ---
        _MinObjectScale ("Min Mesh Scale @ Shrink=1", Range(0.1, 1.0)) = 0.7

        // --- SCI-FI RIM ---
        _SciFiColor   ("Sci-Fi Rim Color", Color) = (0, 0.8, 1, 1)
        _FresnelPower ("Rim Power", Range(0.5, 8.0)) = 2.5
        _RimIntensity ("Rim Intensity", Range(0, 10)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ForwardUnlitShrink"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float2 uv         : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _SciFiColor;

                float _ShrinkAmount;
                float _ShrinkScale;
                float _NoiseTiling;

                float _MinObjectScale;

                float _FresnelPower;
                float _RimIntensity;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 1) Shrink progress arttıkça mesh scale azalsın
                float scaleFactor = lerp(1.0, _MinObjectScale, _ShrinkAmount);

                // Pivot'a göre ölçekleme (object space)
                float3 scaledPosOS = input.positionOS.xyz * scaleFactor;

                // 2) Noise tabanlı inward displacement (büzüşme derinliği)
                float noiseVal = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, input.uv * _NoiseTiling, 0).r;
                noiseVal = pow(noiseVal, 1.5);

                float displacement = noiseVal * _ShrinkAmount * _ShrinkScale;

                // Ölçeklenmiş pozisyondan normal yönünde içeri it
                float3 newPositionOS = scaledPosOS - (input.normalOS * displacement);

                // Output
                VertexPositionInputs posInputs = GetVertexPositionInputs(newPositionOS);
                VertexNormalInputs   norInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = norInputs.normalWS;

                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Unlit base
                half3 baseColor = albedo.rgb;

                // Rim
                float3 N = NormalizeNormalPerPixel(input.normalWS);
                float3 V = normalize(input.viewDirWS);

                float fresnel = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                float rimBoost = (1.0 + _ShrinkAmount);
                half3 rim = _SciFiColor.rgb * fresnel * _RimIntensity * rimBoost;

                return half4(baseColor + rim, albedo.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _ShrinkAmount;
                float _ShrinkScale;
                float _NoiseTiling;
                float _MinObjectScale;
            CBUFFER_END

            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output;

                float scaleFactor = lerp(1.0, _MinObjectScale, _ShrinkAmount);
                float3 scaledPosOS = input.positionOS.xyz * scaleFactor;

                float noiseVal = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, input.uv * _NoiseTiling, 0).r;
                noiseVal = pow(noiseVal, 1.5);

                float displacement = noiseVal * _ShrinkAmount * _ShrinkScale;

                float3 newPositionOS = scaledPosOS - (input.normalOS * displacement);

                output.positionCS = TransformObjectToHClip(newPositionOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
