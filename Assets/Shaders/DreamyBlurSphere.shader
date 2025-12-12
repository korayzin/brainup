Shader "Custom/DreamySphereURP"
{
    Properties
    {
        [Header(Main Settings)]
        [MainTexture] _MainTex ("Main Image", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)

        [Header(Dreamy Clouds)]
        _CloudTex ("Cloud/Noise Texture", 2D) = "black" {}
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 0.5)
        _CloudSpeed ("Cloud Scroll Speed", Vector) = (0.1, 0.05, 0, 0)
        _CloudScale ("Cloud Tiling", Float) = 1.0

        [Header(Physical Waves)]
        _WaveAmplitude ("Wave Height", Range(0, 0.2)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(0, 10)) = 2.0
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 1.0

        [Header(Blur Effect)]
        _BlurAmount ("Blur Radius", Range(0, 0.05)) = 0.01
        _Samples ("Blur Quality (Int)", Range(4, 20)) = 10

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (0.0, 0.8, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
    }
    SubShader
    {
        // URP Etiketleri
        Tags { "RenderType"="Opaque" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        
        // Yumusak gecis icin blending
        Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            Name "DreamyPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP Core Kütüphanesi
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv_cloud : TEXCOORD1;
                float3 normalWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };

            // Texture Tanımları (URP stili)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);

            // Değişkenler (CBUFFER içinde olmalı - Batching için)
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _CloudTex_ST;
                float4 _Color;
                float4 _CloudColor;
                float2 _CloudSpeed;
                float _CloudScale;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _BlurAmount;
                int _Samples;
                float4 _RimColor;
                float _RimPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // --- 1. VERTEX WAVES (URP) ---
                // Object Space pozisyonunu al
                float3 pos = input.positionOS.xyz;
                
                // Dalgalanma matematigi
                float wave = sin(pos.x * _WaveFrequency + _Time.y * _WaveSpeed) 
                           + cos(pos.z * _WaveFrequency * 0.5 + _Time.y * _WaveSpeed);
                
                // Normal yönünde şişirme
                pos += input.normalOS * wave * _WaveAmplitude;

                // URP Dönüşümleri
                // Object Space -> World Space -> Clip Space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(pos);
                output.positionCS = vertexInput.positionCS;

                // UV Hesaplama
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.uv_cloud = input.uv * _CloudScale; 

                // Normal ve View Direction Hesaplama (Rim Light icin)
                // Normali World Space'e cevir
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // View Direction (Kameradan objeye bakis)
                // URP'de GetWorldSpaceViewDir fonksiyonu pozisyon kullanir
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- 2. BLUR MEKANIZMASI ---
                half4 mainCol = 0;
                float totalWeight = 0;

                // URP'de loop icin [unroll] gerekebilir ama basit dongu de calisir
                for (int j = 0; j < _Samples; j++)
                {
                    float offset = (float)j / (float)_Samples;
                    float angle = j * 10.0 + _Time.y * 0.5; // Dönen blur
                    float2 blurOffset = float2(cos(angle), sin(angle)) * _BlurAmount * offset;
                    
                    // URP Texture Örnekleme: SAMPLE_TEXTURE2D(Texture, Sampler, UV)
                    mainCol += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + blurOffset);
                    totalWeight += 1.0;
                }
                mainCol /= totalWeight;
                mainCol *= _Color;

                // --- 3. CLOUD LAYER ---
                float2 cloudUV = input.uv_cloud + (_Time.y * _CloudSpeed);
                half4 cloudTex = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloudUV);
                
                half4 cloudLayer = cloudTex * _CloudColor;
                half4 finalCol = lerp(mainCol, cloudLayer, cloudLayer.a * 0.5);

                // --- 4. RIM LIGHT (FRESNEL) ---
                // Normalleri normalize et
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                
                // Dot product (Nokta carpimi)
                float NdotV = saturate(dot(N, V));
                float rim = pow(1.0 - NdotV, _RimPower);
                
                finalCol.rgb += _RimColor.rgb * rim;

                return finalCol;
            }
            ENDHLSL
        }
    }
}